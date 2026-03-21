using Macrosharp.Devices.Keyboard;
using Macrosharp.UserInterfaces.DynamicWindow;

namespace Macrosharp.Runtime.Core;

public static class RuntimeHotkeyReferenceWindow
{
    public static void Show(HotkeyManager? hotkeyManager)
    {
        if (hotkeyManager is null)
        {
            Console.WriteLine("Hotkeys are not initialized yet.");
            return;
        }

        var rows = hotkeyManager
            .GetRegisteredHotkeysSnapshot()
            .OrderBy(h => string.IsNullOrWhiteSpace(h.SourceContext) ? "No source" : h.SourceContext, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Hotkey.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(h => (IReadOnlyList<string>)new List<string> { h.Hotkey.ToString(), string.IsNullOrWhiteSpace(h.Description) ? "No description" : h.Description, string.IsNullOrWhiteSpace(h.SourceContext) ? "No source" : h.SourceContext })
            .ToList();

        FilterableTableWindow.ShowOrActivate($"Macrosharp Hotkeys ({rows.Count})", ["Hotkey", "Description", "Source"], rows, filterPlaceholder: "Type to filter hotkeys...");
    }
}


