using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Win32.Abstractions.WindowTools;

public class Messaging
{
    /// <summary>Sends a message to the specified window and waits for processing. If no window is specified, uses the foreground window.</summary>
    public static LRESULT SendMessageToWindow(HWND hwnd, uint message, WPARAM wParam = default, LPARAM lParam = default)
    {
        try
        {
            return PInvoke.SendMessage(hwnd, message, wParam, lParam);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            return default;
        }
    }

    /// <summary>Overload that sends a string message as lParam.</summary>
    public static LRESULT SendMessageToWindow(HWND hwnd, uint message, WPARAM wParam, string lParam)
    {
        // Allocate memory for the string lParam.
        nint lParamPtr = Marshal.StringToHGlobalUni(lParam);
        try
        {
            return SendMessageToWindow(hwnd, message, wParam, (LPARAM)lParamPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(lParamPtr);
        }
    }

    /// <summary>Posts a message to the specified window and returns immediately. If no window is specified, uses the foreground window.</summary>
    public static bool PostMessageToWindow(HWND hwnd, uint message, WPARAM wParam = default, LPARAM lParam = default)
    {
        if (hwnd == HWND.Null)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        return PInvoke.PostMessage(hwnd, message, wParam, lParam);
    }

    /// <summary>Posts a message to a specific thread.</summary>
    public static bool PostMessageToThread(uint threadId, uint message, WPARAM wParam = default, LPARAM lParam = default)
    {
        return PInvoke.PostThreadMessage(threadId, message, wParam, lParam);
    }
}
