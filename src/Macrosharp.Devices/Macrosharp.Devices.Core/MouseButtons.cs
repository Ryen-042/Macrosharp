namespace Macrosharp.Devices.Core;

/// <summary>
/// Flags enum representing mouse buttons. Supports combinations for multi-button bindings.
/// </summary>
[Flags]
public enum MouseButtons
{
    /// <summary>No mouse buttons.</summary>
    None = 0,

    /// <summary>The left mouse button.</summary>
    Left = 1 << 0,

    /// <summary>The right mouse button.</summary>
    Right = 1 << 1,

    /// <summary>The middle mouse button (wheel click).</summary>
    Middle = 1 << 2,

    /// <summary>The first extended button (XButton1, typically "back").</summary>
    XButton1 = 1 << 3,

    /// <summary>The second extended button (XButton2, typically "forward").</summary>
    XButton2 = 1 << 4,
}

/// <summary>
/// Represents the direction of a scroll event.
/// </summary>
public enum ScrollDirection
{
    /// <summary>Vertical scroll (standard mouse wheel).</summary>
    Vertical = 0,

    /// <summary>Horizontal scroll (tilt wheel or dedicated horizontal scroll).</summary>
    Horizontal = 1,
}
