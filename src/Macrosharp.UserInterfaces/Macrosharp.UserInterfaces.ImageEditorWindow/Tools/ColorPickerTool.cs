using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class ColorPickerTool : IEditorTool
{
    private int _pickedColor = unchecked((int)0xFFFFFFFF);

    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        var buffer = editor.State.GetMatrix();
        _pickedColor = buffer.GetPixel(input.ImagePoint.X, input.ImagePoint.Y);
    }

    public void OnMouseMove(ImageEditor editor, EditorInput input) { }

    public void OnMouseUp(ImageEditor editor, EditorInput input) { }

    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        double factor = input.WheelDelta > 0 ? 1.1 : 0.9;
        editor.ZoomAt(input.ScreenPoint, factor);
    }

    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers) { }

    public void OnCancel(ImageEditor editor) { }

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
