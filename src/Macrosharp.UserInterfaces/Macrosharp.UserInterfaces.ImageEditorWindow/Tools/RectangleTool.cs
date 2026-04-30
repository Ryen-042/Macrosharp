using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The rectangle drawing tool that allows drawing rectangles on the image.
///
/// Features:
/// - Left mouse drag: Draw rectangle from start to end point
/// - Shift+wheel or +/- keys: Adjust line thickness
/// - Ctrl+wheel: Zoom at viewport center
/// - Plain wheel: Zoom at cursor position
/// - 0-9 keys: Select preset colors (same as draw tool)
/// </summary>
public sealed class RectangleTool : IEditorTool
{
    private bool _isDrawing;
    private IntPoint _startPoint;
    private IntPoint _currentEndPoint;
    private ImageBuffer? _bufferBackup; // Backup of original buffer for preview rendering
    private int _lineThickness = 2; // Line width in pixels
    private int _color = unchecked((int)0xFFFFCC00); // Default color (amber)

    // Preset colors matching the draw tool
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
    /// Starts drawing a rectangle from the clicked point.
    /// </summary>
    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        editor.State.PushUndoSnapshot();
        editor.State.MarkUserHasImage();
        _isDrawing = true;
        _startPoint = input.ImagePoint;
        _currentEndPoint = input.ImagePoint;
        
        // Backup the current buffer state for preview rendering
        var buffer = editor.State.GetMatrix();
        _bufferBackup = buffer.Clone();
        
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Updates the rectangle preview as the mouse moves.
    /// </summary>
    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing || _bufferBackup == null)
        {
            return;
        }

        _currentEndPoint = input.ImagePoint;
        
        // Restore the backup buffer and draw the preview
        var buffer = editor.State.GetMatrix();
        buffer.CopyFrom(_bufferBackup);
        
        // Draw preview rectangle
        DrawRectanglePreview(editor, _startPoint, _currentEndPoint);
        
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Finalizes the rectangle drawing.
    /// </summary>
    public void OnMouseUp(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing)
        {
            return;
        }

        _currentEndPoint = input.ImagePoint;
        DrawRectangle(editor, _startPoint, _currentEndPoint);
        _isDrawing = false;
        _bufferBackup = null;
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Handles mouse wheel input with modifier support:
    /// - Shift+wheel: Adjust line thickness
    /// - Ctrl+wheel: Zoom at viewport center
    /// - Plain wheel: Zoom at cursor position
    /// </summary>
    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        if (input.Modifiers.HasFlag(ModifierState.Shift))
        {
            _lineThickness = Math.Clamp(_lineThickness + Math.Sign(input.WheelDelta), 1, 20);
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
    /// Handles keyboard input for line thickness adjustment and color preset selection.
    /// - +/-: Adjust line thickness
    /// - 0-9: Select preset color
    /// </summary>
    public void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers)
    {
        if (key == VIRTUAL_KEY.VK_OEM_PLUS)
        {
            _lineThickness = Math.Clamp(_lineThickness + 1, 1, 20);
        }
        else if (key == VIRTUAL_KEY.VK_OEM_MINUS)
        {
            _lineThickness = Math.Clamp(_lineThickness - 1, 1, 20);
        }
        else if (key >= VIRTUAL_KEY.VK_0 && key <= VIRTUAL_KEY.VK_9)
        {
            int index = (int)key - (int)VIRTUAL_KEY.VK_0;
            _color = PresetColors[index];
        }
    }

    /// <summary>
    /// Cancels the current rectangle drawing operation.
    /// </summary>
    public void OnCancel(ImageEditor editor)
    {
        _isDrawing = false;
    }

    /// <summary>
    /// Renders the tool UI showing current line thickness and color, and a preview of the rectangle being drawn.
    /// </summary>
    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        if (!editor.IsOverlayVisible)
        {
            return;
        }

        // Display rectangle info text
        string info = $"Rectangle: {_lineThickness}px | Color: #{_color & 0xFFFFFF:X6} | Keys: 0-9 for presets";
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
        int r = (_color >> 16) & 0xFF;
        int g = (_color >> 8) & 0xFF;
        int b = _color & 0xFF;
        HBRUSH colorBrush = PInvoke.CreateSolidBrush(new COLORREF((uint)(b << 16 | g << 8 | r)));
        using var safeBrush = new SafeBrushHandle(colorBrush);
        PInvoke.FillRect(hdc, colorRect, safeBrush);

        HBRUSH borderBrush = PInvoke.CreateSolidBrush(new COLORREF(0x00FFFFFF));
        using var safeBorderBrush = new SafeBrushHandle(borderBrush);
        PInvoke.FrameRect(hdc, colorRect, safeBorderBrush);
    }

    /// <summary>
    /// Draws a thick line using Bresenham's algorithm.
    /// </summary>
    private void DrawLine(ImageEditor editor, IntPoint start, IntPoint end)
    {
        var buffer = editor.State.GetMatrix();
        
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
            DrawThickPoint(buffer, new IntPoint(x0, y0), _lineThickness, _color);
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
        
        // Add interpolated points between main points for smoother lines
        InterpolateAndDrawLine(buffer, start, end);
    }

    /// <summary>
    /// Interpolates additional points between start and end for smoother line rendering.
    /// </summary>
    private void InterpolateAndDrawLine(ImageBuffer buffer, IntPoint start, IntPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        
        if (distance < 1)
            return;

        int steps = (int)Math.Ceiling(distance / 2.0); // More steps = smoother line
        for (int i = 1; i < steps; i++)
        {
            double t = i / (double)steps;
            int x = (int)(start.X + dx * t);
            int y = (int)(start.Y + dy * t);
            DrawThickPoint(buffer, new IntPoint(x, y), _lineThickness, _color);
        }
    }

    /// <summary>
    /// Draws a thick point (circle) on the buffer.
    /// </summary>
    private void DrawThickPoint(ImageBuffer buffer, IntPoint point, int thickness, int color)
    {
        for (int y = -thickness; y <= thickness; y++)
        {
            for (int x = -thickness; x <= thickness; x++)
            {
                if (x * x + y * y <= thickness * thickness)
                {
                    buffer.SetPixel(point.X + x, point.Y + y, color);
                }
            }
        }
    }

    /// <summary>
    /// Draws a complete rectangle outline from start to end point (for preview).
    /// </summary>
    private void DrawRectanglePreview(ImageEditor editor, IntPoint start, IntPoint end)
    {
        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        // Draw four sides of the rectangle
        DrawLine(editor, new IntPoint(minX, minY), new IntPoint(maxX, minY)); // Top
        DrawLine(editor, new IntPoint(maxX, minY), new IntPoint(maxX, maxY)); // Right
        DrawLine(editor, new IntPoint(maxX, maxY), new IntPoint(minX, maxY)); // Bottom
        DrawLine(editor, new IntPoint(minX, maxY), new IntPoint(minX, minY)); // Left
    }

    /// <summary>
    /// Draws a complete rectangle outline from start to end point.
    /// </summary>
    private void DrawRectangle(ImageEditor editor, IntPoint start, IntPoint end)
    {
        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        // Draw four sides of the rectangle
        DrawLine(editor, new IntPoint(minX, minY), new IntPoint(maxX, minY)); // Top
        DrawLine(editor, new IntPoint(maxX, minY), new IntPoint(maxX, maxY)); // Right
        DrawLine(editor, new IntPoint(maxX, maxY), new IntPoint(minX, maxY)); // Bottom
        DrawLine(editor, new IntPoint(minX, maxY), new IntPoint(minX, minY)); // Left
    }
}
