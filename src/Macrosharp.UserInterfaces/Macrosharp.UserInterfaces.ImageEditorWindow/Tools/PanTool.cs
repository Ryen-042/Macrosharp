using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class PanTool : IEditorTool
{
    private bool _isPanning;
    private IntPoint _lastPoint;

    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        _isPanning = true;
        _lastPoint = input.ScreenPoint;
    }

    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isPanning)
        {
            return;
        }

        int dx = input.ScreenPoint.X - _lastPoint.X;
        int dy = input.ScreenPoint.Y - _lastPoint.Y;
        editor.PanBy(dx, dy);
        _lastPoint = input.ScreenPoint;
    }

    public void OnMouseUp(ImageEditor editor, EditorInput input)
    {
        _isPanning = false;
    }

    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        double factor = input.WheelDelta > 0 ? 1.1 : 0.9;
        editor.ZoomAt(input.ScreenPoint, factor);
    }

    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers) { }

    public void OnCancel(ImageEditor editor)
    {
        _isPanning = false;
    }

    public void OnRender(ImageEditor editor, HDC hdc, int width, int height) { }
}
