namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Event arguments raised when a toast notification fails to display.
/// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
/// </summary>
public sealed class ToastFailedEventArgs : EventArgs
{
    /// <summary>The exception that caused the toast to fail.</summary>
    public Exception Error { get; }

    internal ToastFailedEventArgs(Exception error) => Error = error;
}
