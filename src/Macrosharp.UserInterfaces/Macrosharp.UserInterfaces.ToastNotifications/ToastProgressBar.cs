namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Represents an optional progress bar inside a toast notification.
/// </summary>
public sealed class ToastProgressBar
{
    /// <summary>Title text displayed above the progress bar.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// Progress value between 0.0 and 1.0, or <c>null</c> for an indeterminate progress bar.
    /// </summary>
    public double? Value { get; init; }

    /// <summary>Status text displayed below the progress bar (e.g. "Downloading...").</summary>
    public string? Status { get; init; }

    /// <summary>
    /// Value string override (e.g. "3/10 files"). If <c>null</c>, the percentage is shown.
    /// </summary>
    public string? ValueStringOverride { get; init; }
}
