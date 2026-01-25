namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// A 2D pixel buffer stored as a 1D array of ARGB pixels.
/// Provides pixel access and common image operations like copy, clone, crop, and fill.
/// Pixel format: 32-bit ARGB (A=bits[24-31], R=bits[16-23], G=bits[8-15], B=bits[0-7])
/// Layout: Row-major (pixels are stored left-to-right, top-to-bottom)
/// </summary>
public sealed class ImageBuffer
{
    public int Width { get; }
    public int Height { get; }
    public int[] Pixels { get; }

    /// <summary>
    /// Creates a new image buffer with the specified dimensions.
    /// Width and height are clamped to minimum of 1.
    /// </summary>
    public ImageBuffer(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Pixels = new int[Width * Height];
    }

    /// <summary>
    /// Fills the entire buffer with the specified ARGB color.
    /// </summary>
    public void Fill(int argb)
    {
        Array.Fill(Pixels, argb);
    }

    /// <summary>
    /// Copies all pixel data from another buffer of the same size.
    /// Throws if dimensions don't match.
    /// </summary>
    public void CopyFrom(ImageBuffer other)
    {
        if (other.Width != Width || other.Height != Height)
        {
            throw new InvalidOperationException("Image buffer size mismatch.");
        }

        Array.Copy(other.Pixels, Pixels, Pixels.Length);
    }

    /// <summary>
    /// Creates a deep copy of this buffer.
    /// </summary>
    public ImageBuffer Clone()
    {
        var clone = new ImageBuffer(Width, Height);
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }

    /// <summary>
    /// Extracts a rectangular region and returns it as a new buffer.
    /// </summary>
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

    /// <summary>
    /// Sets a single pixel value at the specified coordinates.
    /// Silently ignores out-of-bounds access.
    /// </summary>
    public void SetPixel(int x, int y, int argb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        Pixels[y * Width + x] = argb;
    }

    /// <summary>
    /// Gets a single pixel value at the specified coordinates.
    /// Returns 0 for out-of-bounds access.
    /// </summary>
    public int GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return 0;
        }

        return Pixels[y * Width + x];
    }
}
