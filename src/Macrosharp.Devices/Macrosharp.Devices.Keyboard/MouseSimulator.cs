using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32; // For PInvoke access to generated constants and methods
using Windows.Win32.Foundation; // For HWND, LPARAM, WPARAM, POINT
using Windows.Win32.UI.Input.KeyboardAndMouse; // For MOUSEEVENTF_* constants
using Windows.Win32.UI.WindowsAndMessaging; // For WM_* constants (PostMessage)

namespace Macrosharp.Devices.Keyboard; // Assuming the same namespace for easy integration


/// <summary>Defines the various mouse buttons that can be simulated.</summary>
public enum MouseButton
{
    /// <summary>Represents the left mouse button.</summary>
    LeftButton = 1,
    /// <summary>Represents the right mouse button.</summary>
    RightButton = 2,
    /// <summary>Represents the middle mouse button (wheel click).</summary>
    MiddleButton = 3,
    /// <summary>Represents the first X button (XBUTTON1).</summary>
    XButton1 = 4,
    /// <summary>Represents the second X button (XBUTTON2).</summary>
    XButton2 = 5
}

/// <summary>Defines the types of mouse click operations.</summary>
public enum MouseEventOperation
{
    /// <summary>Sends both mouse button down and up events (a complete click).</summary>
    Click = 1,
    /// <summary>Sends only a mouse button down event.</summary>
    MouseDown = 2,
    /// <summary>Sends only a mouse button up event.</summary>
    MouseUp = 3
}

/// <summary>Defines constants for mouse button states, indicating which buttons are pressed.</summary>
enum MK
{
    /// <summary>The left mouse button is down.</summary>
    LBUTTON = 0x1,
    /// <summary>The right mouse button is down.</summary>
    RBUTTON = 0x2,
    /// <summary>The shift key is down.</summary>
    SHIFT = 0x4,
    /// <summary>The control key is down.</summary>
    CONTROL = 0x8,
    /// <summary>The middle mouse button is down.</summary>
    MBUTTON = 0x10,
    /// <summary>The XBUTTON1 is down.</summary>
    XBUTTON1 = 0x20,
    /// <summary>The XBUTTON2 is down.</summary>
    XBUTTON2 = 0x40
}


/// <summary>Provides methods for simulating mouse input, including clicks, movement, and scrolling.</summary>
public static class MouseSimulator
{
    /// <summary>Sends a mouse click to the given position. If no position is specified, the current cursor position is used.</summary>
    /// <param name="x">The x-coordinate for the click. If both x and y are -1, the current cursor position is used.</param>
    /// <param name="y">The y-coordinate for the click. If both x and y are -1, the current cursor position is used.</param>
    /// <param name="button">Specifies which mouse button to click.</param>
    /// <param name="op">Specifies the type of mouse event to send.</param>
    public static void SendMouseClick(int x = -1, int y = -1, MouseButton button = MouseButton.LeftButton, MouseEventOperation op = MouseEventOperation.Click)
    {
        if (x == -1 && y == -1)
        {
            PInvoke.GetCursorPos(out Point currentPos);
            x = currentPos.X;
            y = currentPos.Y;
        }

        MOUSE_EVENT_FLAGS mouseDownFlags = 0;
        MOUSE_EVENT_FLAGS mouseUpFlags = 0;
        uint xButtonData = 0; // For XButton clicks, mouseData parameter needs to be set

        switch (button)
        {
            case MouseButton.LeftButton:
                mouseDownFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN;
                mouseUpFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP;
                break;
            case MouseButton.RightButton:
                mouseDownFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN;
                mouseUpFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP;
                break;
            case MouseButton.MiddleButton:
                mouseDownFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN;
                mouseUpFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP;
                break;
            case MouseButton.XButton1:
                mouseDownFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_XDOWN;
                mouseUpFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_XUP;
                xButtonData = 0x0001;
                break;
            case MouseButton.XButton2:
                mouseDownFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_XDOWN;
                mouseUpFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_XUP;
                xButtonData = 0x0002;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button), "Invalid mouse button specified.");
        }

        var inputs = new List<INPUT>();

        // Move to the requested position first to ensure the click happens at (x, y).
        AddAbsoluteMove(inputs, x, y);

        if (op == MouseEventOperation.Click || op == MouseEventOperation.MouseDown)
        {
            inputs.Add(CreateMouseInput(mouseDownFlags, 0, 0, xButtonData));
        }

        if (op == MouseEventOperation.Click || op == MouseEventOperation.MouseUp)
        {
            inputs.Add(CreateMouseInput(mouseUpFlags, 0, 0, xButtonData));
        }

        SendInput(inputs);
    }

    /// <summary>Sends a mouse click to the given location in the specified window without moving the mouse cursor.</summary>
    /// <remarks>Note: This sends messages directly to the window, which might be ignored by some applications if they are not actively listening for them or if security restrictions apply.</remarks>
    /// <param name="hwnd">The handle to the window.</param>
    /// <param name="x">The x-coordinate relative to the client area of the window.</param>
    /// <param name="y">The y-coordinate relative to the client area of the window.</param>
    /// <param name="button">Specifies which mouse button to click.</param>
    public static void SendMouseClickToWindow(IntPtr hwnd, ushort x, ushort y, MouseButton button = MouseButton.LeftButton)
    {
        LPARAM l_param = PInvoke.MAKELPARAM(x, y);

        switch (button)
        {
            case MouseButton.LeftButton:
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_LBUTTONDOWN, new WPARAM((nuint)MK.LBUTTON), l_param);
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_LBUTTONUP, new WPARAM((nuint)MK.LBUTTON), l_param);
                break;
            case MouseButton.RightButton:
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_RBUTTONDOWN, new WPARAM(0), l_param);
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_RBUTTONUP, new WPARAM(0), l_param);
                break;
            case MouseButton.MiddleButton:
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_MBUTTONDOWN, new WPARAM(0), l_param);
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_MBUTTONUP, new WPARAM(0), l_param);
                break;
            case MouseButton.XButton1:
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_XBUTTONDOWN, new WPARAM(0), l_param);
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_XBUTTONUP, new WPARAM(0), l_param);
                break;
            case MouseButton.XButton2:
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_XBUTTONDOWN, new WPARAM(0), l_param);
                EnsurePostMessage(new HWND(hwnd), PInvoke.WM_XBUTTONUP, new WPARAM(0), l_param);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button), "Invalid mouse button specified.");
        }
    }

    /// <summary>Moves the mouse cursor some distance (dx, dy) away from its current position.</summary>
    /// <param name="dx">The distance to move horizontally from the current position.</param>
    /// <param name="dy">The distance to move vertically from the current position.</param>
    public static void MoveCursor(int dx = 0, int dy = 0)
    {
        PInvoke.GetCursorPos(out Point currentPos);
        PInvoke.SetCursorPos(currentPos.X + dx, currentPos.Y + dy);
    }

    /// <summary>Sends a mouse scroll event with the specified number of steps and direction.</summary>
    /// <param name="steps">The number of steps to scroll. Positive values scroll up/right, negative values scroll down/left.</param>
    /// <param name="direction">The direction to scroll in: 1 for vertical (default), 0 for horizontal.</param>
    /// <param name="wheelDelta">The amount of scroll per step. The system default is typically 120 (PInvoke.WHEEL_DELTA).</param>
    public static void SendMouseScroll(int steps = 1, int direction = 1, int wheelDelta = 120) // Default wheelDelta is 120 (WHEEL_DELTA)
    {
        MOUSE_EVENT_FLAGS dwFlags;
        int dwData = steps * wheelDelta; // dwData holds the wheel movement amount

        if (direction == 1) // Vertical scroll
        {
            dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL;
        }
        else if (direction == 0) // Horizontal scroll
        {
            dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_HWHEEL;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(direction), "Invalid scroll direction. Use 0 for horizontal or 1 for vertical.");
        }

        var inputs = new List<INPUT>
        {
            CreateMouseInput(dwFlags, 0, 0, unchecked((uint)dwData))
        };

        SendInput(inputs);
    }

    private static void AddAbsoluteMove(List<INPUT> inputs, int x, int y)
    {
        int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        int screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);

        int normalizedX = NormalizeAbsoluteCoordinate(x, screenWidth);
        int normalizedY = NormalizeAbsoluteCoordinate(y, screenHeight);

        inputs.Add(CreateMouseInput(
            MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE,
            normalizedX,
            normalizedY,
            0));
    }

    private static int NormalizeAbsoluteCoordinate(int coordinate, int max)
    {
        if (max <= 1)
            return 0;

        return (int)Math.Round(coordinate * 65535.0 / (max - 1));
    }

    private static INPUT CreateMouseInput(MOUSE_EVENT_FLAGS flags, int dx, int dy, uint mouseData)
    {
        return new INPUT
        {
            type = INPUT_TYPE.INPUT_MOUSE,
            Anonymous = new INPUT._Anonymous_e__Union
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = 0
                }
            }
        };
    }

    private static void SendInput(List<INPUT> inputs)
    {
        if (inputs.Count == 0)
            return;

        uint sent = PInvoke.SendInput(CollectionsMarshal.AsSpan(inputs), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    private static void EnsurePostMessage(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (!PInvoke.PostMessage(hwnd, msg, wParam, lParam))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
}
