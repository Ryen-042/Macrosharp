using Macrosharp.Infrastructure;
using Macrosharp.Runtime.Configuration;
using Macrosharp.UserInterfaces.Reminders;
using Macrosharp.UserInterfaces.ToastNotifications;
using Macrosharp.UserInterfaces.TrayIcon;

namespace Macrosharp.Runtime.Core;

public static class ProgramTrayMenuFactory
{
    public sealed class Dependencies
    {
        public required IconCycler IconCycler { get; init; }
        public required IReadOnlyList<string> IconPaths { get; init; }
        public required ToastNotificationHost ToastHost { get; init; }
        public required MainConfigurationManager MainConfigurationManager { get; init; }
        public required ReminderConfigurationManager ReminderConfigurationManager { get; init; }
        public required ReminderCrudService ReminderCrudService { get; init; }
        public required string TextExpansionConfigPath { get; init; }
        public required string ReminderConfigPath { get; init; }
        public required Func<bool> GetNotificationsHidden { get; init; }
        public required Action<bool> SetNotificationsHidden { get; init; }
        public required Func<bool> GetReminderSoundsMuted { get; init; }
        public required Action<bool> SetReminderSoundsMuted { get; init; }
        public required Func<bool> GetTerminalMessagesEnabled { get; init; }
        public required Action<bool> SetTerminalMessagesEnabled { get; init; }
        public required Func<bool> IsBurstClickActive { get; init; }
        public required Action StartBurstClick { get; init; }
        public required Action<string> StopBurstClick { get; init; }
        public required Action ShowHotkeysWindow { get; init; }
        public required Action ShowTextExpansionsWindow { get; init; }
        public required Func<ToastNotificationContent> CreateRunningToast { get; init; }
        public required Func<TrayIconHost?> GetTrayHost { get; init; }
    }

    public static List<TrayMenuItem> Build(Dependencies dependencies)
    {
        void OpenInShell(string path, string label)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                Console.WriteLine($"Opened {label}: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open {label}: {ex.Message}");
            }
        }

        void OpenRunningFolder() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });

        void OpenProjectFolder()
        {
            if (!string.IsNullOrWhiteSpace(PathLocator.RootPath) && Directory.Exists(PathLocator.RootPath))
            {
                OpenInShell(PathLocator.RootPath, "project folder");
                return;
            }

            Console.WriteLine("Project root not detected; opening running folder instead.");
            OpenRunningFolder();
        }

        void OpenMainConfig()
        {
            dependencies.MainConfigurationManager.LoadOrCreate();
            OpenInShell(dependencies.MainConfigurationManager.ConfigPath, "main config");
        }

        void OpenTextExpansionConfig() => OpenInShell(dependencies.TextExpansionConfigPath, "text expansion config");

        void OpenRemindersConfig() => OpenInShell(dependencies.ReminderConfigPath, "reminders config");

        void SwitchIcon()
        {
            string? next = dependencies.IconCycler.GetNext();
            if (!string.IsNullOrWhiteSpace(next))
            {
                dependencies.GetTrayHost()?.UpdateIcon(next);
            }
        }

        void ReloadHotkeys() => Console.WriteLine("Tray action: reload hotkeys.");

        void ReloadConfigs() => Console.WriteLine("Tray action: reload configs.");

        void ReloadReminders()
        {
            dependencies.ReminderConfigurationManager.ReloadNow();
            Console.WriteLine("Tray action: reminders config reloaded.");
        }

        void AddReminder() => dependencies.ReminderCrudService.AddReminderInteractively();

        void EditReminder() => dependencies.ReminderCrudService.EditReminderInteractively();

        void DeleteReminder() => dependencies.ReminderCrudService.DeleteReminderInteractively();

        void ClearConsoleLogs()
        {
            Console.Clear();
            Console.WriteLine("Console cleared by tray action.");
        }

        return
        [
            TrayMenuItem.ActionItem("Open Running Folder", OpenRunningFolder, iconPath: dependencies.IconCycler.GetNext()),
            TrayMenuItem.Submenu(
                "Show Notification",
                [
                    TrayMenuItem.ActionItem("Simple", () => dependencies.ToastHost.Show("Macrosharp", "A simple text notification.")),
                    TrayMenuItem.ActionItem(
                        "Long Duration",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "This toast stays visible for ~25 seconds.",
                                    Duration = ToastDuration.Long,
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "With Attribution",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "A notification with attribution text.",
                                    Attribution = "via Macrosharp",
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "Alarm Scenario",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Alarm",
                                    Body = "This is an alarm-style notification.",
                                    Scenario = ToastScenario.Alarm,
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "Reminder Scenario",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Reminder",
                                    Body = "Don't forget your task!",
                                    Scenario = ToastScenario.Reminder,
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "With App Logo",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "Notification with a custom app logo.",
                                    AppLogoPath = dependencies.IconPaths.FirstOrDefault(),
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "Progress (Indeterminate)",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "Working on it...",
                                    ProgressBar = new ToastProgressBar { Title = "Processing", Status = "Please wait..." },
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "Progress (50%)",
                        () =>
                            dependencies.ToastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "Half way there!",
                                    ProgressBar = new ToastProgressBar
                                    {
                                        Title = "Downloading",
                                        Value = 0.5,
                                        ValueStringOverride = "5 / 10 files",
                                        Status = "In progress",
                                    },
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem("With Action Buttons", () => dependencies.ToastHost.Show(dependencies.CreateRunningToast())),
                ],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Notifications & Sounds",
                [
                    TrayMenuItem.ActionItem(
                        () => dependencies.GetNotificationsHidden() ? "Show Notifications" : "Hide Notifications",
                        () =>
                        {
                            bool next = !dependencies.GetNotificationsHidden();
                            dependencies.SetNotificationsHidden(next);
                            Console.WriteLine($"Notifications: {(next ? "Hidden" : "Visible")}");
                            if (!next)
                            {
                                AudioPlayer.PlayOnAsync();
                            }
                            else
                            {
                                AudioPlayer.PlayOffAsync();
                            }
                        },
                        iconPath: dependencies.IconCycler.GetNext()
                    ),
                    TrayMenuItem.ActionItem(
                        () => dependencies.GetReminderSoundsMuted() ? "Unmute Reminder Sounds" : "Mute Reminder Sounds",
                        () =>
                        {
                            bool next = !dependencies.GetReminderSoundsMuted();
                            dependencies.SetReminderSoundsMuted(next);
                            Console.WriteLine($"Reminder sounds: {(next ? "Muted" : "Unmuted")}");
                            if (!next)
                            {
                                AudioPlayer.PlayOnAsync();
                            }
                        },
                        iconPath: dependencies.IconCycler.GetNext()
                    ),
                    TrayMenuItem.ActionItem(
                        () => dependencies.GetTerminalMessagesEnabled() ? "Hide Terminal Keystrokes" : "Show Terminal Keystrokes",
                        () =>
                        {
                            bool next = !dependencies.GetTerminalMessagesEnabled();
                            dependencies.SetTerminalMessagesEnabled(next);
                            Console.WriteLine($"Terminal keystrokes: {(next ? "Shown" : "Hidden")}");
                            if (next)
                            {
                                AudioPlayer.PlayOnAsync();
                            }
                            else
                            {
                                AudioPlayer.PlayOffAsync();
                            }
                        },
                        iconPath: dependencies.IconCycler.GetNext()
                    ),
                ],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Burst Click",
                [
                    TrayMenuItem.ActionItem(() => dependencies.IsBurstClickActive() ? "Start Burst Click (active)" : "Start Burst Click", dependencies.StartBurstClick, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem(() => dependencies.IsBurstClickActive() ? "Stop Burst Click" : "Stop Burst Click (inactive)", () => dependencies.StopBurstClick("tray menu"), iconPath: dependencies.IconCycler.GetNext()),
                ],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.ActionItem("Switch Icon", SwitchIcon, iconPath: dependencies.IconCycler.GetNext()),
            TrayMenuItem.ActionItem("Show Hotkeys", dependencies.ShowHotkeysWindow, iconPath: dependencies.IconCycler.GetNext()),
            TrayMenuItem.ActionItem("Show Text Expansions", dependencies.ShowTextExpansionsWindow, iconPath: dependencies.IconCycler.GetNext()),
            TrayMenuItem.Submenu(
                "Reload",
                [TrayMenuItem.ActionItem("Hotkeys", ReloadHotkeys, iconPath: dependencies.IconCycler.GetNext()), TrayMenuItem.ActionItem("Configs", ReloadConfigs, iconPath: dependencies.IconCycler.GetNext())],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Reminders",
                [
                    TrayMenuItem.ActionItem("Reload reminders config", ReloadReminders, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Add reminder", AddReminder, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Edit reminder", EditReminder, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Delete reminder", DeleteReminder, iconPath: dependencies.IconCycler.GetNext()),
                ],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Configuration",
                [
                    TrayMenuItem.ActionItem("Open Main Config", OpenMainConfig, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Text Expansion Config", OpenTextExpansionConfig, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Reminders Config", OpenRemindersConfig, iconPath: dependencies.IconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Project Folder", OpenProjectFolder, iconPath: dependencies.IconCycler.GetNext()),
                ],
                iconPath: dependencies.IconCycler.GetNext()
            ),
            TrayMenuItem.ActionItem("Clear Console Logs", ClearConsoleLogs, iconPath: dependencies.IconCycler.GetNext()),
        ];
    }
}
