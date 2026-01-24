using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

internal static class GdiText
{
    public static unsafe void DrawText(HDC hdc, string text, ref RECT rect, DRAW_TEXT_FORMAT format)
    {
        fixed (char* pText = text)
        {
            PInvoke.DrawText(hdc, new PCWSTR(pText), text.Length, ref rect, format);
        }
    }
}
