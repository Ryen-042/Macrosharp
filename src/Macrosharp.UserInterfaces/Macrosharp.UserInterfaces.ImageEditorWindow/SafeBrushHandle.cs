using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// A safe wrapper around GDI brush handles that automatically deletes the handle when disposed.
/// Implements IDisposable pattern for safe resource cleanup.
/// </summary>
internal sealed class SafeBrushHandle : SafeHandle
{
    /// <summary>
    /// Creates a safe wrapper for the specified GDI brush handle.
    /// The handle will be automatically deleted when this wrapper is disposed.
    /// </summary>
    public unsafe SafeBrushHandle(HBRUSH handle)
        : base(IntPtr.Zero, true)
    {
        SetHandle((nint)handle.Value);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Deletes the GDI brush handle when this wrapper is disposed.
    /// </summary>
    protected override bool ReleaseHandle()
    {
        return PInvoke.DeleteObject(new HGDIOBJ(handle));
    }
}
