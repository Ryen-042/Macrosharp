using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class DrawTool : IEditorTool
{
    private bool _isDrawing;
    private IntPoint _lastPoint;
    private IntPoint _anchorPoint;
    private int _brushRadius = 3;
    private int _color = unchecked((int)0xFFFFCC00);
    private bool _eraseMode;

    public void OnMouseDown(ImageEditor editor, EditorInput input)
    {
        if (input.Button == MouseButton.Right)
        {
            _eraseMode = true;
        }

        _isDrawing = true;
        _lastPoint = input.ImagePoint;
        _anchorPoint = input.ImagePoint;
        DrawDot(editor, input.ImagePoint);
        editor.State.MarkMatrixDirty();
    }

    public void OnMouseMove(ImageEditor editor, EditorInput input)
    {
        if (!_isDrawing)
        {
            return;
        }

        if (input.Modifiers.HasFlag(ModifierState.Shift))
        {
            DrawLine(editor, _anchorPoint, input.ImagePoint);
        }
        else
        {
            DrawLine(editor, _lastPoint, input.ImagePoint);
            _lastPoint = input.ImagePoint;
        }
        editor.State.MarkMatrixDirty();
    }

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

    public void OnMouseWheel(ImageEditor editor, EditorInput input)
    {
        if (input.Modifiers.HasFlag(ModifierState.Control))
        {
            _brushRadius = Math.Clamp(_brushRadius + Math.Sign(input.WheelDelta), 1, 50);
        }
        else
        {
            double factor = input.WheelDelta > 0 ? 1.1 : 0.9;
            editor.ZoomAt(input.ScreenPoint, factor);
        }
    }

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
    }

    public void OnCancel(ImageEditor editor)
    {
        _isDrawing = false;
        _eraseMode = false;
    }

    public void OnRender(ImageEditor editor, HDC hdc, int width, int height)
    {
        string info = $"Brush: {_brushRadius}px";
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
    }

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
