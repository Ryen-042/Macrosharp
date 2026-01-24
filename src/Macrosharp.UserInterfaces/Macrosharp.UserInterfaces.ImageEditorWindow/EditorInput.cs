namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public readonly record struct EditorInput(IntPoint ScreenPoint, IntPoint ImagePoint, ModifierState Modifiers, MouseButton Button, int WheelDelta);

public readonly record struct ViewTransform(double Zoom, int PanX, int PanY, int OriginX, int OriginY, int ViewportWidth, int ViewportHeight)
{
    public IntPoint ScreenToImage(IntPoint screen)
    {
        int ix = (int)Math.Floor((screen.X - OriginX - PanX) / Zoom);
        int iy = (int)Math.Floor((screen.Y - OriginY - PanY) / Zoom);
        return new IntPoint(ix, iy);
    }
}

public readonly record struct IntPoint(int X, int Y);

public readonly record struct IntRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public IntRect Normalize()
    {
        int left = Math.Min(Left, Right);
        int right = Math.Max(Left, Right);
        int top = Math.Min(Top, Bottom);
        int bottom = Math.Max(Top, Bottom);
        return new IntRect(left, top, right, bottom);
    }

    public IntRect Clamp(int minX, int minY, int maxX, int maxY)
    {
        int left = Math.Clamp(Left, minX, maxX);
        int top = Math.Clamp(Top, minY, maxY);
        int right = Math.Clamp(Right, minX, maxX);
        int bottom = Math.Clamp(Bottom, minY, maxY);
        return new IntRect(left, top, right, bottom);
    }
}

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

[Flags]
public enum ModifierState
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
}

public enum MouseButton
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
}

public enum ToolKind
{
    Draw,
    Crop,
    ColorPicker,
    Pan,
}
