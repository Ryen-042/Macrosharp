using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Abstractions.WindowTools;

public class WindowFinder
{
    // Retrieves the class name of the window.
    public static string GetWindowClassName(HWND hwnd = default)
    {
        if (hwnd == HWND.Null)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        int nChars = 256; // Maximum length of a class name is 256 characters (WNDCLASSW.lpszClassName).
        unsafe
        {
            fixed (char* windowClassNameChars = new char[nChars])
            {
                var windowClassNameLength = PInvoke.GetClassName(hwnd, windowClassNameChars, nChars);
                if (windowClassNameLength == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != 0)
                        throw new Win32Exception(errorCode);

                    return "";
                }

                return new string(windowClassNameChars, 0, windowClassNameLength);
            }
        }

        // We could also use stackalloc, but keep in mind that it could lead to stack overflow.
        //unsafe
        //{
        //    char* className = stackalloc char[nChars];
        //    int length = PInvoke.GetClassName(hwnd, className, nChars);
        //    return new string(className);
        //}
    }

    // Retrieves the window title (text) of the window.
    public static string GetWindowTitle(HWND hwnd)
    {
        int nChars = PInvoke.GetWindowTextLength(hwnd) + 1; // +1 for null terminator
        unsafe
        {
            fixed (char* windowTitleChars = new char[nChars])
            {
                var windowTextLength = PInvoke.GetWindowText(hwnd, windowTitleChars, nChars);
                if (windowTextLength == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != 0)
                        throw new Win32Exception(errorCode);

                    return "";
                }

                return new string(windowTitleChars, 0, windowTextLength);
            }
        }
    }

    // Finds all window handles with the specified class name.
    public static List<HWND> GetHwndByClassName(string className, bool checkAll = false)
    {
        var output = new List<HWND>();
        HWND hwnd = PInvoke.GetTopWindow(HWND.Null);

        while (hwnd != HWND.Null)
        {
            if (GetWindowClassName(hwnd) == className)
            {
                if (!checkAll)
                    return [hwnd];

                output.Add(hwnd);
            }

            hwnd = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_HWNDNEXT);
        }

        return output;
    }

    // Finds the first window handle with the specified title.
    public static List<HWND> GetHwndByTitle(string title, bool checkAll = false)
    {
        var output = new List<HWND>();
        HWND hwnd = PInvoke.GetTopWindow(HWND.Null);

        while (hwnd != HWND.Null)
        {
            if (GetWindowTitle(hwnd) == title)
            {
                if (!checkAll)
                    return [hwnd];

                output.Add(hwnd);
            }

            hwnd = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_HWNDNEXT);
        }

        return output;
    }
}
