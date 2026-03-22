using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.UserInterfaces.DynamicWindow;

namespace Macrosharp.Runtime.Core;

public static class RuntimeTextExpansionReferenceWindow
{
    public static void Show(TextExpansionConfigurationManager? configurationManager)
    {
        if (configurationManager is null)
        {
            Console.WriteLine("Text expansions are not initialized yet.");
            return;
        }

        var rows = configurationManager
            .CurrentConfiguration
            .Rules
            .OrderBy(r => r.Trigger, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Mode)
            .ThenByDescending(r => r.Enabled)
            .Select(
                r =>
                    (IReadOnlyList<string>)
                        new List<string>
                        {
                            r.Trigger,
                            EscapeControlCharacters(r.Expansion),
                            r.Mode.ToString(),
                            r.CaseSensitive.ToString(),
                            r.Enabled.ToString(),
                        }
            )
            .ToList();

        FilterableTableWindow.ShowOrActivate(
            $"Macrosharp Text Expansions ({rows.Count})",
            ["Trigger", "Expansion", "Mode", "CaseSensitive", "Enabled"],
            rows,
            filterPlaceholder: "Type to filter text expansions..."
        );
    }

    private static string EscapeControlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}