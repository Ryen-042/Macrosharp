namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Immutable data model representing a toast notification payload.
/// Use <c>init</c>-only properties to configure title, body, images, actions, and more.
/// </summary>
public sealed class ToastNotificationContent
{
    /// <summary>Required. The toast headline.</summary>
    public required string Title { get; init; }

    /// <summary>Required. The toast body text.</summary>
    public required string Body { get; init; }

    /// <summary>
    /// Optional path to an image file displayed as the app logo override
    /// (square, shown on the left of the toast). Supports .png, .jpg, .ico.
    /// </summary>
    public string? AppLogoPath { get; init; }

    /// <summary>
    /// Optional path to a hero image (wide banner shown at the top of the toast).
    /// </summary>
    public string? HeroImagePath { get; init; }

    /// <summary>
    /// Optional attribution text displayed below the body in smaller font
    /// (e.g. "via Macrosharp").
    /// </summary>
    public string? Attribution { get; init; }

    /// <summary>
    /// Optional custom timestamp displayed on the toast instead of the delivery time.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    /// Controls toast behavior. Default = normal dismissal; Alarm/Reminder produce
    /// persistent/looping toasts; IncomingCall shows call-style UI.
    /// </summary>
    public ToastScenario Scenario { get; init; } = ToastScenario.Default;

    /// <summary>
    /// Controls how long the toast remains visible.
    /// </summary>
    public ToastDuration Duration { get; init; } = ToastDuration.Default;

    /// <summary>
    /// Optional action buttons (max 5). Each button has a label and an activation
    /// argument string routed to the <see cref="ToastNotificationHost.Activated"/> event.
    /// </summary>
    public IReadOnlyList<ToastAction>? Actions { get; init; }

    /// <summary>
    /// Optional progress bar for long-running operations.
    /// </summary>
    public ToastProgressBar? ProgressBar { get; init; }
}
