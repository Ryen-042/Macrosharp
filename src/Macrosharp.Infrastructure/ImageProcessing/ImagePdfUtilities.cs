using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Macrosharp.Infrastructure.ImageProcessing;

public static class ImagePdfUtilities
{
    public static IReadOnlyList<string> FilterAndPrepareImages(IEnumerable<string> imagePaths, string outputDirectory, bool resizeMode, int targetWidth, int widthThreshold, int minWidth, int minHeight, out string? tempDirectory)
    {
        tempDirectory = null;
        var prepared = new List<string>();
        if (imagePaths == null)
            return prepared;

        foreach (string path in imagePaths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;

            if (!TryGetImageSize(path, out int width, out int height))
                continue;

            if (width < minWidth || height < minHeight)
                continue;

            if (!resizeMode)
            {
                prepared.Add(path);
                continue;
            }

            if (tempDirectory == null)
            {
                tempDirectory = Path.Combine(outputDirectory, "temp_resized_images");
                Directory.CreateDirectory(tempDirectory);
            }

            string outputPath = Path.Combine(tempDirectory, Path.GetFileName(path));
            if (width < widthThreshold)
            {
                ResizeImageToWidth(path, outputPath, targetWidth);
            }
            else
            {
                File.Copy(path, outputPath, overwrite: true);
            }

            prepared.Add(outputPath);
        }

        return prepared;
    }

    public static void CreatePdfFromImages(IReadOnlyList<string> imagePaths, string outputPath)
    {
        if (imagePaths == null || imagePaths.Count == 0)
            throw new ArgumentException("No images provided.", nameof(imagePaths));

        using var document = new PdfDocument();
        foreach (string path in imagePaths)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var image = XImage.FromStream(() => new MemoryStream(bytes));
            var page = document.AddPage();

            double dpiX = image.HorizontalResolution > 0 ? image.HorizontalResolution : 96.0;
            double dpiY = image.VerticalResolution > 0 ? image.VerticalResolution : 96.0;

            page.Width = image.PixelWidth * 72.0 / dpiX;
            page.Height = image.PixelHeight * 72.0 / dpiY;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(image, 0, 0, page.Width, page.Height);
        }

        document.Save(outputPath);
    }

    public static bool TryGetImageSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            using var image = Image.FromFile(path);
            width = image.Width;
            height = image.Height;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ResizeImageToWidth(string inputPath, string outputPath, int targetWidth)
    {
        using var image = Image.FromFile(inputPath);
        if (image.Width <= 0 || image.Height <= 0)
            throw new InvalidOperationException("Invalid image dimensions.");

        double ratio = (double)targetWidth / image.Width;
        int newHeight = Math.Max(1, (int)Math.Round(image.Height * ratio));

        using var resized = new Bitmap(targetWidth, newHeight);
        using var graphics = Graphics.FromImage(resized);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        graphics.DrawImage(image, 0, 0, targetWidth, newHeight);

        ImageFormat format = GetImageFormatFromExtension(Path.GetExtension(outputPath));
        resized.Save(outputPath, format);
    }

    public static IOrderedEnumerable<string> OrderByNaturalFileName(IEnumerable<string> paths)
    {
        return paths.OrderBy(path => Path.GetFileName(path), NaturalStringComparer.Instance).ThenBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    public static void CleanupTempDirectory(string? tempDirectory)
    {
        if (string.IsNullOrWhiteSpace(tempDirectory) || !Directory.Exists(tempDirectory))
            return;

        try
        {
            foreach (string file in Directory.GetFiles(tempDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup failures.
                }
            }

            Directory.Delete(tempDirectory, recursive: false);
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }

    private static ImageFormat GetImageFormatFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return ImageFormat.Png;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            ".tif" or ".tiff" => ImageFormat.Tiff,
            _ => ImageFormat.Png,
        };
    }

    public sealed class NaturalStringComparer : IComparer<string>
    {
        public static NaturalStringComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            int ix = 0;
            int iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                char cx = x[ix];
                char cy = y[iy];

                if (char.IsDigit(cx) && char.IsDigit(cy))
                {
                    long vx = 0;
                    long vy = 0;
                    int startX = ix;
                    int startY = iy;

                    while (ix < x.Length && char.IsDigit(x[ix]))
                    {
                        vx = (vx * 10) + (x[ix] - '0');
                        ix++;
                    }

                    while (iy < y.Length && char.IsDigit(y[iy]))
                    {
                        vy = (vy * 10) + (y[iy] - '0');
                        iy++;
                    }

                    if (vx != vy)
                        return vx < vy ? -1 : 1;

                    int lenX = ix - startX;
                    int lenY = iy - startY;
                    if (lenX != lenY)
                        return lenX < lenY ? -1 : 1;

                    continue;
                }

                int charCompare = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
                if (charCompare != 0)
                    return charCompare;

                ix++;
                iy++;
            }

            if (ix < x.Length)
                return 1;
            if (iy < y.Length)
                return -1;
            return 0;
        }
    }
}
