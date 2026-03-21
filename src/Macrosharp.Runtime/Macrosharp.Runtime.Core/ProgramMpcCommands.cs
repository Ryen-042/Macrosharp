using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Runtime.Core;

public static class ProgramMpcCommands
{
    public static void Send(int commandId)
    {
        var handles = WindowFinder.GetHwndByClassName("MediaPlayerClassicW");
        if (handles.Count > 0)
        {
            Messaging.PostMessageToWindow(handles[0], PInvoke.WM_COMMAND, (WPARAM)(nuint)commandId, default);
        }
    }
}


