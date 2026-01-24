using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class ImageEditor
{
    private readonly ImageEditorState _state;
    private readonly Dictionary<ToolKind, IEditorTool> _tools;
    private IEditorTool _activeTool;
    private ToolKind _activeToolKind;
    private int _viewportWidth;
    private int _viewportHeight;
    private double _zoom = 1.0;
    private int _panX;
    private int _panY;
    private bool _showStatusBar;
    private IntPoint _lastImagePoint;
    private IntPoint _lastScreenPoint;
    private bool _autoFit = true;
    private HWND _ownerHwnd = HWND.Null;

    public event Action<int, int>? WindowResizeRequested;

    public void SetOwner(HWND hwnd)
    {
        _ownerHwnd = hwnd;
    }

    public ImageEditor()
    {
        _state = new ImageEditorState(640, 480);
        _tools = new Dictionary<ToolKind, IEditorTool>
        {
            { ToolKind.Draw, new DrawTool() },
            { ToolKind.Crop, new CropTool() },
            { ToolKind.ColorPicker, new ColorPickerTool() },
            { ToolKind.Pan, new PanTool() },
        };
        _activeToolKind = ToolKind.Draw;
        _activeTool = _tools[_activeToolKind];
    }

    public void SetViewport(int width, int height)
    {
        _viewportWidth = Math.Max(1, width);
        _viewportHeight = Math.Max(1, height);
        if (_autoFit)
        {
            UpdateFitZoom();
        }
    }

    public void HandleMouseDown(IntPoint point, MouseButton button, ModifierState modifiers)
    {
        var input = BuildInput(point, modifiers, button);
        _activeTool.OnMouseDown(this, input);
    }

    public void HandleMouseMove(IntPoint point, ModifierState modifiers)
    {
        _lastScreenPoint = point;
        _lastImagePoint = Transform.ScreenToImage(point);
        var input = BuildInput(point, modifiers, MouseButton.None);
        _activeTool.OnMouseMove(this, input);
    }

    public void HandleMouseUp(IntPoint point, MouseButton button, ModifierState modifiers)
    {
        var input = BuildInput(point, modifiers, button);
        _activeTool.OnMouseUp(this, input);
        if (_activeTool is DrawTool)
        {
            _state.CommitMatrixToRaster();
        }
    }

    public void HandleMouseWheel(IntPoint point, int delta, ModifierState modifiers)
    {
        var input = BuildInput(point, modifiers, MouseButton.None, delta);
        _activeTool.OnMouseWheel(this, input);
    }

    public void HandleKeyDown(VIRTUAL_KEY key, ModifierState modifiers)
    {
        switch (key)
        {
            case VIRTUAL_KEY.VK_F1:
                _showStatusBar = !_showStatusBar;
                return;
            case VIRTUAL_KEY.VK_O:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    TryOpenFromFileDialog();
                }
                return;
            case VIRTUAL_KEY.VK_V:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    TryOpenFromClipboard();
                    return;
                }

                ApplyFlipVertical();
                return;
            case VIRTUAL_KEY.VK_W:
                SetTool(ToolKind.Draw);
                return;
            case VIRTUAL_KEY.VK_L:
                SetTool(ToolKind.Crop);
                return;
            case VIRTUAL_KEY.VK_C:
                SetTool(ToolKind.ColorPicker);
                return;
            case VIRTUAL_KEY.VK_SPACE:
                SetTool(ToolKind.Pan);
                return;
            case VIRTUAL_KEY.VK_ESCAPE:
                _activeTool.OnCancel(this);
                return;
            case VIRTUAL_KEY.VK_Z:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    _state.TryUndo();
                }
                return;
            case VIRTUAL_KEY.VK_Y:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    _state.TryRedo();
                }
                return;
            case VIRTUAL_KEY.VK_R:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    _state.ResetToOriginal();
                }
                else
                {
                    Rotate90Clockwise();
                }
                return;
            case VIRTUAL_KEY.VK_T:
                ApplyGrayscale();
                return;
            case VIRTUAL_KEY.VK_I:
                ApplyInvert();
                return;
            case VIRTUAL_KEY.VK_H:
                ApplyFlipHorizontal();
                return;
            case VIRTUAL_KEY.VK_0:
                if (modifiers.HasFlag(ModifierState.Control))
                {
                    ResetView();
                }
                return;
        }

        _activeTool.OnKeyDown(this, key, modifiers);
    }

    public void Render(HDC hdc, int width, int height)
    {
        SetViewport(width, height);

        DrawBackground(hdc, width, height);
        DrawImage(hdc);
        DrawOverlay(hdc);
        DrawStatusBar(hdc);
        _activeTool.OnRender(this, hdc, width, height);
    }

    public ImageEditorState State => _state;

    public ViewTransform Transform
    {
        get
        {
            var image = _state.GetMatrix();
            int destWidth = (int)Math.Round(image.Width * _zoom);
            int destHeight = (int)Math.Round(image.Height * _zoom);
            int originX = (_viewportWidth - destWidth) / 2;
            int originY = (_viewportHeight - destHeight) / 2;
            return new ViewTransform(_zoom, _panX, _panY, originX, originY, _viewportWidth, _viewportHeight);
        }
    }

    public void ZoomAt(IntPoint anchor, double factor)
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
        {
            return;
        }

        double newZoom = Math.Clamp(_zoom * factor, 0.1, 32.0);
        if (Math.Abs(newZoom - _zoom) < 0.001)
        {
            return;
        }

        _autoFit = false;

        double scale = newZoom / _zoom;
        _panX = (int)(anchor.X - scale * (anchor.X - _panX));
        _panY = (int)(anchor.Y - scale * (anchor.Y - _panY));
        _zoom = newZoom;

        var image = _state.GetMatrix();
        WindowResizeRequested?.Invoke((int)Math.Round(image.Width * _zoom), (int)Math.Round(image.Height * _zoom));
    }

    public void PanBy(int dx, int dy)
    {
        _panX += dx;
        _panY += dy;
    }

    public void ResetView()
    {
        _autoFit = true;
        _panX = 0;
        _panY = 0;
        UpdateFitZoom();
    }

    public void SetTool(ToolKind tool)
    {
        if (_activeToolKind == tool)
        {
            return;
        }

        _activeTool.OnCancel(this);
        _activeToolKind = tool;
        _activeTool = _tools[tool];
    }

    public void ApplyCrop(IntRect rect)
    {
        _state.ApplyCrop(rect);
        ResetView();
    }

    public bool TryOpenFromClipboard()
    {
        if (ImageEditorIO.TryLoadFromClipboard(out var buffer) && buffer != null)
        {
            ApplyLoadedImage(buffer);
            return true;
        }

        return false;
    }

    public bool TryOpenFromFileDialog()
    {
        if (_ownerHwnd == HWND.Null)
        {
            return false;
        }

        if (FileDialog.TryOpenImageFile(_ownerHwnd, out var path) && !string.IsNullOrWhiteSpace(path))
        {
            return TryOpenFromFile(path);
        }

        return false;
    }

    public bool TryOpenFromFile(string path)
    {
        if (ImageEditorIO.TryLoadFromFile(path, out var buffer) && buffer != null)
        {
            ApplyLoadedImage(buffer);
            return true;
        }

        return false;
    }

    private void ApplyLoadedImage(ImageBuffer buffer)
    {
        _state.ReplaceRaster(buffer);
        _autoFit = false;
        _zoom = 1.0;
        _panX = 0;
        _panY = 0;

        if (buffer.Width > _viewportWidth || buffer.Height > _viewportHeight)
        {
            WindowResizeRequested?.Invoke(buffer.Width, buffer.Height);
        }
    }

    private void ApplyGrayscale()
    {
        _state.ApplyRasterEdit(buffer =>
        {
            for (int i = 0; i < buffer.Pixels.Length; i++)
            {
                int argb = buffer.Pixels[i];
                int a = (argb >> 24) & 0xFF;
                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;
                int gray = (r * 77 + g * 150 + b * 29) >> 8;
                buffer.Pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray;
            }
        });
    }

    private void ApplyInvert()
    {
        _state.ApplyRasterEdit(buffer =>
        {
            for (int i = 0; i < buffer.Pixels.Length; i++)
            {
                int argb = buffer.Pixels[i];
                int a = (argb >> 24) & 0xFF;
                int r = 255 - ((argb >> 16) & 0xFF);
                int g = 255 - ((argb >> 8) & 0xFF);
                int b = 255 - (argb & 0xFF);
                buffer.Pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        });
    }

    private void ApplyFlipHorizontal()
    {
        _state.ApplyRasterEdit(buffer =>
        {
            int width = buffer.Width;
            int height = buffer.Height;
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * width;
                int left = 0;
                int right = width - 1;
                while (left < right)
                {
                    int li = rowStart + left;
                    int ri = rowStart + right;
                    (buffer.Pixels[li], buffer.Pixels[ri]) = (buffer.Pixels[ri], buffer.Pixels[li]);
                    left++;
                    right--;
                }
            }
        });
    }

    private void ApplyFlipVertical()
    {
        _state.ApplyRasterEdit(buffer =>
        {
            int width = buffer.Width;
            int height = buffer.Height;
            int half = height / 2;
            for (int y = 0; y < half; y++)
            {
                int topRow = y * width;
                int bottomRow = (height - 1 - y) * width;
                for (int x = 0; x < width; x++)
                {
                    int ti = topRow + x;
                    int bi = bottomRow + x;
                    (buffer.Pixels[ti], buffer.Pixels[bi]) = (buffer.Pixels[bi], buffer.Pixels[ti]);
                }
            }
        });
    }

    private void Rotate90Clockwise()
    {
        var source = _state.GetRasterCopy();
        int newWidth = source.Height;
        int newHeight = source.Width;
        var rotated = new ImageBuffer(newWidth, newHeight);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                int destX = newWidth - 1 - y;
                int destY = x;
                rotated.SetPixel(destX, destY, source.GetPixel(x, y));
            }
        }

        _state.ReplaceRaster(rotated);
        ResetView();
    }

    private EditorInput BuildInput(IntPoint point, ModifierState modifiers, MouseButton button, int wheelDelta = 0)
    {
        var transform = Transform;
        var imagePoint = transform.ScreenToImage(point);
        return new EditorInput(point, imagePoint, modifiers, button, wheelDelta);
    }

    private void DrawBackground(HDC hdc, int width, int height)
    {
        var rect = new RECT
        {
            left = 0,
            top = 0,
            right = width,
            bottom = height,
        };
        HBRUSH brush = PInvoke.CreateSolidBrush(new COLORREF(0x001E1E1E));
        using var safeBrush = new SafeBrushHandle(brush);
        PInvoke.FillRect(hdc, rect, safeBrush);
    }

    private unsafe void DrawImage(HDC hdc)
    {
        var image = _state.GetMatrix();
        if (image.Width <= 0 || image.Height <= 0)
        {
            return;
        }

        var transform = Transform;
        int destWidth = (int)Math.Round(image.Width * transform.Zoom);
        int destHeight = (int)Math.Round(image.Height * transform.Zoom);
        int destX = transform.OriginX + transform.PanX;
        int destY = transform.OriginY + transform.PanY;

        BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = image.Width;
        bmi.bmiHeader.biHeight = -image.Height;
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = (uint)BI_COMPRESSION.BI_RGB;

        fixed (int* pixels = image.Pixels)
        {
            PInvoke.StretchDIBits(hdc, destX, destY, destWidth, destHeight, 0, 0, image.Width, image.Height, pixels, &bmi, DIB_USAGE.DIB_RGB_COLORS, ROP_CODE.SRCCOPY);
        }
    }

    private void UpdateFitZoom()
    {
        var image = _state.GetMatrix();
        if (image.Width <= 0 || image.Height <= 0)
        {
            _zoom = 1.0;
            return;
        }

        double fitX = _viewportWidth / (double)image.Width;
        double fitY = _viewportHeight / (double)image.Height;
        _zoom = Math.Clamp(Math.Min(fitX, fitY), 0.1, 32.0);
    }

    private void DrawOverlay(HDC hdc)
    {
        string text = $"{_activeToolKind.ToString().ToUpperInvariant()}";
        var rect = new RECT
        {
            left = 8,
            top = 8,
            right = _viewportWidth - 8,
            bottom = 32,
        };
        PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        PInvoke.SetTextColor(hdc, new COLORREF(0x00FFFFFF));
        GdiText.DrawText(hdc, text, ref rect, DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER);
    }

    private void DrawStatusBar(HDC hdc)
    {
        if (!_showStatusBar)
        {
            return;
        }

        string text = $"{_state.Width}x{_state.Height} | Cursor: {_lastImagePoint.X},{_lastImagePoint.Y} | Zoom: {(int)Math.Round(_zoom * 100)}% | Tool: {_activeToolKind}";
        var rect = new RECT
        {
            left = 8,
            top = _viewportHeight - 28,
            right = _viewportWidth - 8,
            bottom = _viewportHeight - 8,
        };
        PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        PInvoke.SetTextColor(hdc, new COLORREF(0x00FFFFFF));
        GdiText.DrawText(hdc, text, ref rect, DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER);
    }
}
