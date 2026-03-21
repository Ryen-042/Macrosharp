using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Infrastructure;
using Macrosharp.Win32.Abstractions.SystemControl;
using Macrosharp.Win32.Abstractions.WindowTools;

namespace Macrosharp.Hosts.Shared.HotkeyRegistrations;

public static class WindowManagementHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext)
    {
        // ` + = or ` + Add -> Increase opacity
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_PLUS, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: 25), description: "Increase active window opacity.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.ADD, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: 25), description: "Increase active window opacity.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);

        // ` + - or ` + Subtract -> Decrease opacity
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_MINUS, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: -25), description: "Decrease active window opacity.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.SUBTRACT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: -25), description: "Decrease active window opacity.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);

        // Ctrl + Win + A -> Toggle always-on-top
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_A,
            Modifiers.CTRL_WIN,
            () =>
            {
                int state = WindowModifier.ToggleAlwaysOnTopState(default);
                Console.WriteLine($"Always-on-top: {(state == 1 ? "On" : "Off")}");
                if (state == 1)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            },
            description: "Toggle always-on-top for the active window.",
            sourceContext: sourceContext
        );

        // ` + Arrow Keys -> Move active window (medium: 50px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: -50), description: "Move active window up by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: 50), description: "Move active window down by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: -50), description: "Move active window left by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: 50), description: "Move active window right by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);

        // ` + Shift + Arrow Keys -> Move active window (small: 10px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: -10), description: "Move active window up by 10 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: 10), description: "Move active window down by 10 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: -10), description: "Move active window left by 10 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: 10), description: "Move active window right by 10 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);

        // ` + Alt + Arrow Keys -> Resize active window (50px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaHeight: -50), description: "Decrease active window height by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaHeight: 50), description: "Increase active window height by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaWidth: -50), description: "Decrease active window width by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaWidth: 50), description: "Increase active window width by 50 pixels.", sourceContext: sourceContext, dispatchPolicy: HotkeyDispatchPolicy.Coalesced);

        // Ctrl + Pause -> Suspend active window's process
        hotkeyManager.RegisterHotkey(
            VirtualKey.PAUSE,
            Modifiers.CTRL,
            () =>
            {
                bool ok = ProcessControl.SuspendActiveWindowProcess();
                Console.WriteLine(ok ? "Process suspended." : "Failed to suspend process.");
                if (ok)
                    AudioPlayer.PlayOffAsync();
            },
            description: "Suspend the process of the active window.",
            sourceContext: sourceContext
        );

        // Ctrl + Shift + Pause -> Resume active window's process
        hotkeyManager.RegisterHotkey(
            VirtualKey.PAUSE,
            Modifiers.CTRL_SHIFT,
            () =>
            {
                bool ok = ProcessControl.ResumeActiveWindowProcess();
                Console.WriteLine(ok ? "Process resumed." : "Failed to resume process.");
                if (ok)
                    AudioPlayer.PlayOnAsync();
            },
            description: "Resume the process of the active window.",
            sourceContext: sourceContext
        );
    }
}
