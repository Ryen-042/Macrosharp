using System.Runtime.InteropServices;
using WindowsNotifications = Windows.UI.Notifications;

namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Manages Windows toast notifications for an unpackaged desktop application.
/// Thread-safe. All public methods may be called from any thread.
/// </summary>
public sealed class ToastNotificationHost : IDisposable
{
    /// <summary>
    /// Application User Model ID used for AUMID registration, Start Menu shortcut stamping,
    /// and toast notifier creation. Must be consistent across all three call sites.
    /// </summary>
    internal const string AppUserModelId = "Macrosharp.Desktop";

    private readonly string appName;
    private readonly string? iconPath;
    private readonly object lifecycleLock = new();

    private WindowsNotifications.ToastNotifier? notifier;
    private bool isRunning;

    /// <summary>
    /// Creates a new toast notification host.
    /// </summary>
    /// <param name="appName">Display name for the app (used in shortcut description).</param>
    /// <param name="iconPath">
    /// Optional path to the app icon (.ico) used for the Start Menu shortcut
    /// and as the default toast app logo. <c>null</c> uses the running executable's icon.
    /// </param>
    public ToastNotificationHost(string appName, string? iconPath = null)
    {
        this.appName = appName;
        this.iconPath = iconPath;
    }

    /// <summary>
    /// Gets whether the host is currently running and able to show toast notifications.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (lifecycleLock)
            {
                return isRunning;
            }
        }
    }

    /// <summary>
    /// Registers the process AUMID via <c>SetCurrentProcessExplicitAppUserModelID</c>.
    /// Call once at the very top of <c>Main()</c>, before any toast or tray icon work.
    /// </summary>
    public static void RegisterAppUserModelId()
    {
        int hr = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        if (hr < 0)
        {
            Console.WriteLine($"AUMID registration failed: HRESULT 0x{hr:X8}");
        }
        else
        {
            Console.WriteLine($"AUMID registered: {AppUserModelId}");
        }
    }

    [DllImport("shell32.dll", PreserveSig = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    /// <summary>
    /// Starts the notification host: ensures the Start Menu shortcut exists and
    /// initializes the WinRT toast notifier. Returns <c>true</c> on success.
    /// </summary>
    public bool Start()
    {
        lock (lifecycleLock)
        {
            if (isRunning)
            {
                return true;
            }

            try
            {
                string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException("Unable to determine the executable path.");

                ShortcutManager.EnsureStartMenuShortcut(appName, exePath, iconPath, AppUserModelId);

                notifier = WindowsNotifications.ToastNotificationManager.CreateToastNotifier(AppUserModelId);
                isRunning = true;

                Console.WriteLine("Toast notification host started.");
                return true;
            }
            catch (TypeLoadException ex)
            {
                Console.WriteLine($"Toast notification infrastructure unavailable: {ex.Message}");
                return false;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Console.WriteLine($"Toast notification infrastructure unavailable: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Toast notification host failed to start: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Stops the notification host and releases resources.
    /// </summary>
    public void Stop()
    {
        lock (lifecycleLock)
        {
            if (!isRunning)
            {
                return;
            }

            notifier = null;
            isRunning = false;
            Console.WriteLine("Toast notification host stopped.");
        }
    }

    /// <summary>Shows a simple text toast with default settings.</summary>
    /// <param name="title">The toast headline.</param>
    /// <param name="message">The toast body text.</param>
    public void Show(string title, string message)
    {
        Show(new ToastNotificationContent { Title = title, Body = message });
    }

    /// <summary>Shows a toast with optional icon and duration control.</summary>
    /// <param name="title">The toast headline.</param>
    /// <param name="message">The toast body text.</param>
    /// <param name="iconPath">Optional path to an image for the app logo override.</param>
    /// <param name="duration">Controls how long the toast remains visible.</param>
    public void Show(string title, string message, string? iconPath, ToastDuration duration = ToastDuration.Default)
    {
        Show(new ToastNotificationContent
        {
            Title = title,
            Body = message,
            AppLogoPath = iconPath,
            Duration = duration
        });
    }

    /// <summary>Shows a toast from a structured content model.</summary>
    /// <param name="content">The toast content to display.</param>
    public void Show(ToastNotificationContent content)
    {
        lock (lifecycleLock)
        {
            if (!isRunning || notifier is null)
            {
                Console.WriteLine("Toast notification host is not running. Call Start() first.");
                return;
            }
        }

        try
        {
            var xmlDoc = ToastXmlBuilder.Build(content);
            var toast = new WindowsNotifications.ToastNotification(xmlDoc);

            WireEvents(toast);

            // ToastNotifier.Show is thread-safe
            notifier!.Show(toast);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toast notification failed: {ex.Message}");
            Failed?.Invoke(this, new ToastFailedEventArgs(ex));
        }
    }

    /// <summary>
    /// Raised when the user clicks the toast body or an action button.
    /// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
    /// </summary>
    public event EventHandler<ToastActivatedEventArgs>? Activated;

    /// <summary>
    /// Raised when a toast is dismissed (by user, timeout, or app).
    /// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
    /// </summary>
    public event EventHandler<ToastDismissedEventArgs>? Dismissed;

    /// <summary>
    /// Raised when a toast fails to display.
    /// WARNING: This event fires on a background COM thread — marshal to your own thread if needed.
    /// </summary>
    public event EventHandler<ToastFailedEventArgs>? Failed;

    /// <summary>Stops the host and releases resources.</summary>
    public void Dispose()
    {
        Stop();
    }

    private void WireEvents(WindowsNotifications.ToastNotification toast)
    {
        toast.Activated += (sender, args) =>
        {
            string argument = string.Empty;
            if (args is WindowsNotifications.ToastActivatedEventArgs toastArgs)
            {
                argument = toastArgs.Arguments ?? string.Empty;
            }
            Activated?.Invoke(this, new ToastActivatedEventArgs(argument));
        };

        toast.Dismissed += (sender, args) =>
        {
            var reason = args.Reason switch
            {
                WindowsNotifications.ToastDismissalReason.UserCanceled => ToastDismissReason.UserCanceled,
                WindowsNotifications.ToastDismissalReason.ApplicationHidden => ToastDismissReason.ApplicationHidden,
                WindowsNotifications.ToastDismissalReason.TimedOut => ToastDismissReason.TimedOut,
                _ => ToastDismissReason.TimedOut
            };
            Dismissed?.Invoke(this, new ToastDismissedEventArgs(reason));
        };

        toast.Failed += (sender, args) =>
        {
            Failed?.Invoke(this, new ToastFailedEventArgs(args.ErrorCode));
        };
    }
}
