using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.DynamicWindow
{
    public class WindowSample
    {
        private static WNDPROC _wndProcDelegate = null!;

        private static unsafe LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_DESTROY:
                    PInvoke.PostQuitMessage(0);
                    return (LRESULT)0;
                default:
                    return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        public static unsafe void Main()
        {
            _wndProcDelegate = WndProc;

            fixed (char* lpszClassName = "MyWindowClass")
            {
                // Register Window Class
                var wc = new WNDCLASSW
                {
                    //cbSize = (uint)Marshal.SizeOf<WNDCLASSW>(),
                    style = 0,
                    lpfnWndProc = _wndProcDelegate,
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    hCursor = PInvoke.LoadCursor(default, PInvoke.IDC_ARROW),
                    lpszClassName = new PCWSTR(lpszClassName),
                };

                PInvoke.RegisterClass(wc);
            }

            // Create the Window
            var hWnd = PInvoke.CreateWindowEx(0, "MyWindowClass", "CsWin32 Window", WINDOW_STYLE.WS_OVERLAPPEDWINDOW, PInvoke.CW_USEDEFAULT, PInvoke.CW_USEDEFAULT, 800, 600, default, default, PInvoke.GetModuleHandle((string?)null), null);

            // Show and Update the Window
            PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);
            PInvoke.UpdateWindow(hWnd);

            // Message Loop
            while (PInvoke.GetMessage(out var msg, default, 0, 0))
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }
}
