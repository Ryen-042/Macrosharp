namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class ImageBuffer
{
    public int Width { get; }
    public int Height { get; }
    public int[] Pixels { get; }

    public ImageBuffer(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Pixels = new int[Width * Height];
    }

    public void Fill(int argb)
    {
        Array.Fill(Pixels, argb);
    }

    public void CopyFrom(ImageBuffer other)
    {
        if (other.Width != Width || other.Height != Height)
        {
            throw new InvalidOperationException("Image buffer size mismatch.");
        }

        Array.Copy(other.Pixels, Pixels, Pixels.Length);
    }

    public ImageBuffer Clone()
    {
        var clone = new ImageBuffer(Width, Height);
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }

    public ImageBuffer Crop(IntRect rect)
    {
        rect = rect.Normalize();
        int width = rect.Width;
        int height = rect.Height;
        var cropped = new ImageBuffer(width, height);
        for (int y = 0; y < height; y++)
        {
            int sourceRow = (rect.Top + y) * Width;
            int destRow = y * width;
            Array.Copy(Pixels, sourceRow + rect.Left, cropped.Pixels, destRow, width);
        }

        return cropped;
    }

    public void SetPixel(int x, int y, int argb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        Pixels[y * Width + x] = argb;
    }

    public int GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return 0;
        }

        return Pixels[y * Width + x];
    }
}
