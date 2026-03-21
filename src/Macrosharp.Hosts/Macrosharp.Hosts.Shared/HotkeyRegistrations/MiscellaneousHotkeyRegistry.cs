using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;

namespace Macrosharp.Hosts.Shared.HotkeyRegistrations;

public static class MiscellaneousHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext, Action onOpenImageEditor)
    {
        hotkeyManager.RegisterHotkey(VirtualKey.OEM_5, Modifiers.BACKTICK, onOpenImageEditor, description: "Open the image editor from clipboard content.", sourceContext: sourceContext);
    }
}
