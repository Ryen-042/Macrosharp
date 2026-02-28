namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Controls toast notification behavior and presentation style.
/// </summary>
public enum ToastScenario
{
    /// <summary>Normal toast with standard dismissal behavior.</summary>
    Default,

    /// <summary>Alarm-style toast that stays on screen and may loop audio.</summary>
    Alarm,

    /// <summary>Reminder-style toast that persists until the user interacts with it.</summary>
    Reminder,

    /// <summary>Incoming-call-style toast with call UI layout.</summary>
    IncomingCall
}
