using Macrosharp.Runtime.Configuration;

namespace Macrosharp.Runtime.Core;

public static class ProgramMainConfigurationSetup
{
    public static void Configure(MainConfigurationManager mainConfigurationManager, ProgramRuntimeState runtimeState)
    {
        if (runtimeState.WatchMainConfig)
        {
            mainConfigurationManager.EnableWatching();
        }

        mainConfigurationManager.ConfigurationChanged += (_, updated) =>
        {
            bool previousWatchMainConfig = runtimeState.WatchMainConfig;
            bool previousWatchRemindersConfig = runtimeState.WatchRemindersConfig;
            bool previousWatchTextExpansionsConfig = runtimeState.WatchTextExpansionsConfig;

            runtimeState.Apply(updated);

            if (runtimeState.WatchMainConfig && !previousWatchMainConfig)
            {
                mainConfigurationManager.EnableWatching();
            }

            if (previousWatchRemindersConfig != runtimeState.WatchRemindersConfig || previousWatchTextExpansionsConfig != runtimeState.WatchTextExpansionsConfig)
            {
                Console.WriteLine("[INFO] [Program] Main config watcher toggles for reminders/text-expansions changed. Restart is required to apply manager watcher changes.");
            }

            Console.WriteLine("[INFO] [Program] Main configuration reloaded.");
        };
    }
}
