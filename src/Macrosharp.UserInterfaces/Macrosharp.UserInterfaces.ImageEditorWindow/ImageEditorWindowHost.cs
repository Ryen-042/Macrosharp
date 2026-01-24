using Windows.Win32;
using Windows.Win32.UI.HiDpi;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public static class ImageEditorWindowHost
{
    public static int Run(string title = "Macrosharp Image Editor")
    {
        PInvoke.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
        using var window = new ImageEditorWindow(title);
        return window.Run();
    }

    public static int RunWithFile(string imagePath, string title = "Macrosharp Image Editor")
    {
        PInvoke.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
        using var window = new ImageEditorWindow(title);
        window.QueueOpenFromFile(imagePath);
        return window.Run();
    }

    public static int RunWithClipboard(string title = "Macrosharp Image Editor")
    {
        PInvoke.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
        using var window = new ImageEditorWindow(title);
        window.QueueOpenFromClipboard();
        return window.Run();
    }
}
