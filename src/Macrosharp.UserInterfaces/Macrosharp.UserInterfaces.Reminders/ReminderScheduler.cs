using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.ToastNotifications;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderScheduler : IDisposable
{
    private readonly ReminderConfigurationManager _configurationManager;
    private readonly ToastNotificationHost _toastHost;
    private readonly Func<bool> _isSilentMode;
    private readonly ReminderPopupHost _popupHost;
    private readonly DateTime _programStartLocal;
    private readonly object _gate = new();

    private readonly Dictionary<string, DateTime> _nextById = new();
    private readonly Dictionary<string, DateTime> _snoozedUntilById = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public ReminderScheduler(ReminderConfigurationManager configurationManager, ToastNotificationHost toastHost, Func<bool> isSilentMode)
    {
        _configurationManager = configurationManager;
        _toastHost = toastHost;
        _isSilentMode = isSilentMode;
        _popupHost = new ReminderPopupHost();
        _programStartLocal = DateTime.Now;

        _configurationManager.ConfigurationChanged += (_, _) => RebuildSchedule();
    }

    public void Start()
    {
        lock (_gate)
        {
            _cts = new CancellationTokenSource();
            RebuildSchedule();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _cts?.Cancel();
        }

        try
        {
            _loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore cancellation races on shutdown.
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                EvaluateDueReminders();
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reminder scheduler error: {ex.Message}");
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private void EvaluateDueReminders()
    {
        var config = _configurationManager.CurrentConfiguration;
        if (!config.Settings.Enabled)
        {
            return;
        }

        var now = DateTime.Now;
        var dirty = false;

        foreach (var reminder in config.Reminders.Where(r => r.Enabled))
        {
            lock (_gate)
            {
                if (_snoozedUntilById.TryGetValue(reminder.Id, out var snoozedUntil) && snoozedUntil > now)
                {
                    continue;
                }
            }

            if (!_nextById.TryGetValue(reminder.Id, out var due))
            {
                due = ComputeNext(reminder, now, config.Settings.MissedPolicy, config.Settings.StartupGraceMinutes);
                if (due == DateTime.MinValue)
                {
                    continue;
                }

                _nextById[reminder.Id] = due;
            }

            if (now < due)
            {
                continue;
            }

            FireReminder(reminder, config);
            reminder.LastTriggeredUtc = DateTimeOffset.UtcNow;
            dirty = true;

            var next = ReminderRecurrenceCalculator.GetNextOccurrenceLocal(reminder, now, _programStartLocal);
            if (next.HasValue)
            {
                _nextById[reminder.Id] = next.Value;
            }
            else
            {
                _nextById.Remove(reminder.Id);
            }
        }

        if (dirty)
        {
            _configurationManager.SaveConfiguration(config);
        }
    }

    private DateTime ComputeNext(ReminderDefinition reminder, DateTime now, ReminderMissedPolicy missedPolicy, int startupGraceMinutes)
    {
        var next = ReminderRecurrenceCalculator.GetNextOccurrenceLocal(reminder, now, _programStartLocal);
        if (next.HasValue)
        {
            return next.Value;
        }

        if (!reminder.LastTriggeredUtc.HasValue || missedPolicy == ReminderMissedPolicy.Skip)
        {
            return DateTime.MinValue;
        }

        var scheduled = reminder.LastTriggeredUtc.Value.LocalDateTime;
        if (missedPolicy == ReminderMissedPolicy.FireAllMissed)
        {
            return now;
        }

        if (missedPolicy == ReminderMissedPolicy.FireWithinGraceWindow)
        {
            var grace = TimeSpan.FromMinutes(Math.Max(0, startupGraceMinutes));
            if (scheduled + grace >= now)
            {
                return now;
            }
        }

        return DateTime.MinValue;
    }

    private void FireReminder(ReminderDefinition reminder, ReminderConfiguration config)
    {
        var channels = reminder.Channels ?? config.Settings.DefaultChannels;
        var popup = reminder.Popup ?? config.Settings.PopupDefaults;

        var clampedPrefix = reminder.LastTriggerWasMonthEndClamp ? "(Clamped to month-end) " : string.Empty;
        var message = clampedPrefix + reminder.Message;

        if (!_isSilentMode())
        {
            if (channels.Toast)
            {
                _toastHost.Show(
                    new ToastNotificationContent
                    {
                        Title = reminder.Title,
                        Body = message,
                        Scenario = ToastScenario.Reminder,
                        Duration = ToastDuration.Long,
                    }
                );
            }

            if (channels.Sound)
            {
                AudioPlayer.PlayKnobAsync();
            }

            if (channels.Popup && popup.Enabled)
            {
                var popupReminder = new ReminderDefinition
                {
                    Id = reminder.Id,
                    Title = reminder.Title,
                    Message = message,
                    Enabled = reminder.Enabled,
                    Channels = reminder.Channels,
                    Popup = reminder.Popup,
                    Recurrence = reminder.Recurrence,
                    LastTriggeredUtc = reminder.LastTriggeredUtc,
                    LastTriggerWasMonthEndClamp = reminder.LastTriggerWasMonthEndClamp,
                };

                _popupHost.Show(
                    popupReminder,
                    popup,
                    result =>
                    {
                        if (result.Action != ReminderPopupAction.Snooze)
                        {
                            return;
                        }

                        lock (_gate)
                        {
                            _snoozedUntilById[reminder.Id] = DateTime.Now.AddMinutes(Math.Max(1, result.SnoozeMinutes));
                        }
                    }
                );
            }
        }
    }

    public void RebuildSchedule()
    {
        lock (_gate)
        {
            _nextById.Clear();
            _snoozedUntilById.Clear();

            var now = DateTime.Now;
            var config = _configurationManager.CurrentConfiguration;
            foreach (var reminder in config.Reminders.Where(r => r.Enabled))
            {
                var next = ReminderRecurrenceCalculator.GetNextOccurrenceLocal(reminder, now, _programStartLocal);
                if (next.HasValue)
                {
                    _nextById[reminder.Id] = next.Value;
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
