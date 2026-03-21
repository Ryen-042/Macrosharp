namespace Macrosharp.UserInterfaces.PopupNotifications;

public enum PopupNotificationPosition
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

public sealed class PopupNotificationContent
{
    public string Title { get; set; } = "Notification";
    public string Message { get; set; } = string.Empty;
}

public sealed class PopupNotificationOptions
{
    public PopupNotificationPosition Position { get; set; } = PopupNotificationPosition.BottomRight;
    public int? MonitorIndex { get; set; }
    public int DurationSeconds { get; set; } = 10;
    public int OpacityPercent { get; set; } = 70;
    public List<int> SnoozeMinutes { get; set; } = new() { 5, 10, 15 };
}

public enum PopupNotificationAction
{
    Timeout,
    Dismiss,
    Snooze,
}

public sealed class PopupNotificationResult
{
    public PopupNotificationAction Action { get; init; }
    public int SnoozeMinutes { get; init; }
}
