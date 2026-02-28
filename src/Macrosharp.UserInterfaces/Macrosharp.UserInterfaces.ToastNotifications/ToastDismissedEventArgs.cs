namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Event arguments raised when a toast notification is dismissed.
/// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
/// </summary>
public sealed class ToastDismissedEventArgs : EventArgs
{
    /// <summary>The reason the toast was dismissed.</summary>
    public ToastDismissReason Reason { get; }

    internal ToastDismissedEventArgs(ToastDismissReason reason) => Reason = reason;
}
