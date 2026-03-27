using Macrosharp.UserInterfaces.ToastNotifications;

namespace Macrosharp.Runtime.Core;

public static class ProgramToastSetup
{
    public static void AttachActivatedHandler(ToastNotificationHost toastHost, Action<string> requestAppExit)
    {
        toastHost.Activated += (_, e) =>
        {
            switch (e.Argument)
            {
                case "action=quit":
                    Console.WriteLine("Toast action: Close App.");
                    requestAppExit("toast notification");
                    break;
                case "action=open-folder":
                    Console.WriteLine("Toast action: Open Folder.");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });
                    break;
                case "action=snooze":
                    Console.WriteLine("Toast action: Snooze acknowledged.");
                    break;
            }
        };
    }

    public static ToastNotificationContent CreateRunningToastContent()
    {
        return new ToastNotificationContent
        {
            Title = "Macrosharp",
            Body = "Application is running.",
            Actions = [new ToastAction { Label = "Close App", Argument = "action=quit" }, new ToastAction { Label = "Open Folder", Argument = "action=open-folder" }, new ToastAction { Label = "Snooze", Argument = "action=snooze" }],
        };
    }
}
