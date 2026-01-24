using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class CropTool : IEditorTool
{
    private bool _isDragging;
    private IntPoint _start;
    private IntPoint _end;
    private bool _hasSelection;

    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        _isDragging = true;
        _start = input.ImagePoint;
        _end = input.ImagePoint;
        _hasSelection = false;
    }

    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isDragging)
        {
            return;
        }

        _end = input.ImagePoint;
    }

    public void OnMouseUp(ImageEditor editor, EditorInput input)
    {
        if (!_isDragging)
        {
            return;
        }

        _end = input.ImagePoint;
        _isDragging = false;
        _hasSelection = true;
    }

    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        double factor = input.WheelDelta > 0 ? 1.1 : 0.9;
        editor.ZoomAt(input.ScreenPoint, factor);
    }

    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers)
    {
        if (key == VIRTUAL_KEY.VK_ESCAPE)
        {
            _isDragging = false;
            _hasSelection = false;
        }

        if (key == VIRTUAL_KEY.VK_RETURN && _hasSelection)
        {
            var rect = new IntRect(_start.X, _start.Y, _end.X, _end.Y);
            editor.ApplyCrop(rect);
            _hasSelection = false;
        }
    }

    public void OnCancel(ImageEditor editor)
    {
        _isDragging = false;
        _hasSelection = false;
    }

    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        if (!_isDragging && !_hasSelection)
        {
            return;
        }

        var transform = editor.Transform;
        RECT rect = new()
        {
            left = Math.Min(_start.X, _end.X),
            top = Math.Min(_start.Y, _end.Y),
            right = Math.Max(_start.X, _end.X),
            bottom = Math.Max(_start.Y, _end.Y),
        };

        var screenRect = new RECT
        {
            left = transform.PanX + (int)Math.Round(rect.left * transform.Zoom),
            top = transform.PanY + (int)Math.Round(rect.top * transform.Zoom),
            right = transform.PanX + (int)Math.Round(rect.right * transform.Zoom),
            bottom = transform.PanY + (int)Math.Round(rect.bottom * transform.Zoom),
        };

        HBRUSH brush = PInvoke.CreateSolidBrush(new COLORREF(0x00FFFFFF));
        using var safeBrush = new SafeBrushHandle(brush);
        PInvoke.FrameRect(hdc, screenRect, safeBrush);
    }
}
