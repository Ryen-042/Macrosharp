using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Keyboard.TextExpansion;
using Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;
using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.ToastNotifications;
using Macrosharp.Win32.Abstractions.SystemControl;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Runtime.Core;

public static class ProgramHotkeyRegistration
{
    public sealed class Dependencies
    {
        public required HotkeyManager HotkeyManager { get; init; }
        public required string SourceApplicationControl { get; init; }
        public required string SourceWindowManagement { get; init; }
        public required string SourceMiscellaneous { get; init; }
        public required string SourceFileManagement { get; init; }
        public required ToastNotificationHost ToastHost { get; init; }
        public required Func<ToastNotificationContent> CreateRunningToast { get; init; }
        public required TextExpansionManager TextExpansionManager { get; init; }
        public required Func<bool> IsBurstClickActive { get; init; }
        public required Action StartBurstClick { get; init; }
        public required Action<string> StopBurstClick { get; init; }
        public required Action ShowHotkeysWindow { get; init; }
        public required Func<bool> GetPaused { get; init; }
        public required Action<bool> SetPaused { get; init; }
        public required Action<string> RequestExit { get; init; }
        public required Action<string, string, string, Exception?> Warn { get; init; }
        public required int RepeatThrottleMediaSeekMs { get; init; }
        public required int RepeatThrottleVolumeMs { get; init; }
        public required int RepeatThrottleBrightnessMs { get; init; }
        public required int RepeatThrottleZoomMs { get; init; }
        public required Action<int> SendMpcCommand { get; init; }
    }

    public static void RegisterAll(Dependencies dependencies)
    {
        ApplicationControlHotkeyRegistry.Register(
            dependencies.HotkeyManager,
            dependencies.SourceApplicationControl,
            onConfirmExit: () =>
            {
                var result = PInvoke.MessageBox(HWND.Null, "Win+Esc detected.\n\nDo you want to quit Macrosharp?", "Macrosharp - Confirm Exit", MESSAGEBOX_STYLE.MB_ICONQUESTION | MESSAGEBOX_STYLE.MB_YESNO | MESSAGEBOX_STYLE.MB_TOPMOST);

                if (result != MESSAGEBOX_RESULT.IDYES)
                {
                    Console.WriteLine("Win+Esc: exit canceled.");
                    return;
                }

                Console.WriteLine("Win+Esc: exit confirmed.");
                AudioPlayer.PlayCrackTheWhipAsync(shouldPlayAsync: false);
                dependencies.RequestExit("Win+Esc");
            },
            onImmediateExit: () =>
            {
                Console.WriteLine("Alt+Win+Esc: Terminating application immediately...");
                AudioPlayer.PlayCrackTheWhipAsync(shouldPlayAsync: false);
                dependencies.RequestExit("Alt+Win+Esc");
            },
            onShowHotkeys: dependencies.ShowHotkeysWindow,
            onShowRunningToast: () =>
            {
                Console.WriteLine("Win+?: Showing 'running' notification.");
                dependencies.ToastHost.Show(dependencies.CreateRunningToast());
            },
            onClearConsole: () =>
            {
                Console.Clear();
                Console.WriteLine("Console cleared.");
                AudioPlayer.PlayUndoAsync();
            },
            onToggleConsoleVisibility: () =>
            {
                bool visible = SystemActions.ToggleConsoleVisibility();
                Console.WriteLine($"Console visibility: {(visible ? "Shown" : "Hidden")}");
                if (visible)
                {
                    AudioPlayer.PlayOnAsync();
                }
                else
                {
                    AudioPlayer.PlayOffAsync();
                }
            },
            onTogglePauseResume: () =>
            {
                bool next = !dependencies.GetPaused();
                dependencies.SetPaused(next);
                Console.WriteLine($"Event handling: {(next ? "PAUSED" : "RESUMED")}");
                if (next)
                {
                    AudioPlayer.PlayOffAsync();
                }
                else
                {
                    AudioPlayer.PlayOnAsync();
                }
            },
            onToggleBurstClick: () =>
            {
                if (dependencies.IsBurstClickActive())
                {
                    dependencies.StopBurstClick("hotkey");
                }
                else
                {
                    dependencies.StartBurstClick();
                }
            },
            onToggleTextExpansion: () =>
            {
                dependencies.TextExpansionManager.IsEnabled = !dependencies.TextExpansionManager.IsEnabled;
                Console.WriteLine($"Text expansion {(dependencies.TextExpansionManager.IsEnabled ? "ENABLED" : "DISABLED")}");
                if (dependencies.TextExpansionManager.IsEnabled)
                {
                    AudioPlayer.PlayOnAsync();
                }
                else
                {
                    AudioPlayer.PlayOffAsync();
                }
            }
        );

        WindowManagementHotkeyRegistry.Register(dependencies.HotkeyManager, dependencies.SourceWindowManagement);

        MiscellaneousHotkeyRegistry.Register(
            dependencies.HotkeyManager,
            dependencies.SourceMiscellaneous,
            () =>
            {
                Console.WriteLine("Opening image editor...");
                try
                {
                    AudioPlayer.PlayAudio(@"C:\Windows\Media\Windows Proximity Notification.wav", async: true);
                }
                catch (Exception ex)
                {
                    dependencies.Warn("Program", "PlayImageEditorLaunchSound", "Failed to play image-editor launch sound", ex);
                }

                Task.Run(() => Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.RunWithClipboard());
            }
        );

        MediaAndDisplayHotkeyRegistry.Register(
            dependencies.HotkeyManager,
            dependencies.SourceMiscellaneous,
            dependencies.SendMpcCommand,
            dependencies.RepeatThrottleMediaSeekMs,
            dependencies.RepeatThrottleVolumeMs,
            dependencies.RepeatThrottleBrightnessMs,
            dependencies.RepeatThrottleZoomMs
        );

        PowerAndDisplayHotkeyRegistry.Register(
            dependencies.HotkeyManager,
            dependencies.SourceMiscellaneous,
            () =>
            {
                var result = PInvoke.MessageBox(HWND.Null, "Are you sure you want to shut down?", "Macrosharp - Shutdown", MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_YESNO);
                return result == MESSAGEBOX_RESULT.IDYES;
            },
            dependencies.Warn
        );

        FileManagementHotkeyRegistry.Register(dependencies.HotkeyManager, dependencies.SourceFileManagement);
    }
}



