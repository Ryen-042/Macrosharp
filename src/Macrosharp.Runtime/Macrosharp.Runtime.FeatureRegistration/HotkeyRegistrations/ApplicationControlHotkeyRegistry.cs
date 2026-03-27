using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;

namespace Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;

public static class ApplicationControlHotkeyRegistry
{
    public static void Register(
        HotkeyManager hotkeyManager,
        string sourceContext,
        Func<bool> canExecuteWhenNotPaused,
        Action onConfirmExit,
        Action onImmediateExit,
        Action onShowHotkeys,
        Action onShowTextExpansions,
        Action onShowRunningToast,
        Action onClearConsole,
        Action onToggleConsoleVisibility,
        Action onTogglePauseResume,
        Action onToggleBurstClick,
        Action onToggleTextExpansion
    )
    {
        // Pause-mode allowlist documentation:
        // 1) Add new shortcuts that must remain active while paused by passing allowWhenPaused: true.
        // 2) Add shortcuts that should be blocked while paused by omitting allowWhenPaused (default false).
        // 3) Blocked shortcuts are registered as conditional hotkeys using canExecuteWhenNotPaused,
        //    so they pass through to the focused application instead of being suppressed.
        void RegisterApplicationControlHotkey(VirtualKey key, int modifiers, Action action, string description, bool allowWhenPaused = false)
        {
            if (allowWhenPaused)
            {
                hotkeyManager.RegisterHotkey(key, modifiers, action, description: description, sourceContext: sourceContext);
                return;
            }

            hotkeyManager.RegisterConditionalHotkey(key, modifiers, action, canExecuteWhenNotPaused, description: description, sourceContext: sourceContext);
        }

        // Always active while paused so users can recover/inspect/exit.
        // Add future pause-mode allowlist entries in this section.
        RegisterApplicationControlHotkey(VirtualKey.ESCAPE, Modifiers.WIN, onConfirmExit, "Prompt to terminate Macrosharp.", allowWhenPaused: true);

        RegisterApplicationControlHotkey(VirtualKey.ESCAPE, Modifiers.ALT_WIN, onImmediateExit, "Terminate Macrosharp immediately.", allowWhenPaused: true);

        RegisterApplicationControlHotkey(VirtualKey.OEM_2, Modifiers.CTRL_WIN, onShowHotkeys, "Open the hotkeys reference window.", allowWhenPaused: true);

        RegisterApplicationControlHotkey(VirtualKey.OEM_2, Modifiers.CTRL_ALT_WIN, onShowTextExpansions, "Open the text expansions reference window.", allowWhenPaused: true);

        RegisterApplicationControlHotkey(VirtualKey.KEY_P, Modifiers.CTRL_ALT_WIN, onTogglePauseResume, "Pause or resume keyboard and mouse automation.", allowWhenPaused: true);

        RegisterApplicationControlHotkey(VirtualKey.OEM_2, Modifiers.SHIFT_WIN, onShowRunningToast, "Show the running status toast with quick actions.");

        RegisterApplicationControlHotkey(VirtualKey.DELETE, Modifiers.SHIFT_WIN, onClearConsole, "Clear console output.");

        RegisterApplicationControlHotkey(VirtualKey.INSERT, Modifiers.SHIFT_WIN, onToggleConsoleVisibility, "Toggle console window visibility.");

        RegisterApplicationControlHotkey(VirtualKey.KEY_B, Modifiers.CTRL_ALT_WIN, onToggleBurstClick, "Toggle burst click (start when inactive, stop when active).");

        RegisterApplicationControlHotkey(VirtualKey.KEY_T, Modifiers.CTRL_ALT, onToggleTextExpansion, "Toggle text expansion on or off.");
    }
}

