namespace Macrosharp.UserInterfaces.Reminders;

public enum ReminderRecurrenceKind
{
    Once,
    EveryInterval,
    Daily,
    Weekly,
    MonthlyDayOfMonth,
    MonthlyNthWeekday,
}

public enum ReminderIntervalAnchor
{
    ProgramStart,
    ExplicitStart,
}

public enum ReminderMissedPolicy
{
    Skip,
    FireWithinGraceWindow,
    FireAllMissed,
}

public enum ReminderPopupPosition
{
    TopRight,
    TopCenter,
    TopLeft,
    MiddleRight,
    Center,
    MiddleLeft,
    BottomRight,
    BottomCenter,
    BottomLeft,
}

public sealed class ReminderConfiguration
{
    public int Version { get; set; } = 1;
    public ReminderSettings Settings { get; set; } = new();
    public List<ReminderDefinition> Reminders { get; set; } = new();
}

public sealed class ReminderSettings
{
    public bool Enabled { get; set; } = true;
    public bool LocalTimeOnly { get; set; } = true;
    public ReminderMissedPolicy MissedPolicy { get; set; } = ReminderMissedPolicy.Skip;
    public int StartupGraceMinutes { get; set; } = 0;
    public int GlobalVolumePercent { get; set; } = 100;
    public ReminderChannels DefaultChannels { get; set; } = new();
    public ReminderPopupOptions PopupDefaults { get; set; } = new();
}

public sealed class ReminderChannels
{
    public bool Toast { get; set; } = true;
    public bool Popup { get; set; } = true;
    public bool Sound { get; set; } = false;
}

public sealed class ReminderPopupOptions
{
    public bool Enabled { get; set; } = true;
    public ReminderPopupPosition Position { get; set; } = ReminderPopupPosition.BottomRight;
    public int? MonitorIndex { get; set; }
    public int DurationSeconds { get; set; } = 10;
    public int OpacityPercent { get; set; } = 70;
    public List<int> SnoozeMinutes { get; set; } = new() { 5, 10, 15 };
}

public sealed class ReminderDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Reminder";
    public string Message { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int? SoundVolumePercent { get; set; }
    public ReminderChannels? Channels { get; set; }
    public ReminderPopupOptions? Popup { get; set; }
    public ReminderRecurrence Recurrence { get; set; } = new();

    // Runtime metadata persisted for dedup and resume behavior.
    public DateTimeOffset? LastTriggeredUtc { get; set; }

    // Indicates monthly day overflow was clamped to month end.
    public bool LastTriggerWasMonthEndClamp { get; set; }
}

public sealed class ReminderRecurrence
{
    public ReminderRecurrenceKind Kind { get; set; } = ReminderRecurrenceKind.Daily;

    // For once or explicit interval anchor.
    public string? StartDate { get; set; } // yyyy-MM-dd

    // For once/daily/weekly/monthly formats.
    public string? Time { get; set; } // HH:mm

    // For everyInterval (ab:cd:ef == HH:mm:ss)
    public string? Interval { get; set; }
    public ReminderIntervalAnchor Anchor { get; set; } = ReminderIntervalAnchor.ProgramStart;

    // For weekly recurrence.
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    // For monthly day-of-month recurrence.
    public int? DayOfMonth { get; set; }

    // For monthly nth-weekday recurrence.
    public int? NthWeek { get; set; }

    public DayOfWeek? NthWeekday { get; set; }
}

public enum ReminderPopupAction
{
    Timeout,
    Dismiss,
    Snooze,
}

public sealed class ReminderPopupResult
{
    public ReminderPopupAction Action { get; init; }
    public int SnoozeMinutes { get; init; }
}
