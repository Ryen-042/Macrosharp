using System.ComponentModel;
using System.Runtime.InteropServices;
using Macrosharp.Win32.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Abstractions.WindowTools;

public class WindowBuilder
{
    // Define a managed delegate for the window procedure
    public delegate LRESULT WndProcDelegate(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam);

    // Define the subclass procedure delegate
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate LRESULT SubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData);

    // Update the delegate type to match the expected type
    private static SUBCLASSPROC _subclassProc = SubclassProcHandler;

    public static IntPtr MAKEINTRESOURCEW(int id) => (IntPtr)id;

    public static ushort LOWORD(nint l) => (ushort)(l & 0xFFFF);

    public static ushort HIWORD(nint l) => (ushort)((l >> 16) & 0xFFFF);

    public static LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        switch (uMsg)
        {
            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                return (LRESULT)0;
            default:
                return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }
    }

    public static unsafe void RegisterWindowClass(string className, WNDPROC wndProc)
    {
        HINSTANCE hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
        if (hInstance == HINSTANCE.Null)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        fixed (char* classNamePtr = className)
        {
            var wc = new WNDCLASSW
            {
                style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                lpfnWndProc = wndProc,
                hInstance = hInstance,
                lpszClassName = classNamePtr,
            };

            ushort atom = PInvoke.RegisterClass(wc);
            if (atom == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static void CreateWindow()
    {
        string className = "MyDynamicWindowClass";
        WindowBuilder.RegisterWindowClass(className, WindowBuilder.WndProc);
        HWND hWnd = CreateDynamicWindow(className, "Dynamic Window");
        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);
        RunMessageLoop();
    }

    public static unsafe HWND CreateDynamicWindow(string className, string windowTitle)
    {
        HINSTANCE hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
        if (hInstance == HINSTANCE.Null)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        HWND hWnd = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_OVERLAPPEDWINDOW, className, windowTitle, WINDOW_STYLE.WS_OVERLAPPEDWINDOW, PInvoke.CW_USEDEFAULT, PInvoke.CW_USEDEFAULT, 800, 600, HWND.Null, HMENU.Null, hInstance, null);

        if (hWnd == HWND.Null)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return hWnd;
    }

    public static void RunMessageLoop()
    {
        while (PInvoke.GetMessage(out MSG msg, HWND.Null, 0, 0))
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
    }

    public static unsafe void AddListViewColumn(HWND hListView, string text, int width)
    {
        fixed (char* textPtr = text)
        {
            var lvColumn = new LVCOLUMNW
            {
                mask = LVCOLUMNW_MASK.LVCF_TEXT | LVCOLUMNW_MASK.LVCF_WIDTH,
                pszText = textPtr,
                cx = width,
            };

            int columnIndex = (int)
                PInvoke.SendMessage(
                    hListView,
                    PInvoke.LVM_INSERTCOLUMNW,
                    (WPARAM)0,
                    (LPARAM)(nint)(&lvColumn) // Proper pointer casting
                );

            if (columnIndex == -1)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    // Update the SubclassProcHandler method signature to match the expected delegate type
    public static LRESULT SubclassProcHandler(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == PInvoke.WM_KEYDOWN)
        {
            if (wParam == (WPARAM)(nuint)VirtualKey.TAB)
            {
                bool shiftPressed = (PInvoke.GetAsyncKeyState((int)VirtualKey.SHIFT) & 0x8000) != 0;
                HWND nextControl = PInvoke.GetNextDlgTabItem((HWND)dwRefData, hWnd, shiftPressed);
                PInvoke.SetFocus(nextControl);
                return (LRESULT)0;
            }
            else if (wParam == (WPARAM)(nuint)VirtualKey.RETURN)
            {
                HWND parentHwnd = (HWND)dwRefData;
                HWND buttonHwnd = PInvoke.GetDlgItem(parentHwnd, 1); // OK button ID=1
                PInvoke.SendMessage(buttonHwnd, PInvoke.BM_CLICK, default, default);
                return (LRESULT)0;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public static unsafe void SubclassControl(HWND hControl, HWND hParent)
    {
        bool success = PInvoke.SetWindowSubclass(hControl, _subclassProc, uIdSubclass: 0, dwRefData: hParent);

        if (!success)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public static unsafe void CreateListView(HWND parentHwnd)
    {
        // Create list-view control
        HWND hListView = PInvoke.CreateWindowEx(
            WINDOW_EX_STYLE.WS_EX_CLIENTEDGE,
            PInvoke.WC_LISTVIEW,
            "",
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (WINDOW_STYLE)PInvoke.LVS_REPORT, // Operator '|' cannot be applied to operands of type 'WINDOW_STYLE' and 'uint'
            10,
            10,
            300,
            200,
            parentHwnd,
            HMENU.Null, // Argument 10: cannot convert from 'Windows.Win32.UI.WindowsAndMessaging.HMENU' to 'System.Runtime.InteropServices.SafeHandle'
            PInvoke.GetModuleHandle((string)null),
            null
        );

        AddListViewColumn(hListView, "Name", 100);
        AddListViewColumn(hListView, "Value", 150);
        SubclassControl(hListView, parentHwnd);
    }
}
