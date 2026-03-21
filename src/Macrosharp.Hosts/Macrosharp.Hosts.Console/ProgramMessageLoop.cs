using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Hosts.ConsoleHost;

internal static class ProgramMessageLoop
{
    public static void Run()
    {
        MSG msg;
        while (PInvoke.GetMessage(out msg, new HWND(), 0, 0).Value != 0)
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
    }
}
