using Macrosharp.UserInterfaces.PopupNotifications;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderPopupHost
{
    private readonly PopupNotificationHost _popupHost = new();

    public void Show(ReminderDefinition reminder, ReminderPopupOptions popupOptions, Action<ReminderPopupResult> onResult)
    {
        var content = new PopupNotificationContent
        {
            Title = reminder.Title,
            Message = reminder.Message,
        };

        var options = MapOptions(popupOptions);
        _popupHost.Show(content, options, result => onResult(MapResult(result)));
    }

    private static PopupNotificationOptions MapOptions(ReminderPopupOptions popupOptions)
    {
        return new PopupNotificationOptions
        {
            Position = MapPosition(popupOptions.Position),
            MonitorIndex = popupOptions.MonitorIndex,
            DurationSeconds = popupOptions.DurationSeconds,
            OpacityPercent = popupOptions.OpacityPercent,
            SnoozeMinutes = popupOptions.SnoozeMinutes.ToList(),
        };
    }

    private static PopupNotificationPosition MapPosition(ReminderPopupPosition position)
    {
        return position switch
        {
            ReminderPopupPosition.TopRight => PopupNotificationPosition.TopRight,
            ReminderPopupPosition.TopCenter => PopupNotificationPosition.TopCenter,
            ReminderPopupPosition.TopLeft => PopupNotificationPosition.TopLeft,
            ReminderPopupPosition.MiddleRight => PopupNotificationPosition.MiddleRight,
            ReminderPopupPosition.Center => PopupNotificationPosition.Center,
            ReminderPopupPosition.MiddleLeft => PopupNotificationPosition.MiddleLeft,
            ReminderPopupPosition.BottomRight => PopupNotificationPosition.BottomRight,
            ReminderPopupPosition.BottomCenter => PopupNotificationPosition.BottomCenter,
            _ => PopupNotificationPosition.BottomLeft,
        };
    }

    private static ReminderPopupResult MapResult(PopupNotificationResult result)
    {
        return new ReminderPopupResult
        {
            Action = result.Action switch
            {
                PopupNotificationAction.Timeout => ReminderPopupAction.Timeout,
                PopupNotificationAction.Dismiss => ReminderPopupAction.Dismiss,
                _ => ReminderPopupAction.Snooze,
            },
            SnoozeMinutes = result.SnoozeMinutes,
        };
    }
}
