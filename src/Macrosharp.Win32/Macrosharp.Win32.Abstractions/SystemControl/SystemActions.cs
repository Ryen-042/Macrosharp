using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Win32.Abstractions.SystemControl;

/// <summary>
/// Provides system-level actions: sleep, shutdown, display switching, and console visibility.
/// </summary>
public static class SystemActions
{
    // PowrProf.dll is not in CsWin32; use manual P/Invoke.
    [DllImport("PowrProf.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.U1)] bool hibernate,
        [MarshalAs(UnmanagedType.U1)] bool forceCritical,
        [MarshalAs(UnmanagedType.U1)] bool disableWakeEvent);

    /// <summary>Puts the system into sleep (suspend-to-RAM) mode.</summary>
    public static void Sleep()
    {
        SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);
    }

    /// <summary>
    /// Initiates a system shutdown. Uses <c>shutdown.exe /s /t 0</c> for reliability.
    /// </summary>
    public static void Shutdown()
    {
        Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0") { UseShellExecute = false, CreateNoWindow = true });
    }

    /// <summary>
    /// Switches the display/monitor configuration.
    /// </summary>
    /// <param name="mode">1 = internal only, 2 = external only, 3 = extend, 4 = clone.</param>
    public static void SwitchDisplay(int mode)
    {
        string arg = mode switch
        {
            1 => "/internal",
            2 => "/external",
            3 => "/extend",
            4 => "/clone",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), "Mode must be 1–4.")
        };

        Process.Start(new ProcessStartInfo("displayswitch.exe", arg) { UseShellExecute = false, CreateNoWindow = true });
    }

    #region Console Visibility

    private static bool _consoleVisible = true;

    /// <summary>Toggles the visibility of the console window. Returns the new visibility state.</summary>
    public static bool ToggleConsoleVisibility()
    {
        HWND consoleHwnd = PInvoke.GetConsoleWindow();
        if (consoleHwnd == HWND.Null)
            return _consoleVisible;

        _consoleVisible = !_consoleVisible;
        PInvoke.ShowWindow(consoleHwnd, _consoleVisible
            ? Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOW
            : Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_HIDE);

        return _consoleVisible;
    }

    #endregion
}
