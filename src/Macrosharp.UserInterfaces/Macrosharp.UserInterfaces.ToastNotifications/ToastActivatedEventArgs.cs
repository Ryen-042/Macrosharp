namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Event arguments for toast activation (body click or action button click).
/// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
/// </summary>
public sealed class ToastActivatedEventArgs : EventArgs
{
    /// <summary>
    /// The activation argument string. For body clicks this is empty.
    /// For action button clicks this is the button's <see cref="ToastAction.Argument"/> value.
    /// </summary>
    public string Argument { get; }

    internal ToastActivatedEventArgs(string argument) => Argument = argument;
}
