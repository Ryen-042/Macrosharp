using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

internal static class ImageEditorIO
{
    private const uint ClipboardBitmap = 2; // CF_BITMAP
    private const uint ClipboardDib = 8; // CF_DIB
    private const uint ClipboardUnicodeText = 13; // CF_UNICODETEXT
    private const uint GmemMoveable = 0x0002;

    [DllImport("kernel32.dll", EntryPoint = "GlobalAlloc", SetLastError = true)]
    private static extern nint GlobalAllocManual(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", EntryPoint = "GlobalFree", SetLastError = true)]
    private static extern nint GlobalFreeManual(nint hMem);

    [DllImport("kernel32.dll", EntryPoint = "GlobalLock", SetLastError = true)]
    private static extern nint GlobalLockManual(nint hMem);

    [DllImport("kernel32.dll", EntryPoint = "GlobalUnlock", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlockManual(nint hMem);

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

    public static bool TryCopyToClipboard(ImageBuffer source)
    {
        if (source.Width <= 0 || source.Height <= 0)
        {
            return false;
        }

        using var bitmap = ToBitmap(source);
        nint hBitmap = bitmap.GetHbitmap();
        if (hBitmap == nint.Zero)
        {
            return false;
        }

        nint hDib = CreateClipboardDib(source);
        if (hDib == nint.Zero)
        {
            PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
            return false;
        }

        if (!TryOpenClipboardWithRetry())
        {
            GlobalFreeManual(hDib);
            PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
            return false;
        }

        bool copiedDib = false;
        bool copiedBitmap = false;
        try
        {
            if (!PInvoke.EmptyClipboard())
            {
                return false;
            }

            copiedDib = !PInvoke.SetClipboardData(ClipboardDib, (HANDLE)hDib).IsNull;
            copiedBitmap = !PInvoke.SetClipboardData(ClipboardBitmap, (HANDLE)hBitmap).IsNull;

            return copiedDib || copiedBitmap;
        }
        finally
        {
            PInvoke.CloseClipboard();

            if (!copiedDib)
            {
                GlobalFreeManual(hDib);
            }

            if (!copiedBitmap)
            {
                PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
            }
        }
    }

    public static unsafe bool TryLoadFromClipboard(out ImageBuffer? buffer)
    {
        buffer = null;
        if (!TryOpenClipboardWithRetry())
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
                var imageBuffer = new ImageBuffer(width, height);
                byte[] bytes = new byte[stride * height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

                for (int y = 0; y < height; y++)
                {
                    int srcOffset = y * stride;
                    int destOffset = y * width * 4;
                    Buffer.BlockCopy(bytes, srcOffset, imageBuffer.Pixels, destOffset, width * 4);
                }

                return imageBuffer;
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

    private static Bitmap ToBitmap(ImageBuffer buffer)
    {
        var bitmap = new Bitmap(buffer.Width, buffer.Height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride;
            for (int y = 0; y < buffer.Height; y++)
            {
                int srcOffset = y * buffer.Width;
                nint rowDest = data.Scan0 + (y * stride);
                Marshal.Copy(buffer.Pixels, srcOffset, rowDest, buffer.Width);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    private static unsafe nint CreateClipboardDib(ImageBuffer source)
    {
        int width = source.Width;
        int height = source.Height;
        int stride = ((width * 32 + 31) / 32) * 4;
        nuint imageSize = (nuint)(stride * height);
        nuint totalSize = (nuint)sizeof(BITMAPINFOHEADER) + imageSize;

        nint handleValue = GlobalAllocManual(GmemMoveable, totalSize);
        if (handleValue == nint.Zero)
        {
            return nint.Zero;
        }

        void* ptr = (void*)GlobalLockManual(handleValue);
        if (ptr == null)
        {
            GlobalFreeManual(handleValue);
            return nint.Zero;
        }

        try
        {
            var header = (BITMAPINFOHEADER*)ptr;
            *header = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = (uint)BI_COMPRESSION.BI_RGB,
                biSizeImage = (uint)imageSize,
            };

            byte* pixelBase = (byte*)ptr + sizeof(BITMAPINFOHEADER);
            for (int y = 0; y < height; y++)
            {
                int srcY = height - 1 - y;
                int srcOffset = srcY * width;
                int* destRow = (int*)(pixelBase + (y * stride));

                for (int x = 0; x < width; x++)
                {
                    destRow[x] = source.Pixels[srcOffset + x];
                }

                for (int b = width * 4; b < stride; b++)
                {
                    *((byte*)destRow + b) = 0;
                }
            }
        }
        finally
        {
            GlobalUnlockManual(handleValue);
        }

        return handleValue;
    }

    private static bool TryOpenClipboardWithRetry()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (PInvoke.OpenClipboard(HWND.Null))
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
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
