using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The arrow drawing tool that allows drawing arrows on the image.
///
/// Features:
/// - Left mouse drag: Draw arrow from start to end point
/// - Shift+wheel or +/- keys: Adjust line thickness
/// - Ctrl+wheel: Zoom at viewport center
/// - Plain wheel: Zoom at cursor position
/// - 0-9 keys: Select preset colors (same as draw tool)
/// </summary>
public sealed class ArrowTool : IEditorTool
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
    /// Starts drawing an arrow from the clicked point.
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
    /// Updates the arrow preview as the mouse moves.
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
        
        // Draw preview arrow
        DrawArrowPreview(editor, _startPoint, _currentEndPoint);
        
        editor.State.MarkMatrixDirty();
    }

    /// <summary>
    /// Finalizes the arrow drawing.
    /// </summary>
    public void OnMouseUp(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing)
        {
            return;
        }

        _currentEndPoint = input.ImagePoint;
        DrawArrow(editor, _startPoint, _currentEndPoint);
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
    /// Cancels the current arrow drawing operation.
    /// </summary>
    public void OnCancel(ImageEditor editor)
    {
        _isDrawing = false;
    }

    /// <summary>
    /// Renders the tool UI showing current line thickness and color, and a preview of the arrow being drawn.
    /// </summary>
    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        if (!editor.IsOverlayVisible)
        {
            return;
        }

        // Display arrow info text
        string info = $"Arrow: {_lineThickness}px | Color: #{_color & 0xFFFFFF:X6} | Keys: 0-9 for presets";
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
    /// Draws a line by densely sampling along the segment and stamping a round brush.
    /// This avoids the staircase look from sparse pixel stepping.
    /// </summary>
    private void DrawLine(ImageEditor editor, IntPoint start, IntPoint end)
    {
        DrawLine(editor.State.GetMatrix(), start, end, _lineThickness, _color);
    }

    /// <summary>
    /// Draws a smooth sampled line into the buffer.
    /// </summary>
    private void DrawLine(ImageBuffer buffer, IntPoint start, IntPoint end, int thickness, int color)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.5)
        {
            DrawThickPoint(buffer, start, thickness, color);
            return;
        }

        int steps = Math.Max(2, (int)Math.Ceiling(distance * 6.0));
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;
            int x = (int)Math.Round(start.X + dx * t);
            int y = (int)Math.Round(start.Y + dy * t);
            DrawThickPoint(buffer, new IntPoint(x, y), thickness, color);
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
    /// Draws an arrowhead whose size is proportional to the shaft thickness and arrow length.
    /// </summary>
    private void DrawArrowHead(ImageBuffer buffer, IntPoint basePoint, IntPoint tipPoint)
    {
        double dx = tipPoint.X - basePoint.X;
        double dy = tipPoint.Y - basePoint.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1)
        {
            return;
        }

        dx /= length;
        dy /= length;

        double headLength = Math.Min(length * 0.24, Math.Max(_lineThickness * 2.5, _lineThickness * 4.0));
        headLength = Math.Min(headLength, length * 0.5);
        double headWidth = Math.Max(_lineThickness * 1.5, headLength * 0.5);

        double px = -dy;
        double py = dx;

        var tip = new IntPoint(tipPoint.X, tipPoint.Y);
        var back = new IntPoint(
            (int)Math.Round(tipPoint.X - dx * headLength),
            (int)Math.Round(tipPoint.Y - dy * headLength));
        var left = new IntPoint(
            (int)Math.Round(back.X + px * headWidth),
            (int)Math.Round(back.Y + py * headWidth));
        var right = new IntPoint(
            (int)Math.Round(back.X - px * headWidth),
            (int)Math.Round(back.Y - py * headWidth));

        DrawFilledTriangle(buffer, tip, left, right, _color);
    }

    /// <summary>
    /// Draws a filled triangle on the buffer.
    /// </summary>
    private void DrawFilledTriangle(ImageBuffer buffer, IntPoint p1, IntPoint p2, IntPoint p3, int color)
    {
        // Find bounding box
        int minX = Math.Min(Math.Min(p1.X, p2.X), p3.X);
        int maxX = Math.Max(Math.Max(p1.X, p2.X), p3.X);
        int minY = Math.Min(Math.Min(p1.Y, p2.Y), p3.Y);
        int maxY = Math.Max(Math.Max(p1.Y, p2.Y), p3.Y);

        // Iterate over bounding box and fill points inside the triangle
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsPointInTriangle(x, y, p1, p2, p3))
                {
                    buffer.SetPixel(x, y, color);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a point is inside or on the edge of a triangle using barycentric coordinates.
    /// </summary>
    private bool IsPointInTriangle(int px, int py, IntPoint p1, IntPoint p2, IntPoint p3)
    {
        // Vector from p1 to p2
        double v0x = p3.X - p1.X;
        double v0y = p3.Y - p1.Y;

        // Vector from p1 to p2
        double v1x = p2.X - p1.X;
        double v1y = p2.Y - p1.Y;

        // Vector from p1 to the point
        double v2x = px - p1.X;
        double v2y = py - p1.Y;

        // Compute dot products
        double dot00 = v0x * v0x + v0y * v0y;
        double dot01 = v0x * v1x + v0y * v1y;
        double dot02 = v0x * v2x + v0y * v2y;
        double dot11 = v1x * v1x + v1y * v1y;
        double dot12 = v1x * v2x + v1y * v2y;

        // Compute barycentric coordinates
        double denom = dot00 * dot11 - dot01 * dot01;
        if (Math.Abs(denom) < 0.0001)
        {
            return false;
        }

        double invDenom = 1.0 / denom;
        double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v <= 1;
    }

    /// <summary>
    /// Draws a complete arrow from start to end point (for preview).
    /// </summary>
    private void DrawArrowPreview(ImageEditor editor, IntPoint start, IntPoint end)
    {
        DrawArrowGeometry(editor.State.GetMatrix(), start, end);
    }

    /// <summary>
    /// Draws a complete arrow from start to end point (final).
    /// </summary>
    private void DrawArrow(ImageEditor editor, IntPoint start, IntPoint end)
    {
        DrawArrowGeometry(editor.State.GetMatrix(), start, end);
    }

    /// <summary>
    /// Draws the arrow shaft and proportional head.
    /// </summary>
    private void DrawArrowGeometry(ImageBuffer buffer, IntPoint start, IntPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 0.5)
        {
            DrawThickPoint(buffer, start, _lineThickness, _color);
            return;
        }

        dx /= length;
        dy /= length;
        double px = -dy;
        double py = dx;

        double shaftHalfWidth = Math.Max(4.0, _lineThickness * 2.25);
        double headLength = Math.Min(length * 0.30, Math.Max(shaftHalfWidth * 4.0, _lineThickness * 7.0));
        headLength = Math.Min(headLength, length * 0.5);
        double headHalfWidth = Math.Max(shaftHalfWidth * 2.15, headLength * 0.62);

        var headBase = new IntPoint(
            (int)Math.Round(end.X - dx * headLength),
            (int)Math.Round(end.Y - dy * headLength));
        var shaftEnd = new IntPoint(headBase.X, headBase.Y);

        var shaftStartLeft = new IntPoint(
            (int)Math.Round(start.X + px * shaftHalfWidth),
            (int)Math.Round(start.Y + py * shaftHalfWidth));
        var shaftStartRight = new IntPoint(
            (int)Math.Round(start.X - px * shaftHalfWidth),
            (int)Math.Round(start.Y - py * shaftHalfWidth));
        var shaftEndLeft = new IntPoint(
            (int)Math.Round(shaftEnd.X + px * shaftHalfWidth),
            (int)Math.Round(shaftEnd.Y + py * shaftHalfWidth));
        var shaftEndRight = new IntPoint(
            (int)Math.Round(shaftEnd.X - px * shaftHalfWidth),
            (int)Math.Round(shaftEnd.Y - py * shaftHalfWidth));

        DrawFilledTriangle(buffer, shaftStartLeft, shaftEndLeft, shaftEndRight, _color);
        DrawFilledTriangle(buffer, shaftStartLeft, shaftEndRight, shaftStartRight, _color);

        var headLeft = new IntPoint(
            (int)Math.Round(headBase.X + px * headHalfWidth),
            (int)Math.Round(headBase.Y + py * headHalfWidth));
        var headRight = new IntPoint(
            (int)Math.Round(headBase.X - px * headHalfWidth),
            (int)Math.Round(headBase.Y - py * headHalfWidth));

        DrawFilledTriangle(buffer, end, headLeft, headRight, _color);
    }
}
