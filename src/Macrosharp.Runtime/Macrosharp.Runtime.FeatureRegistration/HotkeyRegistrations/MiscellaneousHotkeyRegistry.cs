using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;

namespace Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;

public static class MiscellaneousHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext, Func<bool> canExecuteWhenNotPaused, Action onOpenImageEditor)
    {
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.OEM_5,
            Modifiers.BACKTICK,
            onOpenImageEditor,
            canExecuteWhenNotPaused,
            description: "Open the image editor from clipboard content.",
            sourceContext: sourceContext
        );
    }
}

