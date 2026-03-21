using Macrosharp.UserInterfaces.TrayIcon;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Hosts.ConsoleHost;

internal sealed class ProgramExitCoordinator
{
    private readonly uint _mainThreadId;
    private readonly BurstClickCoordinator _burstClickCoordinator;
    private readonly ProgramRuntimeState _runtimeState;
    private readonly Func<TrayIconHost?> _trayHostAccessor;
    private int _exitRequested;

    public ProgramExitCoordinator(uint mainThreadId, BurstClickCoordinator burstClickCoordinator, ProgramRuntimeState runtimeState, Func<TrayIconHost?> trayHostAccessor)
    {
        _mainThreadId = mainThreadId;
        _burstClickCoordinator = burstClickCoordinator;
        _runtimeState = runtimeState;
        _trayHostAccessor = trayHostAccessor;
    }

    public ConsoleCancelEventHandler CreateConsoleCancelHandler()
    {
        return (_, e) =>
        {
            e.Cancel = true;

            if (Volatile.Read(ref _exitRequested) == 1)
            {
                return;
            }

            string shortcut = e.SpecialKey == ConsoleSpecialKey.ControlBreak ? "Ctrl+Break" : "Ctrl+C";
            var result = PInvoke.MessageBox(HWND.Null, $"{shortcut} detected.\n\nDo you want to quit Macrosharp?", "Macrosharp - Confirm Exit", MESSAGEBOX_STYLE.MB_ICONQUESTION | MESSAGEBOX_STYLE.MB_YESNO | MESSAGEBOX_STYLE.MB_TOPMOST);

            if (result == MESSAGEBOX_RESULT.IDYES)
            {
                Console.WriteLine($"{shortcut}: exit confirmed.");
                RequestExit(shortcut);
                return;
            }

            Console.WriteLine($"{shortcut}: exit canceled.");
        };
    }

    public void RequestExit(string source)
    {
        if (Interlocked.Exchange(ref _exitRequested, 1) == 1)
        {
            return;
        }

        _burstClickCoordinator.Stop("application exit", notifyWhenInactive: false);
        _runtimeState.PersistRuntimeSettingsToCurrentConfig();

        Console.WriteLine($"Exit requested from {source}.");
        _trayHostAccessor()?.Dispose();
        PInvoke.PostThreadMessage(_mainThreadId, PInvoke.WM_QUIT, 0, 0);
    }
}
