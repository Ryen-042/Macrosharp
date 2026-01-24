using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

internal sealed class SafeBrushHandle : SafeHandle
{
    public unsafe SafeBrushHandle(HBRUSH handle)
        : base(IntPtr.Zero, true)
    {
        SetHandle((nint)handle.Value);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return PInvoke.DeleteObject(new HGDIOBJ(handle));
    }
}
