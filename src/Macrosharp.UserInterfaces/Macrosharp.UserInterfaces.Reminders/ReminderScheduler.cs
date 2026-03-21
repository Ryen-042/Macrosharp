using System.Text.RegularExpressions;
using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.ToastNotifications;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderScheduler : IDisposable
{
    private static readonly Regex RichTagRegex = new("\\[/?(?:b|i|color(?:=[^\\]]+)?)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ReminderConfigurationManager _configurationManager;
    private readonly ToastNotificationHost _toastHost;
    private readonly Func<bool> _isSilentMode;
    private readonly Func<bool> _areNotificationsHidden;
    private readonly Func<bool> _isReminderSoundMuted;
    private readonly ReminderPopupHost _popupHost;
    private readonly DateTime _programStartLocal;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);

    private readonly Dictionary<string, DateTime> _nextById = new();
    private readonly Dictionary<string, DateTime> _snoozedUntilById = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public ReminderScheduler(
        ReminderConfigurationManager configurationManager,
        ToastNotificationHost toastHost,
        Func<bool> isSilentMode,
        Func<bool>? areNotificationsHidden = null,
        Func<bool>? isReminderSoundMuted = null
    )
    {
        _configurationManager = configurationManager;
        _toastHost = toastHost;
        _isSilentMode = isSilentMode;
        _areNotificationsHidden = areNotificationsHidden ?? (() => false);
        _isReminderSoundMuted = isReminderSoundMuted ?? (() => false);
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
                TimeSpan nextDelay = EvaluateDueRemindersAndGetNextDelay();
                await WaitForWakeOrDelayAsync(nextDelay, cancellationToken);
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

    private TimeSpan EvaluateDueRemindersAndGetNextDelay()
    {
        var config = _configurationManager.CurrentConfiguration;
        if (!config.Settings.Enabled)
        {
            return Timeout.InfiniteTimeSpan;
        }

        var now = DateTime.Now;
        var dirty = false;
        DateTime? nextDue = null;

        foreach (var reminder in config.Reminders.Where(r => r.Enabled))
        {
            DateTime due;

            lock (_gate)
            {
                if (_snoozedUntilById.TryGetValue(reminder.Id, out var snoozedUntil) && snoozedUntil > now)
                {
                    nextDue = MinDate(nextDue, snoozedUntil);
                    continue;
                }

                if (!_nextById.TryGetValue(reminder.Id, out due))
                {
                    due = ComputeNext(reminder, now, config.Settings.MissedPolicy, config.Settings.StartupGraceMinutes);
                    if (due == DateTime.MinValue)
                    {
                        continue;
                    }

                    _nextById[reminder.Id] = due;
                }
            }

            if (now < due)
            {
                nextDue = MinDate(nextDue, due);
                continue;
            }

            FireReminder(reminder, config);
            reminder.LastTriggeredUtc = DateTimeOffset.UtcNow;
            dirty = true;

            var next = ReminderRecurrenceCalculator.GetNextOccurrenceLocal(reminder, now, _programStartLocal);
            lock (_gate)
            {
                if (next.HasValue)
                {
                    _nextById[reminder.Id] = next.Value;
                    nextDue = MinDate(nextDue, next.Value);
                }
                else
                {
                    _nextById.Remove(reminder.Id);
                }
            }
        }

        if (dirty)
        {
            _configurationManager.SaveConfiguration(config);
        }

        if (!nextDue.HasValue)
        {
            return Timeout.InfiniteTimeSpan;
        }

        var delay = nextDue.Value - DateTime.Now;
        return delay <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(50) : delay;
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
        var toastMessage = StripRichTextTags(message);

        if (!_isSilentMode())
        {
            if (channels.Toast)
            {
                if (!_areNotificationsHidden())
                {
                    _toastHost.Show(
                        new ToastNotificationContent
                        {
                            Title = reminder.Title,
                            Body = toastMessage,
                            Scenario = ToastScenario.Reminder,
                            Duration = ToastDuration.Long,
                        }
                    );
                }
            }

            if (channels.Sound)
            {
                if (!_isReminderSoundMuted())
                {
                    var effectiveVolume = reminder.SoundVolumePercent ?? config.Settings.GlobalVolumePercent;
                    AudioPlayer.PlayNotificationAsync(volumePercent: effectiveVolume);
                }
            }

            if (channels.Popup && popup.Enabled)
            {
                if (!_areNotificationsHidden())
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

                            SignalWakeLoop();
                        }
                    );
                }
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

        SignalWakeLoop();
    }

    public void Dispose()
    {
        Stop();
        _wakeSignal.Dispose();
    }

    private async Task WaitForWakeOrDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay == Timeout.InfiniteTimeSpan)
        {
            await _wakeSignal.WaitAsync(cancellationToken);
            return;
        }

        var wakeTask = _wakeSignal.WaitAsync(cancellationToken);
        var delayTask = Task.Delay(delay, cancellationToken);
        await Task.WhenAny(wakeTask, delayTask);
    }

    private void SignalWakeLoop()
    {
        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Wake signal already pending.
        }
    }

    private static DateTime? MinDate(DateTime? current, DateTime candidate)
    {
        return current.HasValue && current.Value <= candidate ? current : candidate;
    }

    private static string StripRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return RichTagRegex.Replace(text, string.Empty);
    }
}
