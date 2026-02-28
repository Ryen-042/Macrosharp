namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Represents a single action button on a toast notification.
/// </summary>
public sealed class ToastAction
{
    /// <summary>Button label text displayed to the user.</summary>
    public required string Label { get; init; }

    /// <summary>
    /// Argument string passed to the <see cref="ToastNotificationHost.Activated"/> event
    /// when this button is clicked.
    /// </summary>
    public required string Argument { get; init; }
}
