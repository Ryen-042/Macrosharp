using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Abstractions.WindowTools;

public class WindowModifier
{
    // Note: In your NativeMethods.txt you should list
    //   GetForegroundWindow
    //   SetWindowPos
    //   GetWindowRect
    //   GetWindowLongW
    //   SetWindowLongW
    //   GetLayeredWindowAttributes
    //   SetLayeredWindowAttributes
    // to have CsWin32 generate these APIs.

    /// <summary>Toggles the always-on-top state of the specified window. If no window is specified (hwnd is default), uses the foreground window. Returns 0 if the window was topmost, 1 if not.</summary>
    public static int ToggleAlwaysOnTopState(HWND hwnd)
    {
        if (hwnd == HWND.Null)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        // Retrieve the current extended window style.
        // In CsWin32, the function GetWindowLong returns an nint; we cast that to the WINDOW_EX_STYLE enum.
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        bool isAlwaysOnTop = exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST);

        // Toggle the always-on-top state.
        BOOL success = PInvoke.SetWindowPos(hwnd, isAlwaysOnTop ? HWND.HWND_NOTOPMOST : HWND.HWND_TOPMOST, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);

        if (success.Value == 0)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        return isAlwaysOnTop ? 0 : 1;
    }

    ///<summary> Adjusts the position and size of the given window by adding the specified deltas. If no window is specified (hwnd is default), uses the foreground window.</summary>
    public static void AdjustWindowPositionAndSize(HWND hwnd = default, int deltaX = 0, int deltaY = 0, int deltaWidth = 0, int deltaHeight = 0)
    {
        if (hwnd == HWND.Null)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        // Get the current window rectangle.
        if (!PInvoke.GetWindowRect(hwnd, out RECT rect))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        int newX = rect.left + deltaX;
        int newY = rect.top + deltaY;
        int newWidth = rect.right - rect.left + deltaWidth;
        int newHeight = rect.bottom - rect.top + deltaHeight;

        BOOL resizeSuccess = PInvoke.SetWindowPos(
            hwnd,
            HWND.Null, // No z-order change.
            newX,
            newY,
            newWidth,
            newHeight,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
        );

        if (resizeSuccess.Value == 0)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>Adjusts the window�s opacity by the given delta (clipped to the range of 5 and 255). If the window isn�t already layered, it sets the WS_EX_LAYERED style.</summary>
    public static bool AdjustWindowOpacity(HWND hwnd = default, int opacityDelta = 0)
    {
        if (hwnd == HWND.Null)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        try
        {
            // Get the current extended window style.
            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            byte currentOpacity;
            if (!exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_LAYERED))
            {
                // Add the layered window style.
                exStyle |= WINDOW_EX_STYLE.WS_EX_LAYERED;
                _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);
                currentOpacity = 255;
            }
            else
            {
                // Get the current opacity.
                unsafe
                {
                    COLORREF pcrKey;
                    byte pbAlpha;
                    if (!PInvoke.GetLayeredWindowAttributes(hwnd, &pcrKey, &pbAlpha, null))
                        return false;
                    currentOpacity = pbAlpha;
                }
            }

            int newOpacity = Math.Clamp(currentOpacity + opacityDelta, 5, 255);
            return PInvoke.SetLayeredWindowAttributes(hwnd, new COLORREF(0), (byte)newOpacity, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
        }
        catch
        {
            return false;
        }
    }
}
