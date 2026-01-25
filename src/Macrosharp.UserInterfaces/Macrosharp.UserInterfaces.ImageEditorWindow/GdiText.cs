using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// Helper class for drawing text using GDI with proper Unicode string marshaling.
/// Reduces P/Invoke boilerplate by handling the fixed pinning internally.
/// </summary>
internal static class GdiText
{
    /// <summary>
    /// Draws a Unicode string in the specified rectangle with GDI formatting.
    /// </summary>
    /// <param name="hdc">Device context to draw to</param>
    /// <param name="text">The text string to draw</param>
    /// <param name="rect">Rectangle defining the bounds</param>
    /// <param name="format">GDI drawing format flags (DT_LEFT, DT_CENTER, etc.)</param>
    public static unsafe void DrawText(HDC hdc, string text, ref RECT rect, DRAW_TEXT_FORMAT format)
    {
        fixed (char* pText = text)
        {
            PInvoke.DrawText(hdc, new PCWSTR(pText), text.Length, ref rect, format);
        }
    }
}
