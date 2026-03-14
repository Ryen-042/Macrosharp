using System.Diagnostics;
using System.IO;
using System.Linq;
using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.Devices.Mouse;
using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.Reminders;
using Macrosharp.UserInterfaces.ToastNotifications;
using Macrosharp.UserInterfaces.TrayIcon;
using Macrosharp.Win32.Abstractions.Explorer;
using Macrosharp.Win32.Abstractions.SystemControl;
using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Macrosharp.Devices.Keyboard.KeyboardHookManager;

namespace Macrosharp.Hosts.ConsoleHost;

public class Program
{
    // ─── Global State ──────────────────────────────────────────────────────────
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

        bool isSilentMode = false;
        TrayIconHost? trayHost = null;

        // ═══════════════════════════════════════════════════════════════════════
        //  2. Toast Notification Host
        // ═══════════════════════════════════════════════════════════════════════
        using var toastHost = new ToastNotificationHost("Macrosharp", iconCycler.GetNext());
        toastHost.Start();

        string reminderConfigPath = PathLocator.GetConfigPath("reminders.json");
        using var reminderConfigManager = new ReminderConfigurationManager(reminderConfigPath);
        var reminderCrudService = new ReminderCrudService(reminderConfigManager);
        using var reminderScheduler = new ReminderScheduler(reminderConfigManager, toastHost, () => isSilentMode);
        reminderConfigManager.LoadConfiguration();
        reminderScheduler.Start();

        toastHost.Activated += (_, e) =>
        {
            switch (e.Argument)
            {
                case "action=quit":
                    Console.WriteLine("Toast action: Close App.");
                    trayHost?.Dispose();
                    Environment.Exit(0);
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
        void OpenScriptFolder() => Process.Start(new ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });
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
        void ToggleSilentMode()
        {
            isSilentMode = !isSilentMode;
            Console.WriteLine($"Silent mode: {(isSilentMode ? "On" : "Off")}");
        }

        var trayMenu = new List<TrayMenuItem>
        {
            TrayMenuItem.ActionItem("Open Script Folder", OpenScriptFolder, iconPath: iconCycler.GetNext()),
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
            TrayMenuItem.ActionItem("Switch Icon", SwitchIcon, iconPath: iconCycler.GetNext()),
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
            TrayMenuItem.ActionItem("Clear Console Logs", ClearConsoleLogs, iconPath: iconCycler.GetNext()),
            TrayMenuItem.ActionItem("Toggle Silent Mode", ToggleSilentMode, iconPath: iconCycler.GetNext()),
        };

        trayHost = new TrayIconHost("Macrosharp", iconCycler.GetNext(), trayMenu, defaultClickIndex: 2, defaultDoubleClickIndex: 0);
        trayHost.Start();

        // ═══════════════════════════════════════════════════════════════════════
        //  4. Text Expansion
        // ═══════════════════════════════════════════════════════════════════════
        string configPath = PathLocator.GetConfigPath("text-expansions.json");
        Console.WriteLine($"Text expansion config: {configPath}");

        using var keyboardHookManager = new KeyboardHookManager();
        using var hotkeyManager = new HotkeyManager(keyboardHookManager);
        using var textExpansionConfigManager = new TextExpansionConfigurationManager(configPath);
        using var textExpansionManager = new TextExpansionManager(keyboardHookManager);

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

        // Win + Esc → Terminate application
        hotkeyManager.RegisterHotkey(
            VirtualKey.ESCAPE,
            Modifiers.WIN,
            () =>
            {
                Console.WriteLine("Win+Esc: Terminating application...");
                AudioPlayer.PlayCrackTheWhipAsync(shouldPlayAsync: false); // sync so it finishes before exit
                trayHost?.Dispose();
                // PostQuitMessage only works on the calling thread. Since hotkey actions run
                // on a Task.Run thread-pool thread, use PostThreadMessage to target the main
                // thread's GetMessage loop instead.
                PInvoke.PostThreadMessage(mainThreadId, PInvoke.WM_QUIT, 0, 0);
            }
        );

        // Win + ? (Shift + / produces '?') → Show "application is running" notification
        hotkeyManager.RegisterHotkey(
            VirtualKey.OEM_2,
            Modifiers.WIN | Modifiers.SHIFT,
            () =>
            {
                Console.WriteLine("Win+?: Showing 'running' notification.");
                toastHost.Show(MakeRunningToast());
            }
        );

        // Win + Shift + Delete → Clear console output
        hotkeyManager.RegisterHotkey(
            VirtualKey.DELETE,
            Modifiers.WIN | Modifiers.SHIFT,
            () =>
            {
                Console.Clear();
                Console.WriteLine("Console cleared.");
                AudioPlayer.PlayUndoAsync();
            }
        );

        // Win + Shift + Insert → Toggle terminal output visibility
        hotkeyManager.RegisterHotkey(
            VirtualKey.INSERT,
            Modifiers.WIN | Modifiers.SHIFT,
            () =>
            {
                bool visible = SystemActions.ToggleConsoleVisibility();
                Console.WriteLine($"Console visibility: {(visible ? "Shown" : "Hidden")}");
                if (visible)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            }
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
            }
        );

        // Ctrl + Alt + T → Toggle text expansion
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_T,
            Modifiers.CTRL | Modifiers.ALT,
            () =>
            {
                textExpansionManager.IsEnabled = !textExpansionManager.IsEnabled;
                Console.WriteLine($"Text expansion {(textExpansionManager.IsEnabled ? "ENABLED" : "DISABLED")}");
                if (textExpansionManager.IsEnabled)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            }
        );

        // ═══════════════════════════════════════════════════════════════════════
        //  9. Register Hotkeys — Window Management
        // ═══════════════════════════════════════════════════════════════════════

        // ` + = or ` + Add → Increase opacity
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_PLUS, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: 25));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.ADD, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: 25));

        // ` + - or ` + Subtract → Decrease opacity
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_MINUS, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: -25));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.SUBTRACT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowOpacity(opacityDelta: -25));

        // Ctrl + Win + A → Toggle always-on-top
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_A,
            Modifiers.CTRL_WIN,
            () =>
            {
                int state = WindowModifier.ToggleAlwaysOnTopState(default);
                Console.WriteLine($"Always-on-top: {(state == 1 ? "On" : "Off")}");
                if (state == 1)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            }
        );

        // ` + Arrow Keys → Move active window (medium: 50px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: -50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: 50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: -50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: 50));

        // ` + Shift + Arrow Keys → Move active window (small: 10px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: -10));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaY: 10));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: -10));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.SHIFT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaX: 10));

        // ` + Alt + Arrow Keys → Resize active window (50px)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.UP, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaHeight: -50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.DOWN, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaHeight: 50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.LEFT, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaWidth: -50));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.RIGHT, Modifiers.ALT_BACKTICK, () => WindowModifier.AdjustWindowPositionAndSize(deltaWidth: 50));

        // Ctrl + Pause → Suspend active window's process
        hotkeyManager.RegisterHotkey(
            VirtualKey.PAUSE,
            Modifiers.CTRL,
            () =>
            {
                bool ok = ProcessControl.SuspendActiveWindowProcess();
                Console.WriteLine(ok ? "Process suspended." : "Failed to suspend process.");
                if (ok)
                    AudioPlayer.PlayOffAsync();
            }
        );

        // Ctrl + Shift + Pause → Resume active window's process
        hotkeyManager.RegisterHotkey(
            VirtualKey.PAUSE,
            Modifiers.CTRL_SHIFT,
            () =>
            {
                bool ok = ProcessControl.ResumeActiveWindowProcess();
                Console.WriteLine(ok ? "Process resumed." : "Failed to resume process.");
                if (ok)
                    AudioPlayer.PlayOnAsync();
            }
        );

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
                catch { }
                Task.Run(() => Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.RunWithClipboard());
            }
        );

        // ` + W → Seek forward in MPC-HC; ` + S → Seek backward; ` + Space → Play/Pause
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.KEY_W, Modifiers.BACKTICK, () => SendMpcCommand(905)); // Jump forward (small)
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.KEY_S, Modifiers.BACKTICK, () => SendMpcCommand(906)); // Jump backward (small)
        hotkeyManager.RegisterHotkey(VirtualKey.SPACE, Modifiers.BACKTICK, () => SendMpcCommand(889)); // Play/Pause

        // Win + CapsLock → Toggle Scroll Lock
        hotkeyManager.RegisterHotkey(
            VirtualKey.CAPITAL,
            Modifiers.WIN,
            () =>
            {
                KeyboardSimulator.SimulateKeyPress(VirtualKey.SCROLL);
                bool scrollOn = Modifiers.IsScrollLockOn;
                Console.WriteLine($"Scroll Lock toggled → {(scrollOn ? "ON" : "OFF")}");
                if (scrollOn)
                    AudioPlayer.PlayOnAsync();
                else
                    AudioPlayer.PlayOffAsync();
            }
        );

        // Ctrl + Alt + Win + S → Sleep mode
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_S,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                Console.WriteLine("Entering sleep mode...");
                try
                {
                    AudioPlayer.PlayAudio(@"C:\Windows\Media\Windows Logoff Sound.wav");
                }
                catch { } // sync so it finishes before sleep
                SystemActions.Sleep();
            }
        );

        // Ctrl + Alt + Win + Q → Shutdown (with confirmation)
        hotkeyManager.RegisterHotkey(
            VirtualKey.KEY_Q,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                var result = PInvoke.MessageBox(HWND.Null, "Are you sure you want to shut down?", "Macrosharp — Shutdown", MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_YESNO);
                if (result == MESSAGEBOX_RESULT.IDYES)
                {
                    Console.WriteLine("Shutting down...");
                    SystemActions.Shutdown();
                }
            }
        );

        // Ctrl + Alt + Win + Num1-4 → Display switch
        hotkeyManager.RegisterHotkey(
            VirtualKey.NUMPAD1,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                SystemActions.SwitchDisplay(1);
                AudioPlayer.PlayBonkAsync();
            }
        ); // Internal
        hotkeyManager.RegisterHotkey(
            VirtualKey.NUMPAD2,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                SystemActions.SwitchDisplay(2);
                AudioPlayer.PlayBonkAsync();
            }
        ); // External
        hotkeyManager.RegisterHotkey(
            VirtualKey.NUMPAD3,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                SystemActions.SwitchDisplay(3);
                AudioPlayer.PlayBonkAsync();
            }
        ); // Extend
        hotkeyManager.RegisterHotkey(
            VirtualKey.NUMPAD4,
            Modifiers.CTRL_ALT_WIN,
            () =>
            {
                SystemActions.SwitchDisplay(4);
                AudioPlayer.PlayBonkAsync();
            }
        ); // Clone

        // Ctrl + Shift + = or Ctrl + Shift + Add → Increase volume
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_PLUS, Modifiers.CTRL_SHIFT, () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_UP));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.ADD, Modifiers.CTRL_SHIFT, () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_UP));

        // Ctrl + Shift + - or Ctrl + Shift + Subtract → Decrease volume
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.OEM_MINUS, Modifiers.CTRL_SHIFT, () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_DOWN));
        hotkeyManager.RegisterRepeatableHotkey(VirtualKey.SUBTRACT, Modifiers.CTRL_SHIFT, () => KeyboardSimulator.SimulateKeyPress(VirtualKey.VOLUME_DOWN));

        // ` + F2 → Decrease brightness; ` + F3 → Increase brightness
        hotkeyManager.RegisterRepeatableHotkey(
            VirtualKey.F2,
            Modifiers.BACKTICK,
            () =>
            {
                int level = BrightnessControl.DecreaseBrightness();
                if (level >= 0)
                    Console.WriteLine($"Brightness: {level}%");
            }
        );
        hotkeyManager.RegisterRepeatableHotkey(
            VirtualKey.F3,
            Modifiers.BACKTICK,
            () =>
            {
                int level = BrightnessControl.IncreaseBrightness();
                if (level >= 0)
                    Console.WriteLine($"Brightness: {level}%");
            }
        );

        // Ctrl + E (Scroll Lock ON) → Zoom in; Ctrl + Q → Zoom out
        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_E,
            Modifiers.CTRL,
            () =>
            {
                // Simulate Ctrl+ScrollUp for zoom in
                MouseSimulator.SendMouseScroll(steps: 3, direction: 1);
            },
            () => Modifiers.IsScrollLockOn
        );

        hotkeyManager.RegisterConditionalRepeatableHotkey(
            VirtualKey.KEY_Q,
            Modifiers.CTRL,
            () =>
            {
                // Simulate Ctrl+ScrollDown for zoom out
                MouseSimulator.SendMouseScroll(steps: -3, direction: 1);
            },
            () => Modifiers.IsScrollLockOn
        );

        // ═══════════════════════════════════════════════════════════════════════
        // 11. Register Hotkeys — File Management (Explorer-Focused)
        // ═══════════════════════════════════════════════════════════════════════

        // Ctrl + Shift + M → Create new file in Explorer
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_M,
            Modifiers.CTRL_SHIFT,
            () =>
            {
                ExplorerFileAutomation.CreateNewFile();
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused
        );

        // Shift + F2 → Copy full path of selected files to clipboard
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.F2,
            Modifiers.SHIFT,
            () =>
            {
                var paths = ExplorerHotkeys.GetSelectedFilePaths();
                if (paths.Count > 0)
                {
                    string text = string.Join(Environment.NewLine, paths);
                    KeyboardSimulator.SetClipboardText(text);
                    Console.WriteLine($"Copied {paths.Count} path(s) to clipboard.");
                    AudioPlayer.PlaySuccessAsync();
                }
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused
        );

        // ` + P → Convert selected PowerPoint files to PDF
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_P, Modifiers.BACKTICK, () => ExplorerFileAutomation.OfficeFilesToPdf("PowerPoint"), () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn);

        // ` + O → Convert selected Word files to PDF
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_O, Modifiers.BACKTICK, () => ExplorerFileAutomation.OfficeFilesToPdf("Word"), () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn);

        // ` + E → Convert selected Excel files to PDF
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_E, Modifiers.BACKTICK, () => ExplorerFileAutomation.OfficeFilesToPdf("Excel"), () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn);

        // Ctrl + Shift + P → Merge selected images into PDF (Normal mode)
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_P, Modifiers.CTRL_SHIFT, () => ExplorerFileAutomation.ImagesToPdf(), ExplorerHotkeys.IsExplorerOrDesktopFocused);

        // Ctrl + Shift + Alt + P → Merge selected images into PDF (Resize mode)
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_P,
            Modifiers.CTRL_SHIFT_ALT,
            () =>
            {
                var win = new Macrosharp.UserInterfaces.DynamicWindow.SimpleWindow("Images → PDF (Resize)", labelWidth: 160, inputFieldWidth: 120);

                win.CreateDynamicInputWindow(inputLabels: ["Target Width:", "Width Threshold:", "Min Width:", "Min Height:"], placeholders: ["690", "1200", "100", "100"]);

                // If the user dismissed without input, bail out
                if (win.userInputs.Count < 4)
                    return;

                int targetWidth = int.TryParse(win.userInputs[0], out int tw) && tw > 0 ? tw : 690;
                int widthThreshold = int.TryParse(win.userInputs[1], out int wt) && wt > 0 ? wt : 1200;
                int minWidth = int.TryParse(win.userInputs[2], out int mw) && mw > 0 ? mw : 100;
                int minHeight = int.TryParse(win.userInputs[3], out int mh) && mh > 0 ? mh : 100;

                Console.WriteLine($"Images→PDF Resize: targetWidth={targetWidth}, widthThreshold={widthThreshold}, minWidth={minWidth}, minHeight={minHeight}");
                ExplorerFileAutomation.ImagesToPdf(mode: ExplorerFileAutomation.ImagesToPdfMode.Resize, targetWidth: targetWidth, widthThreshold: widthThreshold, minWidth: minWidth, minHeight: minHeight);
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused
        );

        // Ctrl + Alt + Win + I → Convert selected images to .ico
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_I, Modifiers.CTRL_ALT_WIN, () => ExplorerHotkeys.ConvertSelectedImagesToIco(), ExplorerHotkeys.IsExplorerOrDesktopFocused);

        // Ctrl + Alt + Win + M → Convert selected .mp3 files to .wav
        hotkeyManager.RegisterConditionalHotkey(VirtualKey.KEY_M, Modifiers.CTRL_ALT_WIN, () => ExplorerHotkeys.ConvertSelectedMp3ToWav(), ExplorerHotkeys.IsExplorerOrDesktopFocused);

        // ═══════════════════════════════════════════════════════════════════════
        // 12. Startup Banner
        // ═══════════════════════════════════════════════════════════════════════
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║           Macrosharp — Ready                    ║");
        Console.WriteLine("║  Win+Esc        : Quit                          ║");
        Console.WriteLine("║  Win+?          : Show running notification     ║");
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
