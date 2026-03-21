using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;

namespace Macrosharp.Hosts.Shared.HotkeyRegistrations;

public static class ApplicationControlHotkeyRegistry
{
    public static void Register(
        HotkeyManager hotkeyManager,
        string sourceContext,
        Action onConfirmExit,
        Action onImmediateExit,
        Action onShowHotkeys,
        Action onShowRunningToast,
        Action onClearConsole,
        Action onToggleConsoleVisibility,
        Action onTogglePauseResume,
        Action onToggleBurstClick,
        Action onToggleTextExpansion
    )
    {
        hotkeyManager.RegisterHotkey(
            VirtualKey.ESCAPE,
            Modifiers.WIN,
            onConfirmExit,
            description: "Prompt to terminate Macrosharp.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.ESCAPE,
            Modifiers.ALT_WIN,
            onImmediateExit,
            description: "Terminate Macrosharp immediately.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_2,
            Modifiers.CTRL_WIN,
            onShowHotkeys,
            description: "Open the hotkeys reference window.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_2,
            Modifiers.SHIFT_WIN,
            onShowRunningToast,
            description: "Show the running status toast with quick actions.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.DELETE,
            Modifiers.SHIFT_WIN,
            onClearConsole,
            description: "Clear console output.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.INSERT,
            Modifiers.SHIFT_WIN,
            onToggleConsoleVisibility,
            description: "Toggle console window visibility.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_P,
            Modifiers.CTRL_ALT_WIN,
            onTogglePauseResume,
            description: "Pause or resume keyboard and mouse automation.",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_B,
            Modifiers.CTRL_ALT_WIN,
            onToggleBurstClick,
            description: "Toggle burst click (start when inactive, stop when active).",
            sourceContext: sourceContext
        );

        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_T,
            Modifiers.CTRL_ALT,
            onToggleTextExpansion,
            description: "Toggle text expansion on or off.",
            sourceContext: sourceContext
        );
    }
}
