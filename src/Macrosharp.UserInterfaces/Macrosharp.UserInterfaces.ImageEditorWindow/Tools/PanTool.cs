using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The pan/scroll tool allowing users to move the view around a zoomed image.
///
/// Features:
/// - Left mouse drag: Pan the view
/// - Plain wheel: Zoom at cursor position
/// - Ctrl+wheel: Zoom at viewport center
/// </summary>
public sealed class PanTool : IEditorTool
{
    private bool _isPanning;
    private IntPoint _lastPoint;

    /// <summary>
    /// Starts a pan operation at the current position.
    /// </summary>
    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        _isPanning = true;
        _lastPoint = input.ScreenPoint;
    }

    /// <summary>
    /// Continues panning by calculating the delta from the last position.
    /// </summary>
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

    public void OnCancel(ImageEditor editor)
    {
        _isPanning = false;
    }

    public void OnRender(ImageEditor editor, HDC hdc, int width, int height) { }
}
