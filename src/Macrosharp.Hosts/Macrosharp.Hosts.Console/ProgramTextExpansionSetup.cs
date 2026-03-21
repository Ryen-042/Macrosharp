using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.Infrastructure;

namespace Macrosharp.Hosts.ConsoleHost;

internal static class ProgramTextExpansionSetup
{
    public static TextExpansionConfiguration Configure(TextExpansionConfigurationManager configManager, TextExpansionManager textExpansionManager)
    {
        var loadedConfig = configManager.LoadConfiguration();
        textExpansionManager.LoadConfiguration(loadedConfig);

        configManager.ConfigurationChanged += (_, newConfig) =>
        {
            textExpansionManager.LoadConfiguration(newConfig);
            Console.WriteLine("Text expansion configuration reloaded.");
        };

        textExpansionManager.ExpansionOccurred += (_, e) =>
        {
            Console.WriteLine($"Expanded '{e.Rule.Trigger}' → '{(e.ExpandedText.Length > 50 ? e.ExpandedText[..50] + "..." : e.ExpandedText)}'");
            AudioPlayer.PlayKnobAsync();
        };

        textExpansionManager.ExpansionError += (_, e) =>
        {
            Console.WriteLine($"Expansion error for '{e.Rule.Trigger}': {e.Exception.Message}");
            AudioPlayer.PlayFailure();
        };

        return loadedConfig;
    }

    public static void PrintLoadedRules(TextExpansionConfiguration config)
    {
        Console.WriteLine("\nLoaded expansion rules:");
        foreach (var rule in config.Rules.Where(r => r.Enabled))
        {
            Console.WriteLine($"  {rule}");
        }

        Console.WriteLine();
    }
}
