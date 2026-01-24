using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

internal static class ImageEditorIO
{
    public static bool TryLoadFromFile(string path, out ImageBuffer? buffer)
    {
        buffer = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        using var bitmap = new Bitmap(path);
        buffer = ToImageBuffer(bitmap);
        return true;
    }

    private const uint ClipboardBitmap = 2;
    private const uint ClipboardUnicodeText = 13;

    public static unsafe bool TryLoadFromClipboard(out ImageBuffer? buffer)
    {
        buffer = null;
        if (!PInvoke.OpenClipboard(HWND.Null))
        {
            return false;
        }

        try
        {
            if (PInvoke.IsClipboardFormatAvailable(ClipboardBitmap))
            {
                HANDLE handle = PInvoke.GetClipboardData(ClipboardBitmap);
                if (handle != HANDLE.Null)
                {
                    nint hBitmap = (nint)handle.Value;
                    using var image = Image.FromHbitmap(hBitmap);
                    using var bitmap = new Bitmap(image);
                    buffer = ToImageBuffer(bitmap);
                    PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
                    return true;
                }
            }

            if (PInvoke.IsClipboardFormatAvailable(ClipboardUnicodeText))
            {
                HANDLE handle = PInvoke.GetClipboardData(ClipboardUnicodeText);
                if (handle != HANDLE.Null)
                {
                    string? path = GetUnicodeString(handle);
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        using var bitmap = new Bitmap(path);
                        buffer = ToImageBuffer(bitmap);
                        return true;
                    }
                }
            }
        }
        finally
        {
            PInvoke.CloseClipboard();
        }

        return false;
    }

    public static ImageBuffer ToImageBuffer(Bitmap bitmap)
    {
        Bitmap formatted = bitmap;
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
        {
            formatted = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb);
        }

        try
        {
            var rect = new Rectangle(0, 0, formatted.Width, formatted.Height);
            BitmapData data = formatted.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = formatted.Width;
                int height = formatted.Height;
                int stride = data.Stride;
                var buffer = new ImageBuffer(width, height);
                byte[] bytes = new byte[stride * height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

                for (int y = 0; y < height; y++)
                {
                    int srcOffset = y * stride;
                    int destOffset = y * width * 4;
                    Buffer.BlockCopy(bytes, srcOffset, buffer.Pixels, destOffset, width * 4);
                }

                return buffer;
            }
            finally
            {
                formatted.UnlockBits(data);
            }
        }
        finally
        {
            if (!ReferenceEquals(formatted, bitmap))
            {
                formatted.Dispose();
            }
        }
    }

    private static unsafe string? GetUnicodeString(HANDLE handle)
    {
        var global = new HGLOBAL(handle.Value);
        void* ptr = PInvoke.GlobalLock(global);
        if (ptr == null)
        {
            return null;
        }

        try
        {
            nuint size = PInvoke.GlobalSize(global);
            if (size == 0)
            {
                return null;
            }

            return Marshal.PtrToStringUni((nint)ptr);
        }
        finally
        {
            PInvoke.GlobalUnlock(global);
        }
    }
}
