using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The drawing tool that allows free-form painting on the image.
///
/// Features:
/// - Left mouse button: Draw with the current color
/// - Right mouse button: Erase with transparency
/// - Shift+drag: Draw straight lines from the anchor point
/// - Shift+wheel or +/- keys: Adjust brush size
/// - Ctrl+wheel: Zoom at viewport center
/// - Plain wheel: Zoom at cursor position
/// </summary>
public sealed class DrawTool : IEditorTool
{
    private bool _isDrawing;
    private IntPoint _lastPoint; // Last point for continuous line drawing
    private IntPoint _anchorPoint; // Start point for shift+drag straight lines
    private int _brushRadius = 3; // Brush size in pixels
    private int _color = unchecked((int)0xFFFFCC00); // Default brush color (amber)
    private bool _eraseMode; // True when right button is held

    // Preset colors for number key shortcuts (1-9)
    private static readonly int[] PresetColors = new int[]
    {
        unchecked((int)0xFF000000), // 0: Black
        unchecked((int)0xFFFF0000), // 1: Red
        unchecked((int)0xFFFF8000), // 2: Orange
        unchecked((int)0xFFFFFF00), // 3: Yellow
        unchecked((int)0xFF00FF00), // 4: Green
        unchecked((int)0xFF00FFFF), // 5: Cyan
        unchecked((int)0xFF0080FF), // 6: Light Blue
        unchecked((int)0xFF0000FF), // 7: Blue
        unchecked((int)0xFFFF00FF), // 8: Magenta
        unchecked((int)0xFFFFFFFF), // 9: White
    };

    /// <summary>
    /// Gets the current brush color.
    /// </summary>
    public int BrushColor => _color;

    /// <summary>
    /// Sets the brush color.
    /// </summary>
    public void SetBrushColor(int argb)
    {
        _color = argb;
    }

    /// <summary>
    /// Starts a drawing operation or erases (if right button).
    /// Sets anchor point for potential straight line drawing.
    /// Pushes undo snapshot before starting to allow undo of brush strokes.
    /// </summary>
    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        if (input.Button == MouseButton.Right)
        {
            _eraseMode = true;
        }

        // Push undo snapshot before drawing starts (makes brush strokes undoable)
        editor.State.PushUndoSnapshot();
        editor.State.MarkUserHasImage();
        _isDrawing = true;
        _lastPoint = input.ImagePoint;
        _anchorPoint = input.ImagePoint;
        DrawDot(editor, input.ImagePoint);
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Continues drawing with the mouse movement.
    /// If Shift is held, draws a straight line from anchor; otherwise continuous free-form.
    /// </summary>
    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing)
        {
            return;
        }

        if (input.Modifiers.HasFlag(ModifierState.Shift))
        {
            // Straight line from anchor (updated each frame for preview)
            DrawLine(editor, _anchorPoint, input.ImagePoint);
        }
        else
        {
            // Continuous free-form line
            DrawLine(editor, _lastPoint, input.ImagePoint);
            _lastPoint = input.ImagePoint;
        }
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Ends the drawing operation.
    /// </summary>
    public void OnMouseUp(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing)
        {
            return;
        }

        DrawLine(editor, _lastPoint, input.ImagePoint);
        _isDrawing = false;
        _eraseMode = false;
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Handles mouse wheel input with modifier support:
    /// - Shift+wheel: Adjust brush size
    /// - Ctrl+wheel: Zoom at viewport center
    /// - Plain wheel: Zoom at cursor position
    /// </summary>
    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        if (input.Modifiers.HasFlag(ModifierState.Shift))
        {
            _brushRadius = Math.Clamp(_brushRadius + Math.Sign(input.WheelDelta), 1, 50);
        }
        else if (input.Modifiers.HasFlag(ModifierState.Control))
        {
            editor.ZoomAtViewportCenter(input.WheelDelta);
        }
        else
        {
            editor.ZoomAtWheel(input.ScreenPoint, input.WheelDelta);
        }
    }

    /// <summary>
    /// Handles keyboard input for brush size adjustment and color preset selection.
    /// - +/-: Adjust brush size
    /// - 0: Select black color
    /// - 1-9: Select preset color
    /// </summary>
    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers)
    {
        if (key == VIRTUAL_KEY.VK_OEM_PLUS)
        {
            _brushRadius = Math.Clamp(_brushRadius + 1, 1, 50);
        }
        else if (key == VIRTUAL_KEY.VK_OEM_MINUS)
        {
            _brushRadius = Math.Clamp(_brushRadius - 1, 1, 50);
        }
        else if (key >= VIRTUAL_KEY.VK_0 && key <= VIRTUAL_KEY.VK_9)
        {
            int index = (int)key - (int)VIRTUAL_KEY.VK_0;
            _color = PresetColors[index];
        }
    }

    /// <summary>
    /// Cancels the current drawing operation.
    /// </summary>
    public void OnCancel(ImageEditor editor)
    {
        _isDrawing = false;
        _eraseMode = false;
    }

    /// <summary>
    /// Renders the tool UI showing current brush size and color.
    /// </summary>
    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        // Display brush info text
        string info = $"Brush: {_brushRadius}px | Color: #{_color & 0xFFFFFF:X6} | Keys: 0-9 for presets";
        var rect = new RECT
        {
            left = 8,
            top = 32,
            right = width - 8,
            bottom = 52,
        };
        PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        PInvoke.SetTextColor(hdc, new COLORREF(0x00FFFFFF));
        GdiText.DrawText(hdc, info, ref rect, DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER);

        // Draw a color swatch preview
        int swatchSize = 16;
        int swatchX = 8;
        int swatchY = 54;
        var colorRect = new RECT
        {
            left = swatchX,
            top = swatchY,
            right = swatchX + swatchSize,
            bottom = swatchY + swatchSize,
        };
        // Convert ARGB to BGR for GDI (swap R and B)
        int r = (_color >> 16) & 0xFF;
        int g = (_color >> 8) & 0xFF;
        int b = _color & 0xFF;
        HBRUSH colorBrush = PInvoke.CreateSolidBrush(new COLORREF((uint)(b << 16 | g << 8 | r)));
        using var safeBrush = new SafeBrushHandle(colorBrush);
        PInvoke.FillRect(hdc, colorRect, safeBrush);

        // Draw border around swatch
        HBRUSH borderBrush = PInvoke.CreateSolidBrush(new COLORREF(0x00FFFFFF));
        using var safeBorderBrush = new SafeBrushHandle(borderBrush);
        PInvoke.FrameRect(hdc, colorRect, safeBorderBrush);
    }

    /// <summary>
    /// Draws a filled circle (dot) using the brush radius.
    /// Uses anti-aliasing by checking distance from center.
    /// </summary>
    private void DrawDot(ImageEditor editor, IntPoint imagePoint)
    {
        var buffer = editor.State.GetMatrix();
        int argb = _eraseMode ? unchecked((int)0x00000000) : _color;
        for (int y = -_brushRadius; y <= _brushRadius; y++)
        {
            for (int x = -_brushRadius; x <= _brushRadius; x++)
            {
                if (x * x + y * y <= _brushRadius * _brushRadius)
                {
                    buffer.SetPixel(imagePoint.X + x, imagePoint.Y + y, argb);
                }
            }
        }
    }

    private void DrawLine(ImageEditor editor, IntPoint start, IntPoint end)
    {
        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            DrawDot(editor, new IntPoint(x0, y0));
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
