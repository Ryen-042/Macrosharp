using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// The core image editor component that manages image rendering, view transformations, zooming, panning, and tool coordination.
/// This class is responsible for:
/// - Managing the viewport and view transformations (zoom and pan)
/// - Coordinating tool interactions (Draw, Pan, Crop, ColorPicker)
/// - Rendering the image with proper scaling and positioning
/// - Handling user input (mouse, keyboard, mouse wheel)
/// - Managing image state with undo/redo support
/// - Controlling the active tool and switching between tools
/// </summary>
public sealed class ImageEditor
{
    private const double ZoomMin = 0.1;
    private const double ZoomMax = 32.0;
    private const double ZoomEpsilon = 0.001;
    private const double ZoomStepIn = 1.1;
    private const double ZoomStepOut = 0.9;

    // State management
    private readonly ImageEditorState _state;
    private readonly Dictionary<ToolKind, IEditorTool> _tools;
    private IEditorTool _activeTool;
    private ToolKind _activeToolKind;

    // Viewport dimensions
    private int _viewportWidth;
    private int _viewportHeight;

    // View transformation (zoom and pan)
    private double _zoom = 1.0;
    private int _panX;
    private int _panY;

    // UI state
    private bool _showStatusBar;
    private bool _showHelp;
    private IntPoint _lastImagePoint;
    private IntPoint _lastScreenPoint;
    private bool _autoFit = true;
    private HWND _ownerHwnd = HWND.Null;

    // Ctrl+click panning state
    private bool _isPanningWithCtrl;
    private IntPoint _ctrlPanStart;

    // Toggle zoom state (for Space key)
    private double _savedZoom = 1.0;
    private int _savedPanX;
    private int _savedPanY;
    private double _fitZoomValue; // The zoom level when fit was applied

    public event Action<int, int>? WindowResizeRequested;

    /// <summary>
    /// Sets the owner window handle for file dialog operations.
    /// </summary>
    public void SetOwner(HWND hwnd)
    {
        _ownerHwnd = hwnd;
    }

    /// <summary>
    /// Initializes a new instance of the ImageEditor with default tools and blank canvas.
    /// </summary>
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
        if (!_state.HasLoadedImage)
        {
            _state.ResizeBlankToViewport(_viewportWidth, _viewportHeight);
            ResetPanZoom(autoFit: false, zoom: 1.0);
        }
        if (_autoFit)
        {
            UpdateFitZoom();
        }
    }

    /// <summary>
    /// Handles mouse down events. Delegates to the active tool.
    /// Ctrl+left-click starts panning regardless of active tool.
    /// </summary>
    public void HandleMouseDown(IntPoint point, MouseButton button, ModifierState modifiers)
    {
        // Ctrl+left-click initiates panning
        if (button == MouseButton.Left && modifiers.HasFlag(ModifierState.Control))
        {
            _isPanningWithCtrl = true;
            _ctrlPanStart = point;
            return;
        }

        var input = BuildInput(point, modifiers, button);
        _activeTool.OnMouseDown(this, input);
    }

    /// <summary>
    /// Handles mouse move events. Tracks cursor position and delegates to the active tool.
    /// If Ctrl+click panning is active, pans the view instead.
    /// </summary>
    public void HandleMouseMove(IntPoint point, ModifierState modifiers)
    {
        // Handle Ctrl+click panning
        if (_isPanningWithCtrl)
        {
            int dx = point.X - _ctrlPanStart.X;
            int dy = point.Y - _ctrlPanStart.Y;
            PanBy(dx, dy);
            _ctrlPanStart = point;
            return;
        }

        var input = BuildInput(point, modifiers, MouseButton.None);
        _lastScreenPoint = input.ScreenPoint;
        _lastImagePoint = input.ImagePoint;
        _activeTool.OnMouseMove(this, input);
    }

    /// <summary>
    /// Handles mouse up events. Delegates to the active tool and commits drawing operations.
    /// Ends Ctrl+click panning if active.
    /// </summary>
    public void HandleMouseUp(IntPoint point, MouseButton button, ModifierState modifiers)
    {
        // End Ctrl+click panning
        if (_isPanningWithCtrl && button == MouseButton.Left)
        {
            _isPanningWithCtrl = false;
            return;
        }

        var input = BuildInput(point, modifiers, button);
        _activeTool.OnMouseUp(this, input);
        if (_activeTool is DrawTool)
        {
            _state.CommitMatrixToRaster();
        }
    }

    /// <summary>
    /// Handles mouse wheel events for zooming.
    /// </summary>
    public void HandleMouseWheel(IntPoint point, int delta, ModifierState modifiers)
    {
        var input = BuildInput(point, modifiers, MouseButton.None, delta);
        _activeTool.OnMouseWheel(this, input);
    }

    /// <summary>
    /// Handles keyboard input for tool switching, image operations, and view manipulation.
    /// Keyboard shortcuts:
    /// - F1: Toggle status bar visibility
    /// - Shift+/: Toggle help overlay
    /// - Ctrl+O: Open image from file
    /// - Ctrl+V: Open image from clipboard
    /// - V: Flip image vertically
    /// - W: Switch to Draw tool
    /// - L: Switch to Crop tool
    /// - C: Switch to ColorPicker tool
    /// - P: Switch to Pan tool
    /// - Space: Toggle fit-to-screen zoom (press again to restore previous zoom)
    /// - Ctrl+Click: Pan view
    /// - Escape: Cancel current tool operation
    /// - Ctrl+Z: Undo
    /// - Ctrl+Y: Redo
    /// - Ctrl+R: Reset to original image
    /// - R: Rotate 90° clockwise
    /// - T: Apply grayscale filter
    /// - I: Apply invert filter
    /// - H: Flip horizontally
    /// - Ctrl+0: Reset view
    /// - 0: Black brush color (in DrawTool)
    /// - 1-9: Preset brush colors (in DrawTool)
    /// - +/-: Adjust brush size (in DrawTool)
    /// </summary>
    public void HandleKeyDown(VIRTUAL_KEY key, ModifierState modifiers)
    {
        if (key == VIRTUAL_KEY.VK_OEM_2 && modifiers.HasFlag(ModifierState.Shift))
        {
            _showHelp = !_showHelp;
            return;
        }

        if (key == VIRTUAL_KEY.VK_O && modifiers.HasFlag(ModifierState.Control))
        {
            TryOpenFromFileDialog();
            return;
        }

        if (key == VIRTUAL_KEY.VK_V && modifiers.HasFlag(ModifierState.Control))
        {
            TryOpenFromClipboard();
            return;
        }

        if (key == VIRTUAL_KEY.VK_Z && modifiers.HasFlag(ModifierState.Control))
        {
            _state.TryUndo();
            return;
        }

        if (key == VIRTUAL_KEY.VK_Y && modifiers.HasFlag(ModifierState.Control))
        {
            _state.TryRedo();
            return;
        }

        if (key == VIRTUAL_KEY.VK_R && modifiers.HasFlag(ModifierState.Control))
        {
            _state.ResetToOriginal();
            return;
        }

        if (key == VIRTUAL_KEY.VK_0 && modifiers.HasFlag(ModifierState.Control))
        {
            ResetView();
            return;
        }

        switch (key)
        {
            case VIRTUAL_KEY.VK_F1:
                _showStatusBar = !_showStatusBar;
                return;
            case VIRTUAL_KEY.VK_V:
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
            case VIRTUAL_KEY.VK_P:
                SetTool(ToolKind.Pan);
                return;
            case VIRTUAL_KEY.VK_SPACE:
                ToggleFitZoom();
                return;
            case VIRTUAL_KEY.VK_ESCAPE:
                _activeTool.OnCancel(this);
                return;
            case VIRTUAL_KEY.VK_R:
                Rotate90Clockwise();
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
        }

        _activeTool.OnKeyDown(this, key, modifiers);
    }

    /// <summary>
    /// Renders the complete editor including background, image, overlay, and status bar.
    /// </summary>
    public void Render(HDC hdc, int width, int height)
    {
        SetViewport(width, height);

        DrawBackground(hdc, width, height);
        DrawImage(hdc);
        DrawOverlay(hdc);
        DrawStatusBar(hdc);
        DrawHelpOverlay(hdc);
        _activeTool.OnRender(this, hdc, width, height);
    }

    public ImageEditorState State => _state;

    /// <summary>
    /// Gets the current view transform which combines zoom, pan, and viewport origin.
    /// Used to convert between screen and image coordinates.
    /// </summary>
    public ViewTransform Transform
    {
        get
        {
            var image = _state.GetMatrix();
            int destWidth = (int)Math.Round(image.Width * _zoom);
            int destHeight = (int)Math.Round(image.Height * _zoom);
            int originX = (_viewportWidth - destWidth) / 2;
            int originY = _state.HasLoadedImage ? 0 : (_viewportHeight - destHeight) / 2;
            return new ViewTransform(_zoom, _panX, _panY, originX, originY, _viewportWidth, _viewportHeight);
        }
    }

    /// <summary>
    /// Zooms at a specific screen point (anchor), keeping that point fixed on the image.
    /// This ensures zoom centers around the mouse cursor position.
    /// </summary>
    /// <param name="anchor">The screen point to zoom around</param>
    /// <param name="factor">The zoom factor (e.g., 1.1 to zoom in, 0.9 to zoom out)</param>
    public void ZoomAt(IntPoint anchor, double factor)
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
        {
            return;
        }

        double newZoom = Math.Clamp(_zoom * factor, ZoomMin, ZoomMax);
        if (Math.Abs(newZoom - _zoom) < ZoomEpsilon)
        {
            return;
        }

        _autoFit = false;

        // Get current transform to find anchor in image space
        var currentTransform = Transform;
        var imagePoint = currentTransform.ScreenToImage(anchor);

        // Apply the new zoom
        double oldZoom = _zoom;
        _zoom = newZoom;

        // Calculate new origin (which changes with zoom)
        var image = _state.GetMatrix();
        int newDestWidth = (int)Math.Round(image.Width * _zoom);
        int newDestHeight = (int)Math.Round(image.Height * _zoom);
        int newOriginX = (_viewportWidth - newDestWidth) / 2;
        int newOriginY = _state.HasLoadedImage ? 0 : (_viewportHeight - newDestHeight) / 2;

        // Adjust pan so anchor screen point maps to same image point after zoom
        // anchor.X = newOriginX + newPanX + imagePoint.X * newZoom
        // newPanX = anchor.X - newOriginX - imagePoint.X * newZoom
        _panX = (int)Math.Round(anchor.X - newOriginX - imagePoint.X * _zoom);
        _panY = (int)Math.Round(anchor.Y - newOriginY - imagePoint.Y * _zoom);
    }

    /// <summary>
    /// Zooms at the cursor position based on mouse wheel delta.
    /// Positive delta zooms in, negative zooms out.
    /// </summary>
    public void ZoomAtWheel(IntPoint anchor, int wheelDelta)
    {
        if (wheelDelta == 0)
        {
            return;
        }

        ZoomAt(anchor, GetWheelZoomFactor(wheelDelta));
    }

    /// <summary>
    /// Zooms at the viewport center point. Used for Ctrl+wheel operations.
    /// </summary>
    /// <param name="wheelDelta">The mouse wheel delta value</param>
    public void ZoomAtViewportCenter(int wheelDelta)
    {
        if (wheelDelta == 0)
        {
            return;
        }

        var center = new IntPoint(_viewportWidth / 2, _viewportHeight / 2);
        ZoomAt(center, GetWheelZoomFactor(wheelDelta));
    }

    public double GetWheelZoomFactor(int wheelDelta)
    {
        return wheelDelta > 0 ? ZoomStepIn : ZoomStepOut;
    }

    /// <summary>
    /// Pans the view by the specified delta in screen coordinates.
    /// </summary>
    public void PanBy(int dx, int dy)
    {
        _panX += dx;
        _panY += dy;
    }

    /// <summary>
    /// Resets the view to either fit the image in viewport or return to normal state.
    /// </summary>
    public void ResetView()
    {
        ResetPanZoom(autoFit: true, zoom: _zoom);
    }

    /// <summary>
    /// Toggles between fit-to-screen zoom and the previously saved zoom/pan state.
    /// If currently at fit zoom (unchanged), restores saved state.
    /// Otherwise, saves current state and fits to screen.
    /// </summary>
    private void ToggleFitZoom()
    {
        // Check if we're still at the fit zoom level (user hasn't zoomed since fitting)
        bool stillAtFitZoom = Math.Abs(_zoom - _fitZoomValue) < ZoomEpsilon && _panX == 0 && _panY == 0;

        if (stillAtFitZoom && _fitZoomValue > 0)
        {
            // Restore saved zoom/pan state
            _zoom = _savedZoom;
            _panX = _savedPanX;
            _panY = _savedPanY;
            _autoFit = false;
            _fitZoomValue = 0; // Clear fit state
        }
        else
        {
            // Save current state and fit to screen
            _savedZoom = _zoom;
            _savedPanX = _panX;
            _savedPanY = _panY;
            _panX = 0;
            _panY = 0;
            _autoFit = true;
            UpdateFitZoom();
            _fitZoomValue = _zoom; // Remember the fit zoom level
        }
    }

    /// <summary>
    /// Switches to a different tool. Cancels the current tool's operation.
    /// </summary>
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

    /// <summary>
    /// Gets the DrawTool instance for setting brush properties.
    /// </summary>
    public DrawTool GetDrawTool() => (DrawTool)_tools[ToolKind.Draw];

    /// <summary>
    /// Sets the brush color for the draw tool.
    /// </summary>
    public void SetBrushColor(int argb)
    {
        GetDrawTool().SetBrushColor(argb);
    }

    /// <summary>
    /// Applies a crop to the image and resets the view.
    /// </summary>
    public void ApplyCrop(IntRect rect)
    {
        _state.ApplyCrop(rect);
        ResetView();
    }

    /// <summary>
    /// Attempts to load an image from the clipboard.
    /// </summary>
    public bool TryOpenFromClipboard()
    {
        if (ImageEditorIO.TryLoadFromClipboard(out var buffer) && buffer != null)
        {
            ApplyLoadedImage(buffer);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Opens a file dialog and attempts to load the selected image.
    /// </summary>
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

    /// <summary>
    /// Attempts to load an image from the specified file path.
    /// </summary>
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
        ResetPanZoom(autoFit: false, zoom: 1.0);

        if (buffer.Width > _viewportWidth || buffer.Height > _viewportHeight)
        {
            WindowResizeRequested?.Invoke(buffer.Width, buffer.Height);
        }
    }

    /// <summary>
    /// Applies a grayscale filter using standard luminosity weights.
    /// </summary>
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
                // Calculate gray value using luminosity formula (HDTV standard)
                int gray = (r * 77 + g * 150 + b * 29) >> 8;
                buffer.Pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray;
            }
        });
    }

    /// <summary>
    /// Inverts the RGB values of all pixels while preserving alpha.
    /// </summary>
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

    /// <summary>
    /// Flips the image horizontally (mirror effect).
    /// </summary>
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

    /// <summary>
    /// Flips the image vertically (upside down).
    /// </summary>
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

    /// <summary>
    /// Builds an EditorInput object from raw input parameters and current state.
    /// Converts screen coordinates to image coordinates.
    /// </summary>
    private EditorInput BuildInput(IntPoint point, ModifierState modifiers, MouseButton button, int wheelDelta = 0)
    {
        var transform = Transform;
        var imagePoint = transform.ScreenToImage(point);
        return new EditorInput(point, imagePoint, modifiers, button, wheelDelta);
    }

    /// <summary>
    /// Renders the complete editor including background, image, overlay, and status bar.
    /// </summary>
    /// <summary>
    /// Draws the dark background fill color.
    /// </summary>
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

    /// <summary>
    /// Draws the image with current zoom and pan transformations using StretchDIBits.
    /// </summary>
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

        // Create DIB header for pixel data
        BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = image.Width;
        bmi.bmiHeader.biHeight = -image.Height; // Negative for top-down orientation
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = (uint)BI_COMPRESSION.BI_RGB;

        // Stretch the image buffer to the destination size with scaling
        fixed (int* pixels = image.Pixels)
        {
            PInvoke.StretchDIBits(hdc, destX, destY, destWidth, destHeight, 0, 0, image.Width, image.Height, pixels, &bmi, DIB_USAGE.DIB_RGB_COLORS, ROP_CODE.SRCCOPY);
        }

        DrawCanvasBorder(hdc, destX, destY, destWidth, destHeight);
    }

    /// <summary>
    /// Draws a border around the canvas to show its boundaries.
    /// </summary>
    private void DrawCanvasBorder(HDC hdc, int x, int y, int width, int height)
    {
        if (width <= 1 || height <= 1)
        {
            return;
        }

        var rect = new RECT
        {
            left = x,
            top = y,
            right = x + width,
            bottom = y + height,
        };
        HBRUSH brush = PInvoke.CreateSolidBrush(new COLORREF(0x00555555));
        using var safeBrush = new SafeBrushHandle(brush);
        PInvoke.FrameRect(hdc, rect, safeBrush);
    }

    /// <summary>
    /// Updates the zoom level to fit the entire image within the viewport.
    /// </summary>
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
        _zoom = Math.Clamp(Math.Min(fitX, fitY), ZoomMin, ZoomMax);
    }

    /// <summary>
    /// Resets pan and zoom state to initial values or fit-to-viewport.
    /// </summary>
    private void ResetPanZoom(bool autoFit, double zoom)
    {
        _autoFit = autoFit;
        _panX = 0;
        _panY = 0;
        _zoom = zoom;

        if (_autoFit)
        {
            UpdateFitZoom();
        }
    }

    /// <summary>
    /// Draws the overlay text showing the current active tool.
    /// </summary>
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

    /// <summary>
    /// Draws the optional status bar showing image dimensions, cursor position, zoom level, and active tool.
    /// </summary>
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

    /// <summary>
    /// Draws the help overlay showing keyboard shortcuts.
    /// </summary>
    private void DrawHelpOverlay(HDC hdc)
    {
        if (!_showHelp)
        {
            return;
        }

        // Semi-transparent dark background
        int padding = 20;
        int boxWidth = 320;
        int boxHeight = 380;
        int boxX = (_viewportWidth - boxWidth) / 2;
        int boxY = (_viewportHeight - boxHeight) / 2;

        var bgRect = new RECT
        {
            left = boxX,
            top = boxY,
            right = boxX + boxWidth,
            bottom = boxY + boxHeight,
        };
        HBRUSH bgBrush = PInvoke.CreateSolidBrush(new COLORREF(0x00303030));
        using var safeBgBrush = new SafeBrushHandle(bgBrush);
        PInvoke.FillRect(hdc, bgRect, safeBgBrush);

        // Border
        HBRUSH borderBrush = PInvoke.CreateSolidBrush(new COLORREF(0x00888888));
        using var safeBorderBrush = new SafeBrushHandle(borderBrush);
        PInvoke.FrameRect(hdc, bgRect, safeBorderBrush);

        // Help text content
        string[] lines = new[]
        {
            "KEYBOARD SHORTCUTS",
            "",
            "Tools:",
            "  W - Draw tool",
            "  L - Crop tool",
            "  C - Color picker",
            "  P - Pan tool",
            "",
            "View:",
            "  Space - Toggle fit zoom",
            "  Ctrl+Click - Pan",
            "  Ctrl+0 - Reset view",
            "  F1 - Toggle status bar",
            "",
            "Image:",
            "  Ctrl+O - Open file",
            "  Ctrl+V - Paste from clipboard",
            "  Ctrl+Z/Y - Undo/Redo",
            "  R - Rotate 90 degrees",
            "  H/V - Flip horizontal/vertical",
            "  T - Grayscale  |  I - Invert",
            "",
            "Brush: 0-9 colors, +/- size",
        };

        PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
        PInvoke.SetTextColor(hdc, new COLORREF(0x00FFFFFF));

        int lineHeight = 14;
        int textY = boxY + padding;
        foreach (var line in lines)
        {
            var lineRect = new RECT
            {
                left = boxX + padding,
                top = textY,
                right = boxX + boxWidth - padding,
                bottom = textY + lineHeight,
            };
            GdiText.DrawText(hdc, line, ref lineRect, DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_SINGLELINE);
            textY += lineHeight;
        }
    }
}
