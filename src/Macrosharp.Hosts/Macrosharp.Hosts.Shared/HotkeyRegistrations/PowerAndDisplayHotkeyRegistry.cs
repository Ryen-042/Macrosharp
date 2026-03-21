using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Infrastructure;
using Macrosharp.Win32.Abstractions.SystemControl;

namespace Macrosharp.Hosts.Shared.HotkeyRegistrations;

public static class PowerAndDisplayHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext, Func<bool> confirmShutdown, Action<string, string, string, Exception?> warn)
    {
        // Win+CapsLock toggles Scroll Lock state.
        hotkeyManager.RegisterHotkey(
            VirtualKey.CAPITAL,
            Modifiers.WIN,
            () =>
            {
                KeyboardSimulator.SimulateKeyPress(VirtualKey.SCROLL);
                bool scrollOn = Modifiers.IsScrollLockOn;
                Console.WriteLine($"Scroll Lock toggled -> {(scrollOn ? "ON" : "OFF")}");
                if (scrollOn)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            },
            description: "Toggle Scroll Lock.",
            sourceContext: sourceContext
        );

        // Ctrl+Alt+Win+S puts the system to sleep.
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_S,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                Console.WriteLine("Entering sleep mode...");
                try
                {
                    AudioPlayer.PlayAudio(@"C:\Windows\Media\Windows Logoff Sound.wav");
                }
                catch (Exception ex)
                {
                    warn("Program", "PlaySleepSound", "Failed to play sleep sound", ex);
                }

                SystemActions.Sleep();
            },
            description: "Put the system to sleep.",
            sourceContext: sourceContext
        );

        // Ctrl+Alt+Win+Q requests shutdown.
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_Q,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                if (confirmShutdown())
                {
                    Console.WriteLine("Shutting down...");
                    SystemActions.Shutdown();
                }
            },
            description: "Shut down the system with confirmation.",
            sourceContext: sourceContext
        );

        // Ctrl+Alt+Win+Num1-4 switch display mode.
        hotkeyManager.RegisterHotkey(VirtualKey.NUMPAD1, Modifiers.CTRL_ALT_WIN, () => SwitchDisplayWithAudio(1), description: "Switch display mode to internal screen.", sourceContext: sourceContext);
        hotkeyManager.RegisterHotkey(VirtualKey.NUMPAD2, Modifiers.CTRL_ALT_WIN, () => SwitchDisplayWithAudio(2), description: "Switch display mode to external screen.", sourceContext: sourceContext);
        hotkeyManager.RegisterHotkey(VirtualKey.NUMPAD3, Modifiers.CTRL_ALT_WIN, () => SwitchDisplayWithAudio(3), description: "Switch display mode to extend.", sourceContext: sourceContext);
        hotkeyManager.RegisterHotkey(VirtualKey.NUMPAD4, Modifiers.CTRL_ALT_WIN, () => SwitchDisplayWithAudio(4), description: "Switch display mode to clone.", sourceContext: sourceContext);
    }

    private static void SwitchDisplayWithAudio(int mode)
    {
        SystemActions.SwitchDisplay(mode);
        AudioPlayer.PlayBonkAsync();
    }
}
