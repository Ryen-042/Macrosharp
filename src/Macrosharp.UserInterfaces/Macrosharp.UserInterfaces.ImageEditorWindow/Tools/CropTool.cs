using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The crop tool for selecting and removing parts of the image.
///
/// Features:
/// - Left mouse drag: Define crop area
/// - Enter key: Apply crop to the selection
/// - Escape key: Cancel selection
/// - Plain wheel: Zoom at cursor position
/// - Ctrl+wheel: Zoom at viewport center
/// </summary>
public sealed class CropTool : IEditorTool
{
    private bool _isDragging;
    private IntPoint _start;
    private IntPoint _end;
    private bool _hasSelection;

    /// <summary>
    /// Starts defining a crop selection area.
    /// </summary>
    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        _isDragging = true;
        _start = input.ImagePoint;
        _end = input.ImagePoint;
        _hasSelection = false;
    }

    /// <summary>
    /// Updates the crop selection area as the user drags.
    /// </summary>
    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isDragging)
        {
            return;
        }

        _end = input.ImagePoint;
    }

    /// <summary>
    /// Finalizes the crop selection (but doesn't apply it yet).
    /// </summary>
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

    /// <summary>
    /// Handles keyboard input:
    /// - Escape: Cancel selection
    /// - Enter: Apply crop to the selected area
    /// </summary>
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

    /// <summary>
    /// Renders the crop selection rectangle overlay.
    /// </summary>
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

        // Convert from image coordinates to screen coordinates
        var topLeft = transform.ImageToScreen(new IntPoint(rect.left, rect.top));
        var bottomRight = transform.ImageToScreen(new IntPoint(rect.right, rect.bottom));
        var screenRect = new RECT
        {
            left = topLeft.X,
            top = topLeft.Y,
            right = bottomRight.X,
            bottom = bottomRight.Y,
        };

        // Draw selection frame
        HBRUSH brush = PInvoke.CreateSolidBrush(new COLORREF(0x00FFFFFF));
        using var safeBrush = new SafeBrushHandle(brush);
        PInvoke.FrameRect(hdc, screenRect, safeBrush);
    }
}
