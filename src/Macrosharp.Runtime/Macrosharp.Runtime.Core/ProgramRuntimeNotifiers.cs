using Macrosharp.Devices.Keyboard;
using Macrosharp.Infrastructure;
using Macrosharp.Win32.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Runtime.Core;

public static class ProgramRuntimeNotifiers
{
    public static void Warn(string component, string operation, string details, Exception? ex = null)
    {
        string suffix = ex is null ? string.Empty : $" Error='{ex.Message}'.";
        Console.WriteLine($"[WARN] [{component}] Operation='{operation}' Details='{details}'.{suffix}");
    }

    public static void Configure()
    {
        PathLocator.IssueNotifier = OnPathLocatorIssue;
        AudioPlayer.RepeatedFailureNotifier = message => ShowOneTimeWarningDialog("Macrosharp - Audio Warning", message);
        HotkeyManager.RepeatedActionFailureNotifier = message => ShowOneTimeWarningDialog("Macrosharp - Hotkey Warning", message);
    }

    private static void ShowOneTimeWarningDialog(string title, string message)
    {
        try
        {
            MessageBoxes.ShowWarning(HWND.Null, message, title);
        }
        catch (Exception ex)
        {
            Warn("Program", "ShowOneTimeWarningDialog", $"Title='{title}'", ex);
            Warn("Program", "ShowOneTimeWarningDialog", message);
        }
    }

    private static void OnPathLocatorIssue(string message, bool isLongRunningOperation)
    {
        if (isLongRunningOperation)
        {
            ShowOneTimeWarningDialog("Macrosharp - Operation Warning", message);
            return;
        }

        Warn("PathLocator", "NotifyIssue", message);
    }
}
