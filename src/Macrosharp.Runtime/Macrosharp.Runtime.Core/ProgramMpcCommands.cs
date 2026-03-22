using Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;
using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Runtime.Core;

public static class ProgramMpcCommands
{
    /// <summary>
    /// Sends a command message to the MPC-HC window if it is open.
    /// </summary>
    /// <param name="commandId">
    /// The command ID to send to MPC-HC. Command IDs correspond to MPC-HC's internal command definitions, which
    /// you can find in the Options dialog of MPC-HC under "Player" → "Keys".
    /// Common commands include:
    /// <list type="bullet">
    /// <item><description>902: Seek forward</description></item>
    /// <item><description>901: Seek backward</description></item>
    /// <item><description>889: Toggle play/pause</description></item>
    /// </list>
    /// </param>
    /// <remarks>
    /// This method looks for a window with the class name "MediaPlayerClassicW" and sends the specified command message to it.
    /// If multiple MPC-HC windows are open, it will target the first one found.
    /// </remarks>
    public static void Send(MpcCommandId commandId)
    {
        var handles = WindowFinder.GetHwndByClassName("MediaPlayerClassicW");
        if (handles.Count > 0)
        {
            Messaging.PostMessageToWindow(handles[0], PInvoke.WM_COMMAND, (WPARAM)(nuint)(int)commandId, default);
        }
    }
}
