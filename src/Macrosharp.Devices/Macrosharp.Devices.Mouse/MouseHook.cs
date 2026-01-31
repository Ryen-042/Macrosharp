using System.Runtime.InteropServices;
using Macrosharp.Devices.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Devices.Mouse;

/// <summary>Represents the state of a mouse button (down or up).</summary>
public enum MouseButtonState
{
    /// <summary>The button is pressed down.</summary>
    Down,

    /// <summary>The button is released.</summary>
    Up,
}

/// <summary>Represents the type of mouse event captured by the hook.</summary>
public enum MouseEventType
{
    /// <summary>Left button pressed down.</summary>
    LeftDown,

    /// <summary>Left button released.</summary>
    LeftUp,

    /// <summary>Right button pressed down.</summary>
    RightDown,

    /// <summary>Right button released.</summary>
    RightUp,

    /// <summary>Middle button pressed down.</summary>
    MiddleDown,

    /// <summary>Middle button released.</summary>
    MiddleUp,

    /// <summary>XButton1 pressed down.</summary>
    XButton1Down,

    /// <summary>XButton1 released.</summary>
    XButton1Up,

    /// <summary>XButton2 pressed down.</summary>
    XButton2Down,

    /// <summary>XButton2 released.</summary>
    XButton2Up,

    /// <summary>Vertical scroll wheel event.</summary>
    Scroll,

    /// <summary>Horizontal scroll wheel event.</summary>
    HorizontalScroll,

    /// <summary>Mouse movement event.</summary>
    Move,
}

/// <summary>Event arguments for mouse events captured by the low-level hook.</summary>
public class MouseEvent : EventArgs
{
    /// <summary>Gets the X coordinate of the mouse cursor (screen coordinates).</summary>
    public int X { get; }

    /// <summary>Gets the Y coordinate of the mouse cursor (screen coordinates).</summary>
    public int Y { get; }

    /// <summary>Gets the type of mouse event.</summary>
    public MouseEventType EventType { get; }

    /// <summary>Gets the mouse button involved in this event, if applicable.</summary>
    public MouseButtons Button { get; }

    /// <summary>Gets the button state (Down or Up) for button events.</summary>
    public MouseButtonState? ButtonState { get; }

    /// <summary>Gets the wheel delta for scroll events. Positive = up/right, negative = down/left.</summary>
    public short WheelDelta { get; }

    /// <summary>Gets the scroll direction for scroll events.</summary>
    public ScrollDirection? ScrollDirection { get; }

    /// <summary>Gets the raw flags value from MSLLHOOKSTRUCT.</summary>
    public uint Flags { get; }

    /// <summary>Gets the timestamp of the event (time since system start, in milliseconds).</summary>
    public uint Timestamp { get; }

    /// <summary>Gets or sets whether this event has been handled. If true, the event is suppressed.</summary>
    public bool Handled { get; set; }

    /// <summary>True if the event was injected by another process.</summary>
    public bool IsInjected => (Flags & 0x01) != 0;

    /// <summary>True if the event was injected by a process at lower integrity level.</summary>
    public bool IsLowerIntegrityInjected => (Flags & 0x02) != 0;

    /// <summary>Initializes a new instance of the <see cref="MouseEvent"/> class for button events.</summary>
    public MouseEvent(int x, int y, MouseEventType eventType, MouseButtons button, MouseButtonState buttonState, uint flags, uint timestamp)
    {
        X = x;
        Y = y;
        EventType = eventType;
        Button = button;
        ButtonState = buttonState;
        WheelDelta = 0;
        ScrollDirection = null;
        Flags = flags;
        Timestamp = timestamp;
        Handled = false;
    }

    /// <summary>Initializes a new instance of the <see cref="MouseEvent"/> class for scroll events.</summary>
    public MouseEvent(int x, int y, MouseEventType eventType, short wheelDelta, ScrollDirection scrollDirection, uint flags, uint timestamp)
    {
        X = x;
        Y = y;
        EventType = eventType;
        Button = MouseButtons.None;
        ButtonState = null;
        WheelDelta = wheelDelta;
        ScrollDirection = scrollDirection;
        Flags = flags;
        Timestamp = timestamp;
        Handled = false;
    }

    /// <summary>Initializes a new instance of the <see cref="MouseEvent"/> class for move events.</summary>
    public MouseEvent(int x, int y, uint flags, uint timestamp)
    {
        X = x;
        Y = y;
        EventType = MouseEventType.Move;
        Button = MouseButtons.None;
        ButtonState = null;
        WheelDelta = 0;
        ScrollDirection = null;
        Flags = flags;
        Timestamp = timestamp;
        Handled = false;
    }

    /// <summary>Returns a string representation of the mouse event.</summary>
    public override string ToString()
    {
        return EventType switch
        {
            MouseEventType.Scroll or MouseEventType.HorizontalScroll => $"Mouse: {EventType}, Delta={WheelDelta}, Pos=({X},{Y}), Inj={IsInjected}",
            MouseEventType.Move => $"Mouse: Move, Pos=({X},{Y}), Inj={IsInjected}",
            _ => $"Mouse: {EventType}, Button={Button}, Pos=({X},{Y}), Inj={IsInjected}",
        };
    }
}

/// <summary>
/// Manages a global low-level mouse hook (WH_MOUSE_LL).
/// This allows intercepting mouse events across all applications.
/// </summary>
public class MouseHookManager : IDisposable
{
    // WM_ constants for mouse messages (used in hook callback)
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WM_XBUTTONDOWN = 0x020B;
    private const uint WM_XBUTTONUP = 0x020C;
    private const uint WM_MOUSEHWHEEL = 0x020E;

    // XBUTTON indices from high-word of mouseData
    private const int XBUTTON1 = 1;
    private const int XBUTTON2 = 2;

    private HOOKPROC _proc;
    private HHOOK _hookID = default;

    /// <summary>
    /// Gets or sets whether mouse move events should be captured.
    /// Disabled by default for performance (move events are very high frequency).
    /// </summary>
    public bool CaptureMouseMove { get; set; } = false;

    /// <summary>Event that fires when a mouse button is pressed down.</summary>
    public event EventHandler<MouseEvent>? MouseButtonDownHandler;

    /// <summary>Event that fires when a mouse button is released.</summary>
    public event EventHandler<MouseEvent>? MouseButtonUpHandler;

    /// <summary>Event that fires when the mouse wheel is scrolled (vertical or horizontal).</summary>
    public event EventHandler<MouseEvent>? MouseScrollHandler;

    /// <summary>Event that fires when the mouse is moved. Only fires if CaptureMouseMove is true.</summary>
    public event EventHandler<MouseEvent>? MouseMoveHandler;

    /// <summary>Initializes a new instance of the <see cref="MouseHookManager"/> class.</summary>
    public MouseHookManager()
    {
        _proc = HookCallback;
    }

    /// <summary>Installs the global low-level mouse hook.</summary>
    public void Start()
    {
        if (!_hookID.IsNull)
        {
            return; // Hook is already installed.
        }

        HINSTANCE hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
        if (hInstance == HINSTANCE.Null)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        _hookID = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_MOUSE_LL, _proc, hInstance, 0);

        if (_hookID.IsNull)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>Uninstalls the global mouse hook.</summary>
    public void Stop()
    {
        if (!_hookID.IsNull)
        {
            BOOL success = PInvoke.UnhookWindowsHookEx(_hookID);
            _hookID = default;

            if (success.Value == 0)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    /// <summary>Hook callback for low-level mouse events.</summary>
    private LRESULT HookCallback(int nCode, WPARAM wParam, LPARAM lParam)
    {
        if (nCode < PInvoke.HC_ACTION)
            return PInvoke.CallNextHookEx(_hookID, nCode, wParam, lParam);

        MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam)!;

        int x = hookStruct.pt.X;
        int y = hookStruct.pt.Y;
        uint flags = (uint)hookStruct.flags;
        uint timestamp = hookStruct.time;

        MouseEvent? mouseEvent = null;

        switch (wParam.Value)
        {
            case WM_LBUTTONDOWN:
                mouseEvent = new MouseEvent(x, y, MouseEventType.LeftDown, MouseButtons.Left, MouseButtonState.Down, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonDownHandler?.Invoke(this, mouseEvent);
                break;

            case WM_LBUTTONUP:
                mouseEvent = new MouseEvent(x, y, MouseEventType.LeftUp, MouseButtons.Left, MouseButtonState.Up, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonUpHandler?.Invoke(this, mouseEvent);
                break;

            case WM_RBUTTONDOWN:
                mouseEvent = new MouseEvent(x, y, MouseEventType.RightDown, MouseButtons.Right, MouseButtonState.Down, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonDownHandler?.Invoke(this, mouseEvent);
                break;

            case WM_RBUTTONUP:
                mouseEvent = new MouseEvent(x, y, MouseEventType.RightUp, MouseButtons.Right, MouseButtonState.Up, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonUpHandler?.Invoke(this, mouseEvent);
                break;

            case WM_MBUTTONDOWN:
                mouseEvent = new MouseEvent(x, y, MouseEventType.MiddleDown, MouseButtons.Middle, MouseButtonState.Down, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonDownHandler?.Invoke(this, mouseEvent);
                break;

            case WM_MBUTTONUP:
                mouseEvent = new MouseEvent(x, y, MouseEventType.MiddleUp, MouseButtons.Middle, MouseButtonState.Up, flags, timestamp);
                MouseButtonModifiers.UpdateButtonState(mouseEvent);
                MouseButtonUpHandler?.Invoke(this, mouseEvent);
                break;

            case WM_XBUTTONDOWN:
                {
                    int xButton = GetXButtonFromMouseData(hookStruct.mouseData);
                    var button = xButton == XBUTTON1 ? MouseButtons.XButton1 : MouseButtons.XButton2;
                    var eventType = xButton == XBUTTON1 ? MouseEventType.XButton1Down : MouseEventType.XButton2Down;
                    mouseEvent = new MouseEvent(x, y, eventType, button, MouseButtonState.Down, flags, timestamp);
                    MouseButtonModifiers.UpdateButtonState(mouseEvent);
                    MouseButtonDownHandler?.Invoke(this, mouseEvent);
                }
                break;

            case WM_XBUTTONUP:
                {
                    int xButton = GetXButtonFromMouseData(hookStruct.mouseData);
                    var button = xButton == XBUTTON1 ? MouseButtons.XButton1 : MouseButtons.XButton2;
                    var eventType = xButton == XBUTTON1 ? MouseEventType.XButton1Up : MouseEventType.XButton2Up;
                    mouseEvent = new MouseEvent(x, y, eventType, button, MouseButtonState.Up, flags, timestamp);
                    MouseButtonModifiers.UpdateButtonState(mouseEvent);
                    MouseButtonUpHandler?.Invoke(this, mouseEvent);
                }
                break;

            case WM_MOUSEWHEEL:
                {
                    short wheelDelta = GetWheelDeltaFromMouseData(hookStruct.mouseData);
                    mouseEvent = new MouseEvent(x, y, MouseEventType.Scroll, wheelDelta, Core.ScrollDirection.Vertical, flags, timestamp);
                    MouseScrollHandler?.Invoke(this, mouseEvent);
                }
                break;

            case WM_MOUSEHWHEEL:
                {
                    short wheelDelta = GetWheelDeltaFromMouseData(hookStruct.mouseData);
                    mouseEvent = new MouseEvent(x, y, MouseEventType.HorizontalScroll, wheelDelta, Core.ScrollDirection.Horizontal, flags, timestamp);
                    MouseScrollHandler?.Invoke(this, mouseEvent);
                }
                break;

            case WM_MOUSEMOVE:
                if (CaptureMouseMove)
                {
                    mouseEvent = new MouseEvent(x, y, flags, timestamp);
                    MouseMoveHandler?.Invoke(this, mouseEvent);
                }
                break;
        }

        // Check if event should be suppressed
        if (mouseEvent != null && mouseEvent.Handled)
        {
            return (LRESULT)1;
        }

        return PInvoke.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    /// <summary>Extracts the wheel delta from the mouseData field (high-order word, signed).</summary>
    private static short GetWheelDeltaFromMouseData(uint mouseData)
    {
        return (short)((mouseData >> 16) & 0xFFFF);
    }

    /// <summary>Extracts the X button index from the mouseData field (high-order word).</summary>
    private static int GetXButtonFromMouseData(uint mouseData)
    {
        return (int)((mouseData >> 16) & 0xFFFF);
    }

    /// <summary>Disposes of the hook and releases resources.</summary>
    public void Dispose()
    {
        Stop();
    }
}
