using Macrosharp.UserInterfaces.DynamicWindow;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderCrudService
{
    private readonly ReminderConfigurationManager _configurationManager;

    public ReminderCrudService(ReminderConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public void AddReminderInteractively()
    {
        var window = new SimpleWindow("Add Reminder", labelWidth: 150, inputFieldWidth: 260);
        window.CreateDynamicInputWindow(
            new[]
            {
                "Title",
                "Message ([b]/[i]/[color])",
                "Recurrence (once|interval|daily|weekly|monthlyDay|monthlyNth)",
                "Date (yyyy-MM-dd)",
                "Time (HH:mm)",
                "Interval (HH:mm:ss)",
                "Week days (Mon,Tue)",
                "Day of month",
                "Nth week (1-5)",
                "Nth weekday",
                "Channels (toast,popup,sound)",
                "Popup position",
            },
            new[] { "Health Reminder", "[b]Stand up[/b] and stretch", "daily", "", "10:00", "00:30:00", "Mon,Tue,Wed,Thu,Fri", "1", "1", "Monday", "toast,popup", "bottomRight" }
        );

        if (window.userInputs.Count < 12)
        {
            return;
        }

        var reminder = ParseReminderFromInput(window.userInputs);
        if (reminder is null)
        {
            Console.WriteLine("Failed to add reminder: invalid values.");
            return;
        }

        var config = _configurationManager.CurrentConfiguration;
        config.Reminders.Add(reminder);
        _configurationManager.SaveConfiguration(config);
        Console.WriteLine($"Reminder added: {reminder.Title} ({reminder.Id}).");
    }

    public void EditReminderInteractively()
    {
        var config = _configurationManager.CurrentConfiguration;
        if (config.Reminders.Count == 0)
        {
            Console.WriteLine("No reminders available to edit.");
            return;
        }

        PrintReminderList(config.Reminders);

        var selector = new SimpleWindow("Edit Reminder", labelWidth: 150, inputFieldWidth: 260);
        selector.CreateDynamicInputWindow(new[] { "Reminder Id" }, new[] { config.Reminders[0].Id });
        if (selector.userInputs.Count == 0)
        {
            return;
        }

        var id = selector.userInputs[0].Trim();
        var existing = config.Reminders.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Console.WriteLine($"Reminder not found: {id}");
            return;
        }

        var editWindow = new SimpleWindow("Edit Reminder", labelWidth: 150, inputFieldWidth: 260);
        editWindow.CreateDynamicInputWindow(
            new[] { "Title", "Message ([b]/[i]/[color])", "Enabled (true|false)", "Time (HH:mm)", "Channels (toast,popup,sound)", "Popup position", "Popup duration (sec)" },
            new[]
            {
                existing.Title,
                existing.Message,
                existing.Enabled.ToString(),
                existing.Recurrence.Time ?? string.Empty,
                SerializeChannels(existing.Channels ?? config.Settings.DefaultChannels),
                (existing.Popup ?? config.Settings.PopupDefaults).Position.ToString(),
                (existing.Popup ?? config.Settings.PopupDefaults).DurationSeconds.ToString(),
            }
        );

        if (editWindow.userInputs.Count < 7)
        {
            return;
        }

        existing.Title = string.IsNullOrWhiteSpace(editWindow.userInputs[0]) ? existing.Title : editWindow.userInputs[0];
        existing.Message = editWindow.userInputs[1];
        existing.Enabled = bool.TryParse(editWindow.userInputs[2], out var enabled) ? enabled : existing.Enabled;
        if (!string.IsNullOrWhiteSpace(editWindow.userInputs[3]))
        {
            existing.Recurrence.Time = editWindow.userInputs[3];
        }

        existing.Channels = ParseChannels(editWindow.userInputs[4], config.Settings.DefaultChannels);
        existing.Popup ??= new ReminderPopupOptions();
        if (Enum.TryParse<ReminderPopupPosition>(editWindow.userInputs[5], true, out var position))
        {
            existing.Popup.Position = position;
        }

        if (int.TryParse(editWindow.userInputs[6], out var duration))
        {
            existing.Popup.DurationSeconds = Math.Clamp(duration, 3, 120);
        }

        _configurationManager.SaveConfiguration(config);
        Console.WriteLine($"Reminder updated: {existing.Title} ({existing.Id}).");
    }

    public void DeleteReminderInteractively()
    {
        var config = _configurationManager.CurrentConfiguration;
        if (config.Reminders.Count == 0)
        {
            Console.WriteLine("No reminders available to delete.");
            return;
        }

        PrintReminderList(config.Reminders);

        var selector = new SimpleWindow("Delete Reminder", labelWidth: 150, inputFieldWidth: 260);
        selector.CreateDynamicInputWindow(new[] { "Reminder Id" }, new[] { config.Reminders[0].Id });
        if (selector.userInputs.Count == 0)
        {
            return;
        }

        var id = selector.userInputs[0].Trim();
        var removed = config.Reminders.RemoveAll(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            Console.WriteLine($"Reminder not found: {id}");
            return;
        }

        _configurationManager.SaveConfiguration(config);
        Console.WriteLine($"Reminder deleted: {id}");
    }

    private static ReminderDefinition? ParseReminderFromInput(List<string> input)
    {
        var title = input[0].Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var recurrence = ParseRecurrence(input);
        if (recurrence is null)
        {
            return null;
        }

        var channels = ParseChannels(input[10], new ReminderChannels());
        var popupOptions = new ReminderPopupOptions();
        if (Enum.TryParse<ReminderPopupPosition>(input[11], true, out var position))
        {
            popupOptions.Position = position;
        }

        return new ReminderDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Message = input[1],
            Recurrence = recurrence,
            Channels = channels,
            Popup = popupOptions,
        };
    }

    private static ReminderRecurrence? ParseRecurrence(List<string> input)
    {
        var kindRaw = input[2].Trim();
        if (string.IsNullOrWhiteSpace(kindRaw))
        {
            return null;
        }

        var recurrence = new ReminderRecurrence();
        switch (kindRaw.ToLowerInvariant())
        {
            case "once":
                recurrence.Kind = ReminderRecurrenceKind.Once;
                recurrence.StartDate = input[3];
                recurrence.Time = input[4];
                break;
            case "interval":
                recurrence.Kind = ReminderRecurrenceKind.EveryInterval;
                recurrence.Interval = input[5];
                recurrence.Anchor = ReminderIntervalAnchor.ProgramStart;
                break;
            case "daily":
                recurrence.Kind = ReminderRecurrenceKind.Daily;
                recurrence.Time = input[4];
                break;
            case "weekly":
                recurrence.Kind = ReminderRecurrenceKind.Weekly;
                recurrence.Time = input[4];
                recurrence.DaysOfWeek = ParseDays(input[6]);
                break;
            case "monthlyday":
                recurrence.Kind = ReminderRecurrenceKind.MonthlyDayOfMonth;
                recurrence.Time = input[4];
                recurrence.DayOfMonth = int.TryParse(input[7], out var day) ? day : 1;
                break;
            case "monthlynth":
                recurrence.Kind = ReminderRecurrenceKind.MonthlyNthWeekday;
                recurrence.Time = input[4];
                recurrence.NthWeek = int.TryParse(input[8], out var nth) ? nth : 1;
                recurrence.NthWeekday = Enum.TryParse<DayOfWeek>(input[9], true, out var weekDay) ? weekDay : DayOfWeek.Monday;
                break;
            default:
                return null;
        }

        return recurrence;
    }

    private static List<DayOfWeek> ParseDays(string raw)
    {
        var values = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var days = new List<DayOfWeek>();
        foreach (var value in values)
        {
            if (Enum.TryParse<DayOfWeek>(value, true, out var day))
            {
                days.Add(day);
            }
        }

        return days;
    }

    private static ReminderChannels ParseChannels(string raw, ReminderChannels fallback)
    {
        var result = new ReminderChannels();
        var values = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length == 0)
        {
            return new ReminderChannels
            {
                Toast = fallback.Toast,
                Popup = fallback.Popup,
                Sound = fallback.Sound,
            };
        }

        foreach (var value in values)
        {
            if (value.Equals("toast", StringComparison.OrdinalIgnoreCase))
            {
                result.Toast = true;
            }
            else if (value.Equals("popup", StringComparison.OrdinalIgnoreCase))
            {
                result.Popup = true;
            }
            else if (value.Equals("sound", StringComparison.OrdinalIgnoreCase))
            {
                result.Sound = true;
            }
        }

        return result;
    }

    private static string SerializeChannels(ReminderChannels channels)
    {
        var values = new List<string>();
        if (channels.Toast)
        {
            values.Add("toast");
        }

        if (channels.Popup)
        {
            values.Add("popup");
        }

        if (channels.Sound)
        {
            values.Add("sound");
        }

        return string.Join(',', values);
    }

    private static void PrintReminderList(IEnumerable<ReminderDefinition> reminders)
    {
        Console.WriteLine("Existing reminders:");
        foreach (var reminder in reminders)
        {
            Console.WriteLine($"- {reminder.Id} | {reminder.Title} | {reminder.Recurrence.Kind}");
        }
    }
}
