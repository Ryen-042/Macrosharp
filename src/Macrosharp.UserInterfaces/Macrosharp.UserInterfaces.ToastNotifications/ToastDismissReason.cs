namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Indicates why a toast notification was dismissed.
/// </summary>
public enum ToastDismissReason
{
    /// <summary>The user explicitly dismissed the toast.</summary>
    UserCanceled,

    /// <summary>The application hid the toast programmatically.</summary>
    ApplicationHidden,

    /// <summary>The toast timed out and was automatically dismissed.</summary>
    TimedOut
}
