using Macrosharp.Runtime.Configuration;

namespace Macrosharp.Runtime.Core;

public sealed class ProgramRuntimeState
{
    public ProgramRuntimeState(MainConfiguration configuration)
    {
        Apply(configuration);
    }

    public MainConfiguration CurrentMainConfig { get; private set; } = new();

    public bool NotificationsHidden { get; set; }

    public bool ReminderSoundsMuted { get; set; }

    public bool TerminalMessagesEnabled { get; set; }

    public bool WatchMainConfig { get; private set; }

    public bool WatchRemindersConfig { get; private set; }

    public bool WatchTextExpansionsConfig { get; private set; }

    public void Apply(MainConfiguration configuration)
    {
        CurrentMainConfig = configuration;
        NotificationsHidden = configuration.Tray.NotificationsHidden;
        ReminderSoundsMuted = configuration.Tray.ReminderSoundsMuted;
        TerminalMessagesEnabled = configuration.Diagnostics.TerminalMessagesEnabled;
        WatchMainConfig = configuration.FileWatching.MainConfig;
        WatchRemindersConfig = configuration.FileWatching.RemindersConfig;
        WatchTextExpansionsConfig = configuration.FileWatching.TextExpansionsConfig;
    }

    public void PersistRuntimeSettingsToCurrentConfig()
    {
        CurrentMainConfig.Tray.NotificationsHidden = NotificationsHidden;
        CurrentMainConfig.Tray.ReminderSoundsMuted = ReminderSoundsMuted;
        CurrentMainConfig.Diagnostics.TerminalMessagesEnabled = TerminalMessagesEnabled;
    }
}
