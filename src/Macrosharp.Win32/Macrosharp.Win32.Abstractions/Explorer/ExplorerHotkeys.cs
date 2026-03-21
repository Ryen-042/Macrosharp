using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Win32.Abstractions.Explorer;

/// <summary>
/// Helper methods for Explorer-focused hotkey conditions and actions.
/// </summary>
public static class ExplorerHotkeys
{
    // Explorer window class names
    private static readonly HashSet<string> ExplorerClassNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CabinetWClass", // File Explorer windows
        "ExploreWClass", // Older Explorer windows
        "Progman", // Desktop (Program Manager)
        "WorkerW", // Desktop worker window
    };

    /// <summary>
    /// Returns true if the foreground window is a File Explorer or Desktop window.
    /// </summary>
    public static bool IsExplorerOrDesktopFocused()
    {
        try
        {
            HWND hwnd = PInvoke.GetForegroundWindow();
            if (hwnd == HWND.Null)
                return false;

            string className = WindowFinder.GetWindowClassName(hwnd);
            return ExplorerClassNames.Contains(className);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the full paths of the selected items in the active Explorer window.
    /// Returns an empty list if no items are selected or if the foreground window is not Explorer.
    /// </summary>
    public static IReadOnlyList<string> GetSelectedFilePaths()
    {
        try
        {
            HWND hwnd = PInvoke.GetForegroundWindow();
            return ExplorerShellManager.GetSelectedItems(hwnd);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Converts selected image files to .ico format using System.Drawing.
    /// </summary>
    public static void ConvertSelectedImagesToIco()
    {
        try
        {
            HWND hwnd = PInvoke.GetForegroundWindow();
            var items = ExplorerShellManager.GetSelectedItems(hwnd);
            if (items.Count == 0)
                return;

            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            var imageFiles = items.Where(f => imageExtensions.Contains(Path.GetExtension(f))).ToList();
            if (imageFiles.Count == 0)
                return;

            Infrastructure.AudioPlayer.PlayStartAsync();

            foreach (string imagePath in imageFiles)
            {
                string icoPath = Path.ChangeExtension(imagePath, ".ico");
                if (File.Exists(icoPath))
                    continue;

                try
                {
                    ConvertToIco(imagePath, icoPath);
                }
                catch
                {
                    // Skip files that fail to convert
                }
            }

            Infrastructure.AudioPlayer.PlaySuccessAsync();
        }
        catch
        {
            Infrastructure.AudioPlayer.PlayFailure();
        }
    }

    private static void ConvertToIco(string inputPath, string outputPath)
    {
        using var bitmap = new System.Drawing.Bitmap(inputPath);

        // Resize to 256×256 (max ICO dimension) maintaining aspect ratio
        int size = 256;
        int width,
            height;
        if (bitmap.Width >= bitmap.Height)
        {
            width = size;
            height = (int)((float)bitmap.Height / bitmap.Width * size);
        }
        else
        {
            height = size;
            width = (int)((float)bitmap.Width / bitmap.Height * size);
        }

        using var resized = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(resized))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(bitmap, 0, 0, width, height);
        }

        // Write as ICO format
        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        WriteIco(fs, resized);
    }

    private static void WriteIco(Stream stream, System.Drawing.Bitmap bitmap)
    {
        // Convert bitmap to PNG bytes for the ICO entry
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
        byte[] pngData = pngStream.ToArray();

        using var writer = new BinaryWriter(stream);

        // ICONDIR header
        writer.Write((ushort)0); // Reserved
        writer.Write((ushort)1); // Type: 1 = ICO
        writer.Write((ushort)1); // Count: 1 image

        // ICONDIRENTRY
        writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
        writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
        writer.Write((byte)0); // Color palette count
        writer.Write((byte)0); // Reserved
        writer.Write((ushort)1); // Color planes
        writer.Write((ushort)32); // Bits per pixel
        writer.Write((uint)pngData.Length); // Image data size
        writer.Write((uint)22); // Offset to image data (6 header + 16 entry)

        // Image data (PNG)
        writer.Write(pngData);
    }

    /// <summary>
    /// Converts selected MP3 files to WAV using ffmpeg via GenericFileConverter.
    /// </summary>
    public static void ConvertSelectedMp3ToWav()
    {
        ExplorerFileAutomation.GenericFileConverter(
            new[] { ".mp3" },
            (input, output) =>
            {
                var ffmpeg = new System.Diagnostics.ProcessStartInfo("ffmpeg") { UseShellExecute = false, CreateNoWindow = true };
                ffmpeg.ArgumentList.Add("-loglevel");
                ffmpeg.ArgumentList.Add("error");
                ffmpeg.ArgumentList.Add("-hide_banner");
                ffmpeg.ArgumentList.Add("-nostats");
                ffmpeg.ArgumentList.Add("-i");
                ffmpeg.ArgumentList.Add(input);
                ffmpeg.ArgumentList.Add(output);

                using var process = System.Diagnostics.Process.Start(ffmpeg);
                process?.WaitForExit();
            },
            newExtension: ".wav"
        );
    }
}
