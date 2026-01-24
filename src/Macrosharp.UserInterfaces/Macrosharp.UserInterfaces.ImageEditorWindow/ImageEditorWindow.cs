using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class ImageEditorWindow : IDisposable
{
    private const string WindowClassName = "Macrosharp.ImageEditorWindow";

    private readonly string _title;
    private readonly ImageEditor _editor;
    private readonly WNDPROC _wndProc;
    private HWND _hwnd;
    private GCHandle _selfHandle;
    private int _clientWidth;
    private int _clientHeight;
    private bool _isMouseCaptured;
    private string? _pendingFilePath;
    private bool _pendingClipboard;

    public ImageEditorWindow(string title)
    {
        _title = title;
        _editor = new ImageEditor();
        _editor.WindowResizeRequested += ResizeWindowToClient;
        _wndProc = StaticWndProc;
    }

    public int Run()
    {
        RegisterClass();
        CreateWindow();
        LoadPendingImage();
        ShowWindow();
        return MessageLoop();
    }

    public void QueueOpenFromFile(string path)
    {
        _pendingFilePath = path;
        _pendingClipboard = false;
    }

    public void QueueOpenFromClipboard()
    {
        _pendingClipboard = true;
        _pendingFilePath = null;
    }

    public void Dispose()
    {
        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void RegisterClass()
    {
        unsafe
        {
            fixed (char* className = WindowClassName)
            {
                var wc = new WNDCLASSW
                {
                    style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                    lpfnWndProc = _wndProc,
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                    hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.DKGRAY_BRUSH).Value),
                    lpszClassName = new PCWSTR(className),
                };

                ushort atom = PInvoke.RegisterClass(wc);
                if (atom == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != 1410) // Class already exists
                    {
                        throw new InvalidOperationException($"Failed to register window class. Error: {error}");
                    }
                }
            }
        }
    }

    private void CreateWindow()
    {
        _selfHandle = GCHandle.Alloc(this);
        unsafe
        {
            _hwnd = PInvoke.CreateWindowEx(
                0,
                WindowClassName,
                _title,
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE,
                PInvoke.CW_USEDEFAULT,
                PInvoke.CW_USEDEFAULT,
                1200,
                800,
                HWND.Null,
                default,
                new SafeFileHandle(PInvoke.GetModuleHandle((PCWSTR)null), ownsHandle: false),
                GCHandle.ToIntPtr(_selfHandle).ToPointer()
            );
        }

        if (_hwnd == HWND.Null)
        {
            throw new InvalidOperationException("Failed to create window.");
        }

        _editor.SetOwner(_hwnd);
        UpdateClientSize();
    }

    private void ShowWindow()
    {
        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
        PInvoke.UpdateWindow(_hwnd);
    }

    private int MessageLoop()
    {
        MSG msg = new();
        while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }

        return (int)msg.wParam.Value;
    }

    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        nint userData = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
        if (msg == PInvoke.WM_NCCREATE)
        {
            unsafe
            {
                var cs = (CREATESTRUCTW*)lParam.Value;
                if (cs != null)
                {
                    PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (nint)cs->lpCreateParams);
                    userData = (nint)cs->lpCreateParams;
                }
            }
        }

        if (userData == 0)
        {
            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is not ImageEditorWindow window)
        {
            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return window.InstanceWndProc(hwnd, msg, wParam, lParam);
    }

    private LRESULT InstanceWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                PInvoke.UnregisterClass(WindowClassName, default);
                return (LRESULT)0;

            case PInvoke.WM_SIZE:
                _clientWidth = (int)(lParam.Value & 0xFFFF);
                _clientHeight = (int)((lParam.Value >> 16) & 0xFFFF);
                _editor.SetViewport(_clientWidth, _clientHeight);
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_ERASEBKGND:
                return (LRESULT)1;

            case PInvoke.WM_LBUTTONDOWN:
                PInvoke.SetCapture(hwnd);
                _isMouseCaptured = true;
                _editor.HandleMouseDown(GetPoint(lParam), MouseButton.Left, GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_LBUTTONUP:
                if (_isMouseCaptured)
                {
                    _isMouseCaptured = false;
                    PInvoke.ReleaseCapture();
                }
                _editor.HandleMouseUp(GetPoint(lParam), MouseButton.Left, GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_MOUSEMOVE:
                _editor.HandleMouseMove(GetPoint(lParam), GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_RBUTTONDOWN:
                PInvoke.SetCapture(hwnd);
                _isMouseCaptured = true;
                _editor.HandleMouseDown(GetPoint(lParam), MouseButton.Right, GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_RBUTTONUP:
                if (_isMouseCaptured)
                {
                    _isMouseCaptured = false;
                    PInvoke.ReleaseCapture();
                }
                _editor.HandleMouseUp(GetPoint(lParam), MouseButton.Right, GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_MOUSEWHEEL:
                _editor.HandleMouseWheel(GetPoint(lParam), GetWheelDelta(wParam), GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_KEYDOWN:
                _editor.HandleKeyDown((VIRTUAL_KEY)wParam.Value, GetModifierState());
                Invalidate();
                return (LRESULT)0;

            case PInvoke.WM_PAINT:
                Paint();
                return (LRESULT)0;
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private unsafe void Paint()
    {
        PAINTSTRUCT ps;
        HDC hdc = PInvoke.BeginPaint(_hwnd, out ps);
        if (hdc == HDC.Null)
        {
            return;
        }

        int width = Math.Max(1, _clientWidth);
        int height = Math.Max(1, _clientHeight);

        HDC memoryDc = PInvoke.CreateCompatibleDC(hdc);
        HBITMAP bitmap = PInvoke.CreateCompatibleBitmap(hdc, width, height);
        HGDIOBJ oldBitmap = PInvoke.SelectObject(memoryDc, bitmap);

        _editor.Render(memoryDc, width, height);
        PInvoke.BitBlt(hdc, 0, 0, width, height, memoryDc, 0, 0, ROP_CODE.SRCCOPY);

        PInvoke.SelectObject(memoryDc, oldBitmap);
        PInvoke.DeleteObject(bitmap);
        PInvoke.DeleteDC(memoryDc);
        PInvoke.EndPaint(_hwnd, ps);
    }

    private void Invalidate()
    {
        PInvoke.InvalidateRect(_hwnd, null, false);
    }

    private void UpdateClientSize()
    {
        if (PInvoke.GetClientRect(_hwnd, out var rect))
        {
            _clientWidth = rect.right - rect.left;
            _clientHeight = rect.bottom - rect.top;
            _editor.SetViewport(_clientWidth, _clientHeight);
        }
    }

    private void ResizeWindowToClient(int clientWidth, int clientHeight)
    {
        if (_hwnd == HWND.Null)
        {
            return;
        }

        clientWidth = Math.Max(1, clientWidth);
        clientHeight = Math.Max(1, clientHeight);

        RECT rect = new()
        {
            left = 0,
            top = 0,
            right = clientWidth,
            bottom = clientHeight,
        };

        PInvoke.AdjustWindowRectEx(ref rect, WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE, false, 0);
        int windowWidth = rect.right - rect.left;
        int windowHeight = rect.bottom - rect.top;
        PInvoke.SetWindowPos(_hwnd, HWND.Null, 0, 0, windowWidth, windowHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    private void LoadPendingImage()
    {
        if (!string.IsNullOrWhiteSpace(_pendingFilePath))
        {
            _editor.TryOpenFromFile(_pendingFilePath);
        }
        else if (_pendingClipboard)
        {
            _editor.TryOpenFromClipboard();
        }

        _pendingFilePath = null;
        _pendingClipboard = false;
    }

    private static IntPoint GetPoint(LPARAM lParam)
    {
        int x = (short)(lParam.Value & 0xFFFF);
        int y = (short)((lParam.Value >> 16) & 0xFFFF);
        return new IntPoint(x, y);
    }

    private static int GetWheelDelta(WPARAM wParam)
    {
        return (short)((wParam.Value >> 16) & 0xFFFF);
    }

    private static ModifierState GetModifierState()
    {
        ModifierState state = ModifierState.None;
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CONTROL) & 0x8000) != 0)
        {
            state |= ModifierState.Control;
        }

        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_SHIFT) & 0x8000) != 0)
        {
            state |= ModifierState.Shift;
        }

        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_MENU) & 0x8000) != 0)
        {
            state |= ModifierState.Alt;
        }

        return state;
    }
}
