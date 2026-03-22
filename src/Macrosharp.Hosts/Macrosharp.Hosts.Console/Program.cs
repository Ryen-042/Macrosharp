using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.Devices.Mouse;
using Macrosharp.Infrastructure;
using Macrosharp.Runtime.Configuration;
using Macrosharp.Runtime.Core;
using Macrosharp.UserInterfaces.Reminders;
using Macrosharp.UserInterfaces.ToastNotifications;
using Macrosharp.UserInterfaces.TrayIcon;
using Macrosharp.Win32.Abstractions.Explorer;
using Macrosharp.Win32.Abstractions.SystemControl;
using Windows.Win32;

namespace Macrosharp.Hosts.ConsoleHost;

public class Program
{
    // ─── Global State ──────────────────────────────────────────────────────────
    private static HotkeyManager? _hotkeyManager;
    private static bool _paused; // Ctrl+Alt+Win+P toggle

    private const string SourceApplicationControl = "Application Control";
    private const string SourceWindowManagement = "Window Management";
    private const string SourceMiscellaneous = "Miscellaneous";
    private const string SourceFileManagement = "File Management";
    private const int RepeatThrottleMediaSeekMs = 80;
    private const int RepeatThrottleVolumeMs = 50;
    private const int RepeatThrottleBrightnessMs = 125;
    private const int RepeatThrottleZoomMs = 60;

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
        var runtimeState = new ProgramRuntimeState(mainConfigManager.LoadOrCreate());
        var burstClickCoordinator = new BurstClickCoordinator();

        TrayIconHost? trayHost = null;
        var exitCoordinator = new ProgramExitCoordinator(mainThreadId, burstClickCoordinator, runtimeState, () => trayHost);

        ProgramMainConfigurationSetup.Configure(mainConfigManager, runtimeState);

        bool IsBurstClickActive() => burstClickCoordinator.IsActive();
        void StopBurstClick(string reason, bool notifyWhenInactive = true) => burstClickCoordinator.Stop(reason, notifyWhenInactive);
        void StartBurstClickFromTray() => burstClickCoordinator.Start();
        void RequestAppExit(string source) => exitCoordinator.RequestExit(source);

        var onConsoleCancelKeyPress = exitCoordinator.CreateConsoleCancelHandler();
        Console.CancelKeyPress += onConsoleCancelKeyPress;

        string textExpansionConfigPath = PathLocator.GetConfigPath("text-expansions.json");
        string reminderConfigPath = PathLocator.GetConfigPath("reminders.json");
        using var textExpansionConfigManager = new TextExpansionConfigurationManager(textExpansionConfigPath, watchForChanges: runtimeState.WatchTextExpansionsConfig);

        // ═══════════════════════════════════════════════════════════════════════
        //  2. Toast Notification Host
        // ═══════════════════════════════════════════════════════════════════════
        using var toastHost = new ToastNotificationHost("Macrosharp", iconCycler.GetNext());
        toastHost.Start();

        using var reminderConfigManager = new ReminderConfigurationManager(reminderConfigPath, watchForChanges: runtimeState.WatchRemindersConfig);
        var reminderCrudService = new ReminderCrudService(reminderConfigManager);
        using var reminderScheduler = new ReminderScheduler(reminderConfigManager, toastHost, () => false, () => runtimeState.NotificationsHidden, () => runtimeState.ReminderSoundsMuted);
        reminderConfigManager.LoadConfiguration();
        reminderScheduler.Start();

        ProgramToastSetup.AttachActivatedHandler(toastHost, RequestAppExit);
        ToastNotificationContent MakeRunningToast() => ProgramToastSetup.CreateRunningToastContent();

        var trayMenu = ProgramTrayMenuFactory.Build(
            new ProgramTrayMenuFactory.Dependencies
            {
                IconCycler = iconCycler,
                IconPaths = iconPaths,
                ToastHost = toastHost,
                MainConfigurationManager = mainConfigManager,
                ReminderConfigurationManager = reminderConfigManager,
                ReminderCrudService = reminderCrudService,
                TextExpansionConfigPath = textExpansionConfigPath,
                ReminderConfigPath = reminderConfigPath,
                GetNotificationsHidden = () => runtimeState.NotificationsHidden,
                SetNotificationsHidden = value => runtimeState.NotificationsHidden = value,
                GetReminderSoundsMuted = () => runtimeState.ReminderSoundsMuted,
                SetReminderSoundsMuted = value => runtimeState.ReminderSoundsMuted = value,
                GetTerminalMessagesEnabled = () => runtimeState.TerminalMessagesEnabled,
                SetTerminalMessagesEnabled = value => runtimeState.TerminalMessagesEnabled = value,
                IsBurstClickActive = IsBurstClickActive,
                StartBurstClick = StartBurstClickFromTray,
                StopBurstClick = reason => StopBurstClick(reason),
                ShowHotkeysWindow = () => RuntimeHotkeyReferenceWindow.Show(_hotkeyManager),
                ShowTextExpansionsWindow = () => RuntimeTextExpansionReferenceWindow.Show(textExpansionConfigManager),
                CreateRunningToast = MakeRunningToast,
                GetTrayHost = () => trayHost,
            }
        );

        trayHost = new TrayIconHost("Macrosharp", iconCycler.GetNext(), trayMenu, defaultClickIndex: 2, defaultDoubleClickIndex: 0, quitAction: () => RequestAppExit("tray menu"));
        trayHost.Start();

        // ═══════════════════════════════════════════════════════════════════════
        //  4. Text Expansion
        // ═══════════════════════════════════════════════════════════════════════
        Console.WriteLine($"Text expansion config: {textExpansionConfigPath}");

        using var keyboardHookManager = new KeyboardHookManager();
        using var hotkeyManager = new HotkeyManager(keyboardHookManager);
        _hotkeyManager = hotkeyManager;

        ProgramRuntimeNotifiers.Configure();

        using var textExpansionManager = new TextExpansionManager(keyboardHookManager);
        ProgramKeyboardHandlerSetup.SetupTerminalKeyLoggingHandler(keyboardHookManager, () => runtimeState.TerminalMessagesEnabled);
        ProgramKeyboardHandlerSetup.SetupBurstClickEscapeStopHandler(keyboardHookManager, IsBurstClickActive, () => StopBurstClick("ESC key", notifyWhenInactive: false));

        var config = ProgramTextExpansionSetup.Configure(textExpansionConfigManager, textExpansionManager);
        ProgramTextExpansionSetup.PrintLoadedRules(config);

        // ═══════════════════════════════════════════════════════════════════════
        //  5. Mouse Hook
        // ═══════════════════════════════════════════════════════════════════════
        using var mouseHookManager = new MouseHookManager();
        using var mouseBindingManager = new MouseBindingManager(mouseHookManager);

        ProgramKeyboardHandlerSetup.SetupScrollLockMouseHandler(keyboardHookManager, () => _paused);

        // ═══════════════════════════════════════════════════════════════════════
        //  7. Start Hooks
        // ═══════════════════════════════════════════════════════════════════════
        keyboardHookManager.Start();
        mouseHookManager.Start();

        ProgramHotkeyRegistration.RegisterAll(
            new ProgramHotkeyRegistration.Dependencies
            {
                HotkeyManager = hotkeyManager,
                SourceApplicationControl = SourceApplicationControl,
                SourceWindowManagement = SourceWindowManagement,
                SourceMiscellaneous = SourceMiscellaneous,
                SourceFileManagement = SourceFileManagement,
                ToastHost = toastHost,
                CreateRunningToast = MakeRunningToast,
                TextExpansionManager = textExpansionManager,
                IsBurstClickActive = IsBurstClickActive,
                StartBurstClick = StartBurstClickFromTray,
                StopBurstClick = reason => StopBurstClick(reason),
                ShowHotkeysWindow = () => RuntimeHotkeyReferenceWindow.Show(_hotkeyManager),
                ShowTextExpansionsWindow = () => RuntimeTextExpansionReferenceWindow.Show(textExpansionConfigManager),
                GetPaused = () => _paused,
                SetPaused = value => _paused = value,
                RequestExit = RequestAppExit,
                Warn = ProgramRuntimeNotifiers.Warn,
                RepeatThrottleMediaSeekMs = RepeatThrottleMediaSeekMs,
                RepeatThrottleVolumeMs = RepeatThrottleVolumeMs,
                RepeatThrottleBrightnessMs = RepeatThrottleBrightnessMs,
                RepeatThrottleZoomMs = RepeatThrottleZoomMs,
                SendMpcCommand = ProgramMpcCommands.Send,
            }
        );

        // ═══════════════════════════════════════════════════════════════════════
        // 12. Startup Banner
        // ═══════════════════════════════════════════════════════════════════════
        ProgramStartupBanner.Write();
        AudioPlayer.PlayAchievementAsync(); // startup chime

        // ═══════════════════════════════════════════════════════════════════════
        // 13. Message Loop
        // ═══════════════════════════════════════════════════════════════════════
        ProgramMessageLoop.Run();

        // ═══════════════════════════════════════════════════════════════════════
        // 14. Cleanup
        // ═══════════════════════════════════════════════════════════════════════
        void CleanupAndExit()
        {
            Console.CancelKeyPress -= onConsoleCancelKeyPress;
            trayHost?.Dispose();
            Console.WriteLine("Application exiting.");
        }

        CleanupAndExit();
    }
}
