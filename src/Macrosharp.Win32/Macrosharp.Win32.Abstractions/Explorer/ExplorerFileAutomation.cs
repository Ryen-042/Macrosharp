using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Macrosharp.Infrastructure;
using Macrosharp.Infrastructure.ImageProcessing;
using Macrosharp.Win32.Abstractions.WindowTools;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Abstractions.Explorer;

/// <summary>
/// Provides file-related actions for the active Explorer/desktop window.
/// </summary>
public static class ExplorerFileAutomation
{
    private static readonly StaThreadRunner Sta = new();

    public enum ImagesToPdfMode
    {
        Normal = 1,
        Resize = 2,
    }

    /// <summary>
    /// Creates a new incremental text file in the active Explorer/desktop folder and enters edit mode.
    /// Returns 1 when a file was created, otherwise 0.
    /// </summary>
    public static int CreateNewFile(HWND hwnd = default)
    {
        if (!TryGetActiveFolderPath(hwnd, allowDesktop: true, out string folderPath, out HWND targetHwnd))
            return 0;

        try
        {
            Directory.CreateDirectory(folderPath);
            string filePath = GetUniqueFilePath(folderPath, "New File", ".txt");
            File.Create(filePath).Dispose();
            // File.WriteAllText(filePath, "# Created using \"File Factory\"");

            bool selected = SelectAndEditItem(targetHwnd, folderPath, filePath);
            if (!selected)
            {
                ExplorerShellManager.SelectItems(folderPath, new[] { filePath }, targetHwnd);
                SendF2(targetHwnd);
            }

            AudioPlayer.PlaySuccessAsync();
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Converts selected Office files in the active Explorer window to PDF.
    /// Supports PowerPoint, Word, and Excel.
    /// </summary>
    public static void OfficeFilesToPdf(string officeApplication = "PowerPoint", HWND hwnd = default)
    {
        if (string.IsNullOrWhiteSpace(officeApplication))
            throw new ArgumentException("Office application is required.", nameof(officeApplication));

        if (!TryGetActiveFolderPath(hwnd, allowDesktop: false, out _, out HWND targetHwnd))
            return;

        var selected = ExplorerShellManager.GetSelectedItems(targetHwnd);
        if (selected.Count == 0)
            return;

        char appKey = char.ToLowerInvariant(officeApplication.Trim()[0]);
        var extensions = GetOfficeExtensions(appKey);
        if (extensions.Count == 0)
            return;

        bool skippedExisting = false;
        var targets = new List<string>();
        foreach (string filePath in selected)
        {
            if (!HasExtension(filePath, extensions))
                continue;

            string pdfPath = Path.ChangeExtension(filePath, ".pdf");
            if (File.Exists(pdfPath))
            {
                skippedExisting = true;
                continue;
            }

            targets.Add(filePath);
        }

        if (targets.Count == 0)
        {
            if (skippedExisting)
                AudioPlayer.PlayFailure();
            return;
        }

        if (skippedExisting)
            AudioPlayer.PlayFailure();

        AudioPlayer.PlayStartAsync();
        Sta.Run(() => ConvertOfficeFilesToPdfInternal(appKey, officeApplication, targets));

        string lastPdf = Path.ChangeExtension(targets[^1], ".pdf");
        ExplorerShellManager.SelectItems(Path.GetDirectoryName(lastPdf) ?? string.Empty, new[] { lastPdf }, targetHwnd);
        AudioPlayer.PlaySuccessAsync();
    }

    /// <summary>
    /// Converts selected files in the active Explorer window using the provided filter and conversion function.
    /// </summary>
    public static void GenericFileConverter(IReadOnlyCollection<string>? patterns, Action<string, string> convertFunc, string? newLocation = null, string newExtension = "", HWND hwnd = default)
    {
        if (convertFunc == null)
            throw new ArgumentNullException(nameof(convertFunc));

        var confirm = PInvoke.MessageBox(HWND.Null, "Are you sure you want to convert the selected files?", "Confirmation", MESSAGEBOX_STYLE.MB_ICONQUESTION | MESSAGEBOX_STYLE.MB_YESNO);
        if (confirm != MESSAGEBOX_RESULT.IDYES)
            return;

        if (!TryGetActiveFolderPath(hwnd, allowDesktop: false, out _, out HWND targetHwnd))
            return;

        var selected = ExplorerShellManager.GetSelectedItems(targetHwnd);
        if (selected.Count == 0)
            return;

        var filtered = new List<string>();
        foreach (string filePath in selected)
        {
            if (patterns == null || patterns.Count == 0 || HasExtension(filePath, patterns))
                filtered.Add(filePath);
        }

        if (filtered.Count == 0)
            return;

        if (!string.IsNullOrWhiteSpace(newLocation))
            Directory.CreateDirectory(newLocation);

        AudioPlayer.PlayStartAsync();

        bool anyConverted = false;
        string? lastOutput = null;
        foreach (string filePath in filtered)
        {
            string newFilePath = Path.ChangeExtension(filePath, null) + newExtension;
            if (!string.IsNullOrWhiteSpace(newLocation))
                newFilePath = Path.Combine(newLocation!, Path.GetFileName(newFilePath));

            if (File.Exists(newFilePath))
                continue;

            convertFunc(filePath, newFilePath);
            anyConverted = true;
            lastOutput = newFilePath;
        }

        if (!anyConverted)
        {
            AudioPlayer.PlayFailure();
            return;
        }

        AudioPlayer.PlaySuccessAsync();

        if (!string.IsNullOrWhiteSpace(lastOutput))
        {
            ExplorerShellManager.SelectItems(Path.GetDirectoryName(lastOutput) ?? string.Empty, new[] { lastOutput }, targetHwnd);
        }
    }

    /// <summary>
    /// Flattens selected folders in the active Explorer window into a new "Flattened" folder.
    /// </summary>
    public static void FlattenDirectories(HWND hwnd = default)
    {
        if (!TryGetActiveFolderPath(hwnd, allowDesktop: false, out string folderPath, out HWND targetHwnd))
            return;

        var selected = ExplorerShellManager.GetSelectedItems(targetHwnd);
        if (selected.Count == 0)
            return;

        var directories = selected.Where(Directory.Exists).ToList();
        if (directories.Count == 0)
            return;

        string destination = GetUniqueFolderPath(folderPath, "Flattened");
        Directory.CreateDirectory(destination);

        foreach (string directory in directories)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                string targetPath = Path.Combine(destination, Path.GetFileName(filePath));
                if (File.Exists(targetPath))
                    continue;

                try
                {
                    File.Move(filePath, targetPath);
                }
                catch (IOException)
                {
                    // Skip files that cannot be moved.
                }
            }
        }

        ExplorerShellManager.SelectItems(folderPath, new[] { destination }, targetHwnd);
    }

    /// <summary>
    /// Combines selected images into a PDF file, with optional resize mode.
    /// </summary>
    public static void ImagesToPdf(ImagesToPdfMode mode = ImagesToPdfMode.Normal, int targetWidth = 690, int widthThreshold = 1200, int minWidth = 100, int minHeight = 100, HWND hwnd = default)
    {
        if (!TryGetActiveFolderPath(hwnd, allowDesktop: false, out _, out HWND targetHwnd))
        {
            AudioPlayer.PlayFailure();
            return;
        }

        var selected = ExplorerShellManager.GetSelectedItems(targetHwnd);
        if (selected.Count == 0)
        {
            AudioPlayer.PlayFailure();
            return;
        }

        var imageFiles = selected.Where(path => HasExtension(path, ImageExtensions)).ToList();
        if (imageFiles.Count == 0)
        {
            AudioPlayer.PlayFailure();
            return;
        }

        string outputDirectory = Path.GetDirectoryName(imageFiles[0]) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            AudioPlayer.PlayFailure();
            return;
        }

        AudioPlayer.PlayStartAsync();

        string? tempDirectory = null;
        try
        {
            var prepared = ImagePdfUtilities.FilterAndPrepareImages(
                imageFiles,
                outputDirectory,
                resizeMode: mode == ImagesToPdfMode.Resize,
                targetWidth: targetWidth,
                widthThreshold: widthThreshold,
                minWidth: minWidth,
                minHeight: minHeight,
                tempDirectory: out tempDirectory
            );

            if (prepared.Count == 0)
            {
                AudioPlayer.PlayFailure();
                return;
            }

            var sorted = ImagePdfUtilities.OrderByNaturalFileName(prepared).ToList();
            string outputPath = GetUniqueFilePath(outputDirectory, "New PDF", ".pdf");

            ImagePdfUtilities.CreatePdfFromImages(sorted, outputPath);

            if (sorted.Count <= 20)
                ExplorerShellManager.SelectItems(outputDirectory, new[] { outputPath }, targetHwnd);

            AudioPlayer.PlaySuccessAsync();
        }
        catch
        {
            AudioPlayer.PlayFailure();
        }
        finally
        {
            ImagePdfUtilities.CleanupTempDirectory(tempDirectory);
        }
    }

    #region Helpers

    private static bool TryGetActiveFolderPath(HWND hwnd, bool allowDesktop, out string folderPath, out HWND targetHwnd)
    {
        folderPath = string.Empty;
        targetHwnd = hwnd == HWND.Null ? PInvoke.GetForegroundWindow() : hwnd;

        string? explorerPath = ExplorerShellManager.GetCurrentFolderPath(targetHwnd);
        if (!string.IsNullOrWhiteSpace(explorerPath))
        {
            folderPath = explorerPath;
            return true;
        }

        if (!allowDesktop)
            return false;

        try
        {
            string className = WindowFinder.GetWindowClassName(targetHwnd);
            if (string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase) || string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                return !string.IsNullOrWhiteSpace(folderPath);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static IReadOnlyCollection<string> GetOfficeExtensions(char appKey)
    {
        return appKey switch
        {
            'p' => new[] { ".pptx", ".ppt" },
            'w' => new[] { ".docx", ".doc" },
            'e' => new[] { ".xlsx", ".xls", ".xlsm" },
            _ => Array.Empty<string>(),
        };
    }

    private static readonly IReadOnlyCollection<string> ImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

    private static bool HasExtension(string filePath, IReadOnlyCollection<string> extensions)
    {
        string ext = Path.GetExtension(filePath) ?? string.Empty;
        foreach (string allowed in extensions)
        {
            if (ext.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetUniqueFilePath(string folderPath, string baseName, string extension)
    {
        string normalizedExtension;
        if (string.IsNullOrWhiteSpace(extension))
        {
            normalizedExtension = string.Empty;
        }
        else if (extension.StartsWith('.'))
        {
            normalizedExtension = extension;
        }
        else
        {
            normalizedExtension = "." + extension;
        }

        string candidate = Path.Combine(folderPath, baseName + normalizedExtension);
        if (!File.Exists(candidate))
            return candidate;

        for (int i = 1; i < 10000; i++)
        {
            candidate = Path.Combine(folderPath, $"{baseName} ({i}){normalizedExtension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(folderPath, $"{baseName} ({Guid.NewGuid():N}){normalizedExtension}");
    }

    private static string GetUniqueFolderPath(string parentFolder, string baseName)
    {
        string candidate = Path.Combine(parentFolder, baseName);
        if (!Directory.Exists(candidate))
            return candidate;

        for (int i = 1; i < 10000; i++)
        {
            candidate = Path.Combine(parentFolder, $"{baseName} ({i})");
            if (!Directory.Exists(candidate))
                return candidate;
        }

        return Path.Combine(parentFolder, $"{baseName} ({Guid.NewGuid():N})");
    }

    private static bool SelectAndEditItem(HWND hwnd, string folderPath, string filePath)
    {
        return Sta.Run(() =>
        {
            if (!TryGetShellDocument(hwnd, out dynamic? document, out string? currentFolder))
                return false;

            if (!string.IsNullOrWhiteSpace(currentFolder) && !Path.Equals(Path.GetFullPath(currentFolder), Path.GetFullPath(folderPath)))
            {
                return false;
            }

            if (document == null)
                return false;

            dynamic? folder = document?.Folder;
            if (folder == null)
                return false;

            string name = Path.GetFileName(filePath);
            dynamic? item = folder.ParseName(name);
            if (item == null)
                return false;

            const int selectEditFlags = 0x1F;
            document!.SelectItem(item, selectEditFlags);
            return true;
        });
    }

    private static void SendF2(HWND hwnd)
    {
        HWND target = hwnd == HWND.Null ? PInvoke.GetForegroundWindow() : hwnd;
        if (target != HWND.Null && target != PInvoke.GetForegroundWindow())
        {
            PInvoke.SetForegroundWindow(target);
            Thread.Sleep(50);
        }

        var inputs = new INPUT[2];

        inputs[0].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[0].Anonymous.ki.wVk = VIRTUAL_KEY.VK_F2;

        inputs[1].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[1].Anonymous.ki.wVk = VIRTUAL_KEY.VK_F2;
        inputs[1].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        PInvoke.SendInput(inputs.AsSpan(), Marshal.SizeOf<INPUT>());
    }

    private static bool TryGetShellDocument(HWND hwnd, out dynamic? document, out string? folderPath)
    {
        document = null;
        folderPath = null;
        HWND target = hwnd == HWND.Null ? PInvoke.GetForegroundWindow() : hwnd;

        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
                return false;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic windows = shell.Windows();
            int count = windows.Count;

            dynamic? bestMatch = null;
            for (int i = 0; i < count; i++)
            {
                dynamic window = windows.Item(i);
                nint windowHwnd = (nint)(int)window.HWND;

                if (windowHwnd == target)
                {
                    bestMatch = window;
                    break;
                }

                if (PInvoke.GetAncestor((HWND)windowHwnd, GET_ANCESTOR_FLAGS.GA_ROOT) == target)
                    bestMatch ??= window;
            }

            if (bestMatch == null)
                return false;

            document = bestMatch.Document;
            try
            {
                dynamic? folder = document?.Folder;
                string? path = folder?.Self?.Path as string;
                if (!string.IsNullOrWhiteSpace(path))
                    folderPath = path;
            }
            catch
            {
                // Ignore failures.
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ConvertOfficeFilesToPdfInternal(char appKey, string officeApplication, IReadOnlyList<string> files)
    {
        Type? appType = Type.GetTypeFromProgID($"{officeApplication}.Application");
        if (appType == null)
            return;

        dynamic? app = null;
        try
        {
            app = Activator.CreateInstance(appType);
            if (app == null)
                return;

            if (appKey == 'w')
                app.DisplayAlerts = 0;
            if (appKey == 'e')
                app.DisplayAlerts = 0;

            foreach (string filePath in files)
            {
                string pdfPath = Path.ChangeExtension(filePath, ".pdf");

                if (appKey == 'p')
                {
                    dynamic presentation = app.Presentations.Open(filePath);
                    presentation.SaveAs(pdfPath, 32);
                    presentation.Close();
                }
                else if (appKey == 'w')
                {
                    dynamic document = app.Documents.Open(filePath);
                    document.SaveAs(pdfPath, 17);
                    document.Close(false);
                }
                else if (appKey == 'e')
                {
                    dynamic workbook = app.Workbooks.Open(filePath);
                    workbook.ExportAsFixedFormat(0, pdfPath);
                    workbook.Close(false);
                }
            }
        }
        catch
        {
            // Best-effort conversion.
        }
        finally
        {
            try
            {
                app?.Quit();
            }
            catch
            {
                // Ignore quit failures.
            }
        }
    }

    #endregion

    #region STA Runner

    private sealed class StaThreadRunner
    {
        private readonly BlockingCollection<WorkItem> _queue = new();
        private readonly Thread _thread;

        public StaThreadRunner()
        {
            _thread = new Thread(ThreadStart) { IsBackground = true, Name = "ExplorerFileActionsStaThread" };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        public void Run(Action action)
        {
            Run<object>(() =>
            {
                action();
                return null!;
            });
        }

        public T Run<T>(Func<T> func)
        {
            var item = new WorkItem(func);
            _queue.Add(item);
            item.Signal.Wait();
            if (item.Exception != null)
                throw item.Exception;
            return (T)item.Result!;
        }

        private void ThreadStart()
        {
            while (true)
            {
                var item = _queue.Take();
                try
                {
                    item.Result = item.Func.DynamicInvoke();
                }
                catch (Exception ex)
                {
                    item.Exception = ex;
                }
                finally
                {
                    item.Signal.Set();
                }
            }
        }

        private sealed class WorkItem
        {
            public WorkItem(Delegate func)
            {
                Func = func;
                Signal = new ManualResetEventSlim(false);
            }

            public Delegate Func { get; }
            public ManualResetEventSlim Signal { get; }
            public object? Result { get; set; }
            public Exception? Exception { get; set; }
        }
    }

    #endregion
}
