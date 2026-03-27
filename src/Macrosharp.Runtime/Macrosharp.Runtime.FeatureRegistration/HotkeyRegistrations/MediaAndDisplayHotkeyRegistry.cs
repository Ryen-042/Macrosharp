using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Win32.Abstractions.SystemControl;

namespace Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;

public static class MediaAndDisplayHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext, Func<bool> canExecuteWhenNotPaused, Action<MpcCommandId> sendMpcCommand, int mediaSeekThrottleMs, int volumeThrottleMs, int brightnessThrottleMs, int zoomThrottleMs)
    {
        // Backtick+W/S/Space for MPC-HC media controls.
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_W,
            Modifiers.BACKTICK,
            () => sendMpcCommand(MpcCommandId.SeekForward),
            canExecuteWhenNotPaused,
            description: "Seek media forward in MPC-HC.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: mediaSeekThrottleMs
        );
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_S,
            Modifiers.BACKTICK,
            () => sendMpcCommand(MpcCommandId.SeekBackward),
            canExecuteWhenNotPaused,
            description: "Seek media backward in MPC-HC.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: mediaSeekThrottleMs
        );
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.SPACE,
            Modifiers.BACKTICK,
            () => sendMpcCommand(MpcCommandId.TogglePlayPause),
            canExecuteWhenNotPaused,
            description: "Toggle MPC-HC play or pause.",
            sourceContext: sourceContext
        );

        // Ctrl+Shift +/- for system volume.
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.OEM_PLUS,
            Modifiers.CTRL_SHIFT,
            () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_UP),
            canExecuteWhenNotPaused,
            description: "Increase system volume.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: volumeThrottleMs
        );
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.ADD,
            Modifiers.CTRL_SHIFT,
            () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_UP),
            canExecuteWhenNotPaused,
            description: "Increase system volume.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: volumeThrottleMs
        );
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.OEM_MINUS,
            Modifiers.CTRL_SHIFT,
            () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_DOWN),
            canExecuteWhenNotPaused,
            description: "Decrease system volume.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: volumeThrottleMs
        );
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.SUBTRACT,
            Modifiers.CTRL_SHIFT,
            () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_DOWN),
            canExecuteWhenNotPaused,
            description: "Decrease system volume.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: volumeThrottleMs
        );

        // Backtick+F2/F3 for brightness.
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.F2,
            Modifiers.BACKTICK,
            () =>
            {
                int level = BrightnessControl.DecreaseBrightness();
                if (level >= 0)
                    Console.WriteLine($"Brightness: {level}%");
            },
            canExecuteWhenNotPaused,
            description: "Decrease screen brightness.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: brightnessThrottleMs
        );

        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.F3,
            Modifiers.BACKTICK,
            () =>
            {
                int level = BrightnessControl.IncreaseBrightness();
                if (level >= 0)
                    Console.WriteLine($"Brightness: {level}%");
            },
            canExecuteWhenNotPaused,
            description: "Increase screen brightness.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: brightnessThrottleMs
        );

        // Ctrl+E/Q while Scroll Lock is on for zoom.
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_E,
            Modifiers.CTRL,
            () => MouseSimulator.SendMouseScroll(steps: 3, direction: 1),
            () => canExecuteWhenNotPaused() && Modifiers.IsScrollLockOn,
            description: "Zoom in while Scroll Lock is on.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: zoomThrottleMs
        );

        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_Q,
            Modifiers.CTRL,
            () => MouseSimulator.SendMouseScroll(steps: -3, direction: 1),
            () => canExecuteWhenNotPaused() && Modifiers.IsScrollLockOn,
            description: "Zoom out while Scroll Lock is on.",
            sourceContext: sourceContext,
            dispatchPolicy: HotkeyDispatchPolicy.Throttled,
            throttleIntervalMs: zoomThrottleMs
        );
    }
}

