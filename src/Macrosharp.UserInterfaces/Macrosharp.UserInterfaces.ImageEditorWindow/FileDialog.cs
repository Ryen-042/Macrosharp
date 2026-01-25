using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// Wrapper around the Windows GetOpenFileName dialog for selecting image files.
/// Uses P/Invoke to call the Windows file dialog native functions.
/// </summary>
internal static class FileDialog
{
    // File dialog flags
    private const int OFN_FILEMUSTEXIST = 0x00001000; // Selected file must exist
    private const int OFN_PATHMUSTEXIST = 0x00000800; // Selected path must exist
    private const int OFN_EXPLORER = 0x00080000; // Use new explorer style
    private const int OFN_NOCHANGEDIR = 0x00000008; // Don't change working directory

    /// <summary>
    /// Shows the file open dialog filtered to image formats.
    /// </summary>
    /// <param name="owner">Parent window handle</param>
    /// <param name="path">Output path if user selects a file</param>
    /// <returns>True if user selected a file, false if cancelled</returns>
    public static unsafe bool TryOpenImageFile(HWND owner, out string? path)
    {
        path = null;
        Span<char> buffer = stackalloc char[1024];
        buffer.Clear();

        fixed (char* filePtr = buffer)
        fixed (char* filterPtr = "Image Files\0*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff\0All Files\0*.*\0\0")
        {
            OPENFILENAME ofn = new()
            {
                lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>(),
                hwndOwner = (nint)owner.Value,
                lpstrFilter = filterPtr,
                lpstrFile = filePtr,
                nMaxFile = buffer.Length,
                Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_EXPLORER | OFN_NOCHANGEDIR,
            };

            if (GetOpenFileName(ref ofn))
            {
                path = new string(filePtr);
                int nullIndex = path.IndexOf('\0');
                if (nullIndex >= 0)
                {
                    path = path[..nullIndex];
                }
                return !string.IsNullOrWhiteSpace(path);
            }
        }

        return false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct OPENFILENAME
    {
        public uint lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public char* lpstrFilter;
        public char* lpstrCustomFilter;
        public uint nMaxCustFilter;
        public uint nFilterIndex;
        public char* lpstrFile;
        public int nMaxFile;
        public char* lpstrFileTitle;
        public uint nMaxFileTitle;
        public char* lpstrInitialDir;
        public char* lpstrTitle;
        public int Flags;
        public ushort nFileOffset;
        public ushort nFileExtension;
        public char* lpstrDefExt;
        public nint lCustData;
        public nint lpfnHook;
        public char* lpTemplateName;
        public void* pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern unsafe bool GetOpenFileName([In, Out] ref OPENFILENAME ofn);
}
