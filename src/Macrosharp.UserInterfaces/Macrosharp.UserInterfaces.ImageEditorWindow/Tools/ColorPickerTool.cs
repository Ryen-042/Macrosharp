using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The color picker tool for sampling colors from the image.
///
/// Features:
/// - Left mouse click: Sample color at cursor position and set as brush color
/// - Display: Shows the sampled color in hex format
/// - Plain wheel: Zoom at cursor position
/// - Ctrl+wheel: Zoom at viewport center
/// </summary>
public sealed class ColorPickerTool : IEditorTool
{
    private int _pickedColor = unchecked((int)0xFFFFFFFF); // Default to white

    /// <summary>
    /// Samples the color at the clicked position and sets it as the brush color.
    /// </summary>
    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        var buffer = editor.State.GetMatrix();
        _pickedColor = buffer.GetPixel(input.ImagePoint.X, input.ImagePoint.Y);
        // Set the picked color as the brush color for the draw tool
        editor.SetBrushColor(_pickedColor);
    }

    public void OnMouseMove(ImageEditor editor, EditorInput input) { }

    public void OnMouseUp(ImageEditor editor, EditorInput input) { }

    /// <summary>
    /// Handles mouse wheel zoom with modifier support:
    /// - Ctrl+wheel: Zoom at viewport center
    /// - Plain wheel: Zoom at cursor position
    /// </summary>
    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        if (input.Modifiers.HasFlag(ModifierState.Control))
        {
            editor.ZoomAtViewportCenter(input.WheelDelta);
        }
        else
        {
            editor.ZoomAtWheel(input.ScreenPoint, input.WheelDelta);
        }
    }

    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers) { }

    public void OnCancel(ImageEditor editor) { }

    /// <summary>
    /// Renders the picked color display showing the hex color code.
    /// </summary>
    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        string info = $"Picked: #{_pickedColor & 0xFFFFFF:X6}";
        var rect = new RECT
        {
            left = 8,
            top = 54,
            right = width - 8,
            bottom = 74,
        };
        PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        PInvoke.SetTextColor(hdc, new COLORREF(0x00FFFFFF));
        GdiText.DrawText(hdc, info, ref rect, DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER);
    }
}
