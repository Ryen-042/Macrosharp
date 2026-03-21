using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.Devices.Mouse;
using Macrosharp.Hosts.Shared;
using Macrosharp.Hosts.Shared.HotkeyRegistrations;
using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.DynamicWindow;
using Macrosharp.UserInterfaces.Reminders;
using Macrosharp.UserInterfaces.ToastNotifications;
using Macrosharp.UserInterfaces.TrayIcon;
using Macrosharp.Win32.Abstractions.Explorer;
using Macrosharp.Win32.Abstractions.SystemControl;
using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using static Macrosharp.Devices.Keyboard.KeyboardHookManager;

namespace Macrosharp.Hosts.ConsoleHost;

public class Program
{
    // ─── Global State ──────────────────────────────────────────────────────────
    private static HotkeyManager? _hotkeyManager;
    private static bool _paused; // Ctrl+Alt+Win+P toggle
    private static bool _leftMouseHeld,
        _rightMouseHeld,
        _middleMouseHeld; // Scroll Lock mouse-hold toggles

    static void Main()
    {
        // ═══════════════════════════════════════════════════════════════════════
        //  1. Early Initialization
        // ═══════════════════════════════════════════════════════════════════════

        // Capture the main thread's Win32 ID so hotkey callbacks running on Task.Run thread-pool
        // threads can post WM_QUIT to the correct message loop thread.
        uint mainThreadId = PInvoke.GetCurrentThreadId();

        ToastNotificationHost.RegisterAppUserModelId();

        var iconPaths = PathLocator.GetIconFilesFromAssets();
        var iconCycler = IconCycler.Create(iconPaths);
        string mainConfigPath = PathLocator.GetConfigPath("macrosharp.config.json");
        using var mainConfigManager = new MainConfigurationManager(mainConfigPath);
        var mainConfig = mainConfigManager.LoadOrCreate();

        bool notificationsHidden = mainConfig.Tray.NotificationsHidden;
        bool reminderSoundsMuted = mainConfig.Tray.ReminderSoundsMuted;
        bool terminalMessagesEnabled = mainConfig.Diagnostics.TerminalMessagesEnabled;
        bool watchMainConfig = mainConfig.FileWatching.MainConfig;
        bool watchRemindersConfig = mainConfig.FileWatching.RemindersConfig;
        bool watchTextExpansionsConfig = mainConfig.FileWatching.TextExpansionsConfig;
        var burstClickStateGate = new object();
        bool burstClickActive = false;
        VirtualKey burstClickKey = VirtualKey.KEY_A;
        int burstClickIntervalMs = KeyboardSimulator.DefaultBurstClickIntervalMs;
        int burstClickDurationMs = KeyboardSimulator.DefaultBurstClickDurationMs;
        string? burstClickStopReason = null;
        CancellationTokenSource? burstClickCancellation = null;
        Task? burstClickTask = null;

        TrayIconHost? trayHost = null;
        int exitRequested = 0;

        void Warn(string component, string operation, string details, Exception? ex = null)
        {
            string suffix = ex is null ? string.Empty : $" Error='{ex.Message}'.";
            Console.WriteLine($"[WARN] [{component}] Operation='{operation}' Details='{details}'.{suffix}");
        }

        void ShowOneTimeWarningDialog(string title, string message)
        {
            try
            {
                PInvoke.MessageBox(
                    HWND.Null,
                    message,
                    title,
                    MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_TOPMOST
                );
            }
            catch (Exception ex)
            {
                Warn("Program", "ShowOneTimeWarningDialog", $"Title='{title}'", ex);
                Warn("Program", "ShowOneTimeWarningDialog", message);
            }
        }

        void OnPathLocatorIssue(string message, bool isLongRunningOperation)
        {
            if (isLongRunningOperation)
            {
                ShowOneTimeWarningDialog("Macrosharp - Operation Warning", message);
                return;
            }

            Warn("PathLocator", "NotifyIssue", message);
        }

        if (watchMainConfig)
        {
            mainConfigManager.EnableWatching();
        }

        mainConfigManager.ConfigurationChanged += (_, updated) =>
        {
            bool previousWatchMainConfig = watchMainConfig;
            bool previousWatchRemindersConfig = watchRemindersConfig;
            bool previousWatchTextExpansionsConfig = watchTextExpansionsConfig;

            mainConfig = updated;
            notificationsHidden = updated.Tray.NotificationsHidden;
            reminderSoundsMuted = updated.Tray.ReminderSoundsMuted;
            terminalMessagesEnabled = updated.Diagnostics.TerminalMessagesEnabled;
            watchMainConfig = updated.FileWatching.MainConfig;
            watchRemindersConfig = updated.FileWatching.RemindersConfig;
            watchTextExpansionsConfig = updated.FileWatching.TextExpansionsConfig;

            if (watchMainConfig && !previousWatchMainConfig)
            {
                mainConfigManager.EnableWatching();
            }

            if (previousWatchRemindersConfig != watchRemindersConfig || previousWatchTextExpansionsConfig != watchTextExpansionsConfig)
            {
                Console.WriteLine("[INFO] [Program] Main config watcher toggles for reminders/text-expansions changed. Restart is required to apply manager watcher changes.");
            }

            Console.WriteLine("[INFO] [Program] Main configuration reloaded.");
        };

        bool IsBurstClickActive()
        {
            lock (burstClickStateGate)
            {
                return burstClickActive;
            }
        }

        string SanitizeWindowInput(string? value)
        {
            return (value ?? string.Empty).Replace("\0", string.Empty).Trim();
        }

        bool TryParseBurstInteger(string rawValue, int defaultValue, string fieldName, bool allowZero, out int parsedValue, out string? error)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                parsedValue = defaultValue;
                error = null;
                return true;
            }

            if (!int.TryParse(rawValue, out parsedValue))
            {
                error = $"{fieldName} must be a valid integer.";
                return false;
            }

            if (allowZero)
            {
                if (parsedValue < 0)
                {
                    error = $"{fieldName} must be zero or greater.";
                    return false;
                }
            }
            else if (parsedValue <= 0)
            {
                error = $"{fieldName} must be greater than zero.";
                return false;
            }

            error = null;
            return true;
        }

        void StopBurstClick(string reason, bool notifyWhenInactive = true)
        {
            CancellationTokenSource? cancellationToCancel;

            lock (burstClickStateGate)
            {
                if (!burstClickActive)
                {
                    if (notifyWhenInactive)
                    {
                        Console.WriteLine("Burst click is not active.");
                    }
                    return;
                }

                burstClickStopReason = reason;
                cancellationToCancel = burstClickCancellation;
            }

            cancellationToCancel?.Cancel();
        }

        void StartBurstClickFromTray()
        {
            if (IsBurstClickActive())
            {
                Console.WriteLine("Burst click is already active. Use Stop Burst Click first.");
                return;
            }

            var window = new SimpleWindow("Start Burst Click", labelWidth: 200);
            window.CreateDynamicInputWindow(
                ["Interval (ms)", "Duration (ms, 0 = infinite)"],
                [KeyboardSimulator.DefaultBurstClickIntervalMs.ToString(), KeyboardSimulator.DefaultBurstClickDurationMs.ToString()],
                enableKeyCapture: true
            );

            if (window.userInputs.Count < 2)
            {
                Console.WriteLine("Burst click start canceled.");
                return;
            }

            if (window.capturedKeyVK == 0)
            {
                Console.WriteLine("Burst click requires a captured key.");
                return;
            }

            string intervalText = SanitizeWindowInput(window.userInputs[0]);
            string durationText = SanitizeWindowInput(window.userInputs[1]);

            if (!TryParseBurstInteger(intervalText, KeyboardSimulator.DefaultBurstClickIntervalMs, "Interval", allowZero: false, out int requestedIntervalMs, out string? parseError))
            {
                Console.WriteLine($"Burst click start failed: {parseError}");
                return;
            }

            if (!TryParseBurstInteger(durationText, KeyboardSimulator.DefaultBurstClickDurationMs, "Duration", allowZero: true, out int requestedDurationMs, out parseError))
            {
                Console.WriteLine($"Burst click start failed: {parseError}");
                return;
            }

            VirtualKey requestedKey = (VirtualKey)window.capturedKeyVK;
            if (!KeyboardSimulator.TryValidateBurstClickSettings(requestedKey, requestedIntervalMs, requestedDurationMs, out string? validationError))
            {
                Console.WriteLine($"Burst click start failed: {validationError}");
                return;
            }

            CancellationTokenSource localCancellation;
            lock (burstClickStateGate)
            {
                burstClickKey = requestedKey;
                burstClickIntervalMs = requestedIntervalMs;
                burstClickDurationMs = requestedDurationMs;
                burstClickStopReason = null;
                burstClickCancellation = new CancellationTokenSource();
                localCancellation = burstClickCancellation;
                burstClickActive = true;
            }

            burstClickTask = Task.Run(async () =>
            {
                try
                {
                    await KeyboardSimulator.SimulateBurstClicksAsync(burstClickKey, burstClickIntervalMs, burstClickDurationMs, localCancellation.Token);

                    if (burstClickDurationMs > 0)
                    {
                        Console.WriteLine("Burst click finished after requested duration.");
                    }
                }
                catch (OperationCanceledException)
                {
                    string stopReason;
                    lock (burstClickStateGate)
                    {
                        stopReason = burstClickStopReason ?? "cancellation";
                    }
                    Console.WriteLine($"Burst click stopped ({stopReason}).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Burst click failed: {ex.Message}");
                }
                finally
                {
                    lock (burstClickStateGate)
                    {
                        burstClickActive = false;
                        burstClickStopReason = null;

                        if (ReferenceEquals(burstClickCancellation, localCancellation))
                        {
                            burstClickCancellation.Dispose();
                            burstClickCancellation = null;
                        }

                        burstClickTask = null;
                    }
                }
            });

            Console.WriteLine(
                burstClickDurationMs == 0
                    ? $"Burst click started for key {burstClickKey} every {burstClickIntervalMs}ms. Use tray Stop or press ESC to stop."
                    : $"Burst click started for key {burstClickKey} every {burstClickIntervalMs}ms for {burstClickDurationMs}ms."
            );
        }

        void RequestAppExit(string source)
        {
            if (Interlocked.Exchange(ref exitRequested, 1) == 1)
            {
                return;
            }

            StopBurstClick("application exit", notifyWhenInactive: false);

            mainConfig.Tray.NotificationsHidden = notificationsHidden;
            mainConfig.Tray.ReminderSoundsMuted = reminderSoundsMuted;
            mainConfig.Diagnostics.TerminalMessagesEnabled = terminalMessagesEnabled;
            // mainConfigManager.Save(mainConfig);
            Console.WriteLine($"Exit requested from {source}.");
            trayHost?.Dispose();
            // PostQuitMessage only works on the calling thread. Since tray and hotkey callbacks
            // run on worker threads, target the main thread message loop explicitly.
            PInvoke.PostThreadMessage(mainThreadId, PInvoke.WM_QUIT, 0, 0);
        }

        void OnConsoleCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            // Always intercept terminal cancel so we can shut down through our normal path.
            e.Cancel = true;

            if (Volatile.Read(ref exitRequested) == 1)
            {
                return;
            }

            string shortcut = e.SpecialKey == ConsoleSpecialKey.ControlBreak ? "Ctrl+Break" : "Ctrl+C";
            var result = PInvoke.MessageBox(HWND.Null, $"{shortcut} detected.\n\nDo you want to quit Macrosharp?", "Macrosharp — Confirm Exit", MESSAGEBOX_STYLE.MB_ICONQUESTION | MESSAGEBOX_STYLE.MB_YESNO | MESSAGEBOX_STYLE.MB_TOPMOST);

            if (result == MESSAGEBOX_RESULT.IDYES)
            {
                Console.WriteLine($"{shortcut}: exit confirmed.");
                RequestAppExit(shortcut);
                return;
            }

            Console.WriteLine($"{shortcut}: exit canceled.");
        }

        Console.CancelKeyPress += OnConsoleCancelKeyPress;

        void OpenInShell(string path, string label)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                Console.WriteLine($"Opened {label}: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open {label}: {ex.Message}");
            }
        }

        string textExpansionConfigPath = PathLocator.GetConfigPath("text-expansions.json");
        string reminderConfigPath = PathLocator.GetConfigPath("reminders.json");

        // ═══════════════════════════════════════════════════════════════════════
        //  2. Toast Notification Host
        // ═══════════════════════════════════════════════════════════════════════
        using var toastHost = new ToastNotificationHost("Macrosharp", iconCycler.GetNext());
        toastHost.Start();

        using var reminderConfigManager = new ReminderConfigurationManager(reminderConfigPath, watchForChanges: watchRemindersConfig);
        var reminderCrudService = new ReminderCrudService(reminderConfigManager);
        using var reminderScheduler = new ReminderScheduler(reminderConfigManager, toastHost, () => false, () => notificationsHidden, () => reminderSoundsMuted);
        reminderConfigManager.LoadConfiguration();
        reminderScheduler.Start();

        toastHost.Activated += (_, e) =>
        {
            switch (e.Argument)
            {
                case "action=quit":
                    Console.WriteLine("Toast action: Close App.");
                    RequestAppExit("toast notification");
                    break;
                case "action=open-folder":
                    Console.WriteLine("Toast action: Open Folder.");
                    Process.Start(new ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });
                    break;
                case "action=snooze":
                    Console.WriteLine("Toast action: Snooze acknowledged.");
                    break;
            }
        };

        // Reusable toast content matching the "With Action Buttons" entry
        ToastNotificationContent MakeRunningToast() =>
            new()
            {
                Title = "Macrosharp",
                Body = "Application is running.",
                Actions = new List<ToastAction>
                {
                    new() { Label = "Close App", Argument = "action=quit" },
                    new() { Label = "Open Folder", Argument = "action=open-folder" },
                    new() { Label = "Snooze", Argument = "action=snooze" },
                },
            };

        // ═══════════════════════════════════════════════════════════════════════
        //  3. System Tray Icon
        // ═══════════════════════════════════════════════════════════════════════
        void OpenRunningFolder() => Process.Start(new ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });
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
            mainConfig = mainConfigManager.LoadOrCreate();
            OpenInShell(mainConfigManager.ConfigPath, "main config");
        }
        void OpenTextExpansionConfig() => OpenInShell(textExpansionConfigPath, "text expansion config");
        void OpenRemindersConfig() => OpenInShell(reminderConfigPath, "reminders config");
        void SwitchIcon()
        {
            string? next = iconCycler.GetNext();
            if (!string.IsNullOrWhiteSpace(next))
                trayHost?.UpdateIcon(next);
        }
        void ReloadHotkeys() => Console.WriteLine("Tray action: reload hotkeys.");
        void ReloadConfigs() => Console.WriteLine("Tray action: reload configs.");
        void ReloadReminders()
        {
            reminderConfigManager.ReloadNow();
            Console.WriteLine("Tray action: reminders config reloaded.");
        }
        void AddReminder() => reminderCrudService.AddReminderInteractively();
        void EditReminder() => reminderCrudService.EditReminderInteractively();
        void DeleteReminder() => reminderCrudService.DeleteReminderInteractively();
        void ClearConsoleLogs()
        {
            Console.Clear();
            Console.WriteLine("Console cleared by tray action.");
        }

        void ShowHotkeysWindow()
        {
            if (_hotkeyManager is null)
            {
                Console.WriteLine("Hotkeys are not initialized yet.");
                return;
            }

            var rows = _hotkeyManager
                .GetRegisteredHotkeysSnapshot()
                .Select(h => (IReadOnlyList<string>)new List<string>
                {
                    h.Hotkey.ToString(),
                    string.IsNullOrWhiteSpace(h.Description) ? "No description" : h.Description,
                    string.IsNullOrWhiteSpace(h.SourceContext) ? "No source" : h.SourceContext,
                })
                .ToList();

            Macrosharp.UserInterfaces.DynamicWindow.FilterableTableWindow.ShowOrActivate(
                "Macrosharp Hotkeys",
                ["Hotkey", "Description", "Source"],
                rows,
                filterPlaceholder: "Type to filter hotkeys..."
            );
        }

        string FormatTerminalKeyMessage(KeyboardEvent e)
        {
            var scanCode = PInvoke.MapVirtualKey((uint)e.KeyCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
            var isCapsLockOn = Modifiers.IsCapsLockOn;
            var displayName = KeysMapper.GetDisplayName(e.KeyCode, e.IsShiftDown, isCapsLockOn);
            var asciiCode = KeysMapper.GetAsciiCode(e.KeyCode, e.IsShiftDown, isCapsLockOn);
            var pressedModifiers = Modifiers.GetModifiersStringFromMask(Modifiers.CurrentModifiers);
            if (string.IsNullOrWhiteSpace(pressedModifiers))
                pressedModifiers = "None";

            return $"[Key] {displayName}, VK={(ushort)e.KeyCode, 3}, SC={scanCode, 3}, ASCII={asciiCode, -3} | Modifiers={pressedModifiers} ({Modifiers.CurrentModifiers}) | Ext={e.IsExtendedKey}, Inj={e.IsInjected}, Alt={e.IsAltDown} | Caps={Modifiers.IsCapsLockOn}, Num={Modifiers.IsNumLockOn}, Scroll={Modifiers.IsScrollLockOn}";
        }

        var trayMenu = new List<TrayMenuItem>
        {
            TrayMenuItem.ActionItem("Open Running Folder", OpenRunningFolder, iconPath: iconCycler.GetNext()),
            TrayMenuItem.Submenu(
                "Show Notification",
                new List<TrayMenuItem>
                {
                    TrayMenuItem.ActionItem("Simple", () => toastHost.Show("Macrosharp", "A simple text notification.")),
                    TrayMenuItem.ActionItem(
                        "Long Duration",
                        () =>
                            toastHost.Show(
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
                            toastHost.Show(
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
                            toastHost.Show(
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
                            toastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Reminder",
                                    Body = "Don\u0027t forget your task!",
                                    Scenario = ToastScenario.Reminder,
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "With App Logo",
                        () =>
                            toastHost.Show(
                                new ToastNotificationContent
                                {
                                    Title = "Macrosharp",
                                    Body = "Notification with a custom app logo.",
                                    AppLogoPath = iconPaths.FirstOrDefault(),
                                }
                            )
                    ),
                    TrayMenuItem.ActionItem(
                        "Progress (Indeterminate)",
                        () =>
                            toastHost.Show(
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
                            toastHost.Show(
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
                    TrayMenuItem.ActionItem("With Action Buttons", () => toastHost.Show(MakeRunningToast())),
                },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Notifications & Sounds",
                new List<TrayMenuItem>
                {
                    TrayMenuItem.ActionItem(
                        () => notificationsHidden ? "Show Notifications" : "Hide Notifications",
                        () =>
                        {
                            notificationsHidden = !notificationsHidden;
                            // SaveMainConfig(); // Don't save to config immediately since this is more of a "session" setting and prevents accidental persistence of unwanted states. // ///It will still be saved when the user exits via the tray menu or Win+Esc.
                            Console.WriteLine($"Notifications: {(notificationsHidden ? "Hidden" : "Visible")}");
                            if (!notificationsHidden)
                                AudioPlayer.PlayOnAsync();
                            else
                                AudioPlayer.PlayOffAsync();
                        },
                        iconPath: iconCycler.GetNext()
                    ),
                    TrayMenuItem.ActionItem(
                        () => reminderSoundsMuted ? "Unmute Reminder Sounds" : "Mute Reminder Sounds",
                        () =>
                        {
                            reminderSoundsMuted = !reminderSoundsMuted;
                            // SaveMainConfig();
                            Console.WriteLine($"Reminder sounds: {(reminderSoundsMuted ? "Muted" : "Unmuted")}");
                            if (!reminderSoundsMuted)
                                AudioPlayer.PlayOnAsync();
                        },
                        iconPath: iconCycler.GetNext()
                    ),
                    TrayMenuItem.ActionItem(
                        () => terminalMessagesEnabled ? "Hide Terminal Keystrokes" : "Show Terminal Keystrokes",
                        () =>
                        {
                            terminalMessagesEnabled = !terminalMessagesEnabled;
                            // SaveMainConfig();
                            Console.WriteLine($"Terminal keystrokes: {(terminalMessagesEnabled ? "Shown" : "Hidden")}");
                            if (terminalMessagesEnabled)
                                AudioPlayer.PlayOnAsync();
                            else
                                AudioPlayer.PlayOffAsync();
                        },
                        iconPath: iconCycler.GetNext()
                    ),
                },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Burst Click",
                new List<TrayMenuItem>
                {
                    TrayMenuItem.ActionItem(
                        () => IsBurstClickActive() ? "Start Burst Click (active)" : "Start Burst Click",
                        StartBurstClickFromTray,
                        iconPath: iconCycler.GetNext()
                    ),
                    TrayMenuItem.ActionItem(
                        () => IsBurstClickActive() ? "Stop Burst Click" : "Stop Burst Click (inactive)",
                        () => StopBurstClick("tray menu"),
                        iconPath: iconCycler.GetNext()
                    ),
                },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.ActionItem("Switch Icon", SwitchIcon, iconPath: iconCycler.GetNext()),
            TrayMenuItem.ActionItem("Show Hotkeys", ShowHotkeysWindow, iconPath: iconCycler.GetNext()),
            TrayMenuItem.Submenu(
                "Reload",
                new List<TrayMenuItem> { TrayMenuItem.ActionItem("Hotkeys", ReloadHotkeys, iconPath: iconCycler.GetNext()), TrayMenuItem.ActionItem("Configs", ReloadConfigs, iconPath: iconCycler.GetNext()) },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Reminders",
                new List<TrayMenuItem>
                {
                    TrayMenuItem.ActionItem("Reload reminders config", ReloadReminders, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Add reminder", AddReminder, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Edit reminder", EditReminder, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Delete reminder", DeleteReminder, iconPath: iconCycler.GetNext()),
                },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.Submenu(
                "Configuration",
                new List<TrayMenuItem>
                {
                    TrayMenuItem.ActionItem("Open Main Config", OpenMainConfig, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Text Expansion Config", OpenTextExpansionConfig, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Reminders Config", OpenRemindersConfig, iconPath: iconCycler.GetNext()),
                    TrayMenuItem.ActionItem("Open Project Folder", OpenProjectFolder, iconPath: iconCycler.GetNext()),
                },
                iconPath: iconCycler.GetNext()
            ),
            TrayMenuItem.ActionItem("Clear Console Logs", ClearConsoleLogs, iconPath: iconCycler.GetNext()),
        };

        trayHost = new TrayIconHost("Macrosharp", iconCycler.GetNext(), trayMenu, defaultClickIndex: 2, defaultDoubleClickIndex: 0, quitAction: () => RequestAppExit("tray menu"));
        trayHost.Start();

        // ═══════════════════════════════════════════════════════════════════════
        //  4. Text Expansion
        // ═══════════════════════════════════════════════════════════════════════
        Console.WriteLine($"Text expansion config: {textExpansionConfigPath}");

        using var keyboardHookManager = new KeyboardHookManager();
        using var hotkeyManager = new HotkeyManager(keyboardHookManager);
        _hotkeyManager = hotkeyManager;

        PathLocator.IssueNotifier = OnPathLocatorIssue;
        AudioPlayer.RepeatedFailureNotifier = message => ShowOneTimeWarningDialog("Macrosharp - Audio Warning", message);
        HotkeyManager.RepeatedActionFailureNotifier = message => ShowOneTimeWarningDialog("Macrosharp - Hotkey Warning", message);

        using var textExpansionConfigManager = new TextExpansionConfigurationManager(textExpansionConfigPath, watchForChanges: watchTextExpansionsConfig);
        using var textExpansionManager = new TextExpansionManager(keyboardHookManager);

        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (!terminalMessagesEnabled || e.IsInjected)
                return;

            if (Modifiers.ModifierKeys.Contains(e.KeyCode))
                return;

            Console.WriteLine(FormatTerminalKeyMessage(e));
        };

        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (e.Handled)
                return;

            if (e.KeyCode != VirtualKey.ESCAPE)
                return;

            if (Modifiers.CurrentModifiers != 0)
                return;

            if (!IsBurstClickActive())
                return;

            StopBurstClick("ESC key", notifyWhenInactive: false);
            e.Handled = true;
        };

        var config = textExpansionConfigManager.LoadConfiguration();
        textExpansionManager.LoadConfiguration(config);

        textExpansionConfigManager.ConfigurationChanged += (_, newConfig) =>
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

        Console.WriteLine("\nLoaded expansion rules:");
        foreach (var rule in config.Rules.Where(r => r.Enabled))
            Console.WriteLine($"  {rule}");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════
        //  5. Mouse Hook
        // ═══════════════════════════════════════════════════════════════════════
        using var mouseHookManager = new MouseHookManager();
        using var mouseBindingManager = new MouseBindingManager(mouseHookManager);

        // ═══════════════════════════════════════════════════════════════════════
        //  6. Scroll Lock Keyboard-as-Mouse Handler (bypass HotkeyManager)
        // ═══════════════════════════════════════════════════════════════════════
        //  These hotkeys use a raw KeyDownHandler for two reasons:
        //  1. They must check IsScrollLockOn and pass through when OFF.
        //  2. They should repeat-fire when held (no _activeHotkey suppression).
        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (e.Handled || _paused)
                return;
            if (!Modifiers.IsScrollLockOn)
                return;
            // Skip keys that have modifier combos handled by HotkeyManager
            // (Ctrl+Q/E for zoom are registered there with the ScrollLock guard)
            if (Modifiers.HasModifier(Modifiers.CTRL) || Modifiers.HasModifier(Modifiers.WIN))
                return;
            if (Modifiers.ModifierKeys.Contains(e.KeyCode))
                return;

            bool isAlt = Modifiers.HasModifier(Modifiers.ALT);
            bool isShift = Modifiers.HasModifier(Modifiers.SHIFT);
            bool isBacktick = Modifiers.HasModifier(Modifiers.BACKTICK);

            switch (e.KeyCode)
            {
                // ── Scroll ──
                case VirtualKey.KEY_W when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? 8 : 3, direction: 1));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_S when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? -8 : -3, direction: 1));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_A when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? -8 : -3, direction: 0));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_D when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? 8 : 3, direction: 0));
                    e.Handled = true;
                    return;

                // ── Mouse Clicks ──
                case VirtualKey.KEY_Q when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.LeftButton));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_E when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.RightButton));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_2 when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.MiddleButton));
                    e.Handled = true;
                    return;

                // ── Mouse Hold Toggles (Backtick + Q/E/2) ──
                case VirtualKey.KEY_Q when isBacktick:
                {
                    _leftMouseHeld = !_leftMouseHeld;
                    var op = _leftMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.LeftButton, op: op));
                    e.Handled = true;
                    return;
                }
                case VirtualKey.KEY_E when isBacktick:
                {
                    _rightMouseHeld = !_rightMouseHeld;
                    var op = _rightMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.RightButton, op: op));
                    e.Handled = true;
                    return;
                }
                case VirtualKey.KEY_2 when isBacktick:
                {
                    _middleMouseHeld = !_middleMouseHeld;
                    var op = _middleMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.MiddleButton, op: op));
                    e.Handled = true;
                    return;
                }

                // ── Cursor Movement: ; ' / . ──
                case VirtualKey.OEM_1: // ;
                    Task.Run(() =>
                        MouseSimulator.MoveCursor(
                            dx: isAlt ? 80
                                : isShift ? 3
                                : 20,
                            dy: 0
                        )
                    );
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_7: // '
                    Task.Run(() =>
                        MouseSimulator.MoveCursor(
                            dx: 0,
                            dy: isAlt ? 80
                                : isShift ? 3
                                : 20
                        )
                    );
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_2: // /
                    Task.Run(() =>
                        MouseSimulator.MoveCursor(
                            dx: isAlt ? -80
                                : isShift ? -3
                                : -20,
                            dy: 0
                        )
                    );
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_PERIOD: // .
                    Task.Run(() =>
                        MouseSimulator.MoveCursor(
                            dx: 0,
                            dy: isAlt ? -80
                                : isShift ? -3
                                : -20
                        )
                    );
                    e.Handled = true;
                    return;
            }
        };

        // ═══════════════════════════════════════════════════════════════════════
        //  7. Start Hooks
        // ═══════════════════════════════════════════════════════════════════════
        keyboardHookManager.Start();
        mouseHookManager.Start();

        // ═══════════════════════════════════════════════════════════════════════
        //  8. Register Hotkeys — Application Control
        // ═══════════════════════════════════════════════════════════════════════
        const string SourceApplicationControl = "Application Control";
        const string SourceWindowManagement = "Window Management";
        const string SourceMiscellaneous = "Miscellaneous";
        const string SourceFileManagement = "File Management";
        const int RepeatThrottleMediaSeekMs = 80;
        const int RepeatThrottleVolumeMs = 50;
        const int RepeatThrottleBrightnessMs = 125;
        const int RepeatThrottleZoomMs = 60;

        // Win + Esc → Confirm and terminate application
        hotkeyManager.RegisterHotkey(
            VirtualKey.ESCAPE,
            Modifiers.WIN,
            () =>
            {
                var result = PInvoke.MessageBox(
                    HWND.Null,
                    "Win+Esc detected.\n\nDo you want to quit Macrosharp?",
                    "Macrosharp - Confirm Exit",
                    MESSAGEBOX_STYLE.MB_ICONQUESTION | MESSAGEBOX_STYLE.MB_YESNO | MESSAGEBOX_STYLE.MB_TOPMOST
                );

                if (result != MESSAGEBOX_RESULT.IDYES)
                {
                    Console.WriteLine("Win+Esc: exit canceled.");
                    return;
                }

                Console.WriteLine("Win+Esc: exit confirmed.");
                AudioPlayer.PlayCrackTheWhipAsync(shouldPlayAsync: false); // sync so it finishes before exit
                RequestAppExit("Win+Esc");
            },
            description: "Prompt to terminate Macrosharp.",
            sourceContext: SourceApplicationControl
        );

        // Alt + Win + Esc → Terminate application immediately
        hotkeyManager.RegisterHotkey(
            VirtualKey.ESCAPE,
            Modifiers.ALT_WIN,
            () =>
            {
                Console.WriteLine("Alt+Win+Esc: Terminating application immediately...");
                AudioPlayer.PlayCrackTheWhipAsync(shouldPlayAsync: false); // sync so it finishes before exit
                RequestAppExit("Alt+Win+Esc");
            },
            description: "Terminate Macrosharp immediately.",
            sourceContext: SourceApplicationControl
        );

        // Ctrl + Win + / → Show all registered hotkeys
        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_2,
            Modifiers.CTRL_WIN,
            ShowHotkeysWindow,
            description: "Open the hotkeys reference window.",
            sourceContext: SourceApplicationControl
        );

        // Win + ? (Shift + / produces '?') → Show "application is running" notification
        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_2,
            Modifiers.SHIFT_WIN,
            () =>
            {
                Console.WriteLine("Win+?: Showing 'running' notification.");
                toastHost.Show(MakeRunningToast());
            },
            description: "Show the running status toast with quick actions.",
            sourceContext: SourceApplicationControl
        );

        // Win + Shift + Delete → Clear console output
        hotkeyManager.RegisterHotkey(
            VirtualKey.DELETE,
            Modifiers.SHIFT_WIN,
            () =>
            {
                Console.Clear();
                Console.WriteLine("Console cleared.");
                AudioPlayer.PlayUndoAsync();
            },
            description: "Clear console output.",
            sourceContext: SourceApplicationControl
        );

        // Win + Shift + Insert → Toggle terminal output visibility
        hotkeyManager.RegisterHotkey(
            VirtualKey.INSERT,
            Modifiers.SHIFT_WIN,
            () =>
            {
                bool visible = SystemActions.ToggleConsoleVisibility();
                Console.WriteLine($"Console visibility: {(visible ? "Shown" : "Hidden")}");
                if (visible)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            },
            description: "Toggle console window visibility.",
            sourceContext: SourceApplicationControl
        );

        // Ctrl + Alt + Win + P → Pause/resume all keyboard and mouse event handling
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_P,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                _paused = !_paused;
                Console.WriteLine($"Event handling: {(_paused ? "PAUSED" : "RESUMED")}");
                if (_paused)
                    AudioPlayer.PlayOffAsync();
                else
                    AudioPlayer.PlayOnAsync();
            },
            description: "Pause or resume keyboard and mouse automation.",
            sourceContext: SourceApplicationControl
        );

        // Ctrl + Alt + Win + B → Toggle burst click (start with prompt, stop if active)
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_B,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                if (IsBurstClickActive())
                {
                    StopBurstClick("hotkey");
                }
                else
                {
                    StartBurstClickFromTray();
                }
            },
            description: "Toggle burst click (start when inactive, stop when active).",
            sourceContext: SourceApplicationControl
        );

        // Ctrl + Alt + T → Toggle text expansion
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_T,
            Modifiers.CTRL_ALT,
            () =>
            {
                textExpansionManager.IsEnabled = !textExpansionManager.IsEnabled;
                Console.WriteLine($"Text expansion {(textExpansionManager.IsEnabled ? "ENABLED" : "DISABLED")}");
                if (textExpansionManager.IsEnabled)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            },
            description: "Toggle text expansion on or off.",
            sourceContext: SourceApplicationControl
        );

        // ═══════════════════════════════════════════════════════════════════════
        //  9. Register Hotkeys — Window Management
        // ═══════════════════════════════════════════════════════════════════════
        WindowManagementHotkeyRegistry.Register(hotkeyManager, SourceWindowManagement);

        // ═══════════════════════════════════════════════════════════════════════
        // 10. Register Hotkeys — Miscellaneous
        // ═══════════════════════════════════════════════════════════════════════

        // ` + \ → Open image processing window
        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_5,
            Modifiers.BACKTICK,
            () =>
            {
                Console.WriteLine("Opening image editor...");
                try
                {
                    AudioPlayer.PlayAudio(@"C:\Windows\Media\Windows Proximity Notification.wav", async: true);
                }
                catch (Exception ex)
                {
                    Warn("Program", "PlayImageEditorLaunchSound", "Failed to play image-editor launch sound", ex);
                }
                Task.Run(() => Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.RunWithClipboard());
            },
            description: "Open the image editor from clipboard content.",
            sourceContext: SourceMiscellaneous
        );

        MediaAndDisplayHotkeyRegistry.Register(
            hotkeyManager,
            SourceMiscellaneous,
            SendMpcCommand,
            RepeatThrottleMediaSeekMs,
            RepeatThrottleVolumeMs,
            RepeatThrottleBrightnessMs,
            RepeatThrottleZoomMs
        );

        PowerAndDisplayHotkeyRegistry.Register(
            hotkeyManager,
            SourceMiscellaneous,
            () =>
            {
                var result = PInvoke.MessageBox(HWND.Null, "Are you sure you want to shut down?", "Macrosharp - Shutdown", MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_YESNO);
                return result == MESSAGEBOX_RESULT.IDYES;
            },
            (component, operation, details, ex) => Warn(component, operation, details, ex)
        );

        // ═══════════════════════════════════════════════════════════════════════
        // 11. Register Hotkeys — File Management (Explorer-Focused)
        // ═══════════════════════════════════════════════════════════════════════
        FileManagementHotkeyRegistry.Register(hotkeyManager, SourceFileManagement);

        // ═══════════════════════════════════════════════════════════════════════
        // 12. Startup Banner
        // ═══════════════════════════════════════════════════════════════════════
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║           Macrosharp — Ready                    ║");
        Console.WriteLine("║  Win+Esc        : Quit                          ║");
        Console.WriteLine("║  Win+?          : Show running notification     ║");
        Console.WriteLine("║  Ctrl+Win+/     : Show hotkeys window           ║");
        Console.WriteLine("║  Win+CapsLock   : Toggle Scroll Lock            ║");
        Console.WriteLine("║  Ctrl+Alt+T     : Toggle text expansion         ║");
        Console.WriteLine("║  Ctrl+Alt+Win+P : Pause/resume event handling   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
        AudioPlayer.PlayAchievementAsync(); // startup chime

        // ═══════════════════════════════════════════════════════════════════════
        // 13. Message Loop
        // ═══════════════════════════════════════════════════════════════════════
        MSG msg;
        while (PInvoke.GetMessage(out msg, new HWND(), 0, 0).Value != 0)
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 14. Cleanup
        // ═══════════════════════════════════════════════════════════════════════
        Console.CancelKeyPress -= OnConsoleCancelKeyPress;
        trayHost?.Dispose();
        Console.WriteLine("Application exiting.");
    }

    // ─── MPC-HC Helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a WM_COMMAND to the first MPC-HC window (class "MediaPlayerClassicW").
    /// MPC-HC internal command IDs: 889 = Play/Pause, 905 = Jump Forward (small), 906 = Jump Backward (small).
    /// </summary>
    private static void SendMpcCommand(int commandId)
    {
        var handles = WindowFinder.GetHwndByClassName("MediaPlayerClassicW");
        if (handles.Count > 0)
        {
            Messaging.PostMessageToWindow(handles[0], PInvoke.WM_COMMAND, (WPARAM)(nuint)commandId, default);
        }
    }
}
