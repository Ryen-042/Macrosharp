namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// Encapsulates a single input event (mouse or keyboard) with all necessary context.
/// Includes both screen and image coordinates, modifier keys, and mouse wheel delta.
/// </summary>
public readonly record struct EditorInput(IntPoint ScreenPoint, IntPoint ImagePoint, ModifierState Modifiers, MouseButton Button, int WheelDelta);

/// <summary>
/// Converts between screen coordinates (viewport space) and image coordinates (image space).
/// Accounts for zoom level, pan offset, and viewport origin.
/// </summary>
public readonly record struct ViewTransform(double Zoom, int PanX, int PanY, int OriginX, int OriginY, int ViewportWidth, int ViewportHeight)
{
    /// <summary>
    /// Converts a point in screen coordinates (pixels on viewport) to image coordinates.
    /// </summary>
    public IntPoint ScreenToImage(IntPoint screen)
    {
        int ix = (int)Math.Floor((screen.X - OriginX - PanX) / Zoom);
        int iy = (int)Math.Floor((screen.Y - OriginY - PanY) / Zoom);
        return new IntPoint(ix, iy);
    }

    /// <summary>
    /// Converts a point in image coordinates (pixel position on the image) to screen coordinates.
    /// </summary>
    public IntPoint ImageToScreen(IntPoint image)
    {
        int sx = OriginX + PanX + (int)Math.Round(image.X * Zoom);
        int sy = OriginY + PanY + (int)Math.Round(image.Y * Zoom);
        return new IntPoint(sx, sy);
    }
}

/// <summary>
/// A simple 2D integer point.
/// </summary>
public readonly record struct IntPoint(int X, int Y);

/// <summary>
/// A rectangle defined by left, top, right, and bottom coordinates.
/// Provides normalization (ensuring left<right, top<bottom) and clamping operations.
/// </summary>
public readonly record struct IntRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    /// <summary>
    /// Ensures the rectangle has positive width and height by swapping coordinates if needed.
    /// </summary>
    public IntRect Normalize()
    {
        int left = Math.Min(Left, Right);
        int right = Math.Max(Left, Right);
        int top = Math.Min(Top, Bottom);
        int bottom = Math.Max(Top, Bottom);
        return new IntRect(left, top, right, bottom);
    }

    /// <summary>
    /// Clamps all coordinates to the specified bounds.
    /// </summary>
    public IntRect Clamp(int minX, int minY, int maxX, int maxY)
    {
        int left = Math.Clamp(Left, minX, maxX);
        int top = Math.Clamp(Top, minY, maxY);
        int right = Math.Clamp(Right, minX, maxX);
        int bottom = Math.Clamp(Bottom, minY, maxY);
        return new IntRect(left, top, right, bottom);
    }
}

/// <summary>
/// An immutable snapshot of image pixel data for undo/redo history.
/// </summary>
internal readonly record struct ImageSnapshot(int Width, int Height, int[] Pixels)
{
    public static ImageSnapshot From(ImageBuffer buffer)
    {
        int[] pixels = new int[buffer.Pixels.Length];
        Array.Copy(buffer.Pixels, pixels, pixels.Length);
        return new ImageSnapshot(buffer.Width, buffer.Height, pixels);
    }

    public ImageBuffer ToBuffer()
    {
        var buffer = new ImageBuffer(Width, Height);
        Array.Copy(Pixels, buffer.Pixels, Pixels.Length);
        return buffer;
    }
}

/// <summary>
/// Keyboard and mouse modifier keys that can be pressed during input events.
/// </summary>
[Flags]
public enum ModifierState
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
}

/// <summary>
/// Mouse buttons that can generate input events.
/// </summary>
public enum MouseButton
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
}

/// <summary>
/// The different tools available in the image editor.
/// </summary>
public enum ToolKind
{
    Draw, // Free-form drawing
    Crop, // Image cropping
    ColorPicker, // Color sampling
    Pan, // View panning
}
