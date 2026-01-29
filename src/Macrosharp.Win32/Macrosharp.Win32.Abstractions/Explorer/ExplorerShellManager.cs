using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Abstractions.Explorer;

/// <summary>
/// Provides high-level interaction with Windows File Explorer.
/// Uses Shell COM automation on an STA thread with best-effort tab resolution.
/// </summary>
public static class ExplorerShellManager
{
    private static readonly StaThreadRunner Sta = new();

    /// <summary>Opens the specified folder in File Explorer.</summary>
    public static void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        string fullPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException(fullPath);

        if (!OpenFolderAndSelectItemsNative(fullPath, Array.Empty<string>()))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", fullPath) { UseShellExecute = true });
        }
    }

    /// <summary>
    /// Opens a folder and selects one or more items. If multiple items are provided,
    /// this will open the folder and then attempt to select items in the active view.
    /// </summary>
    public static void OpenAndSelectItems(string folderPath, IReadOnlyList<string> itemPaths)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        string fullFolderPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullFolderPath))
            throw new DirectoryNotFoundException(fullFolderPath);

        if (itemPaths is null || itemPaths.Count == 0)
        {
            OpenFolder(fullFolderPath);
            return;
        }

        if (OpenFolderAndSelectItemsNative(fullFolderPath, itemPaths))
            return;

        string firstItem = Path.GetFullPath(itemPaths[0]);
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{firstItem}\"") { UseShellExecute = true });
    }

    /// <summary>Refreshes the active Explorer view (or the view associated with the provided HWND).</summary>
    public static void Refresh(HWND hwnd = default)
    {
        WithShellOrFallback(
            hwnd,
            ctx =>
            {
                ctx.Document?.Refresh();
                return true;
            },
            () => ExplorerUia.Refresh(hwnd)
        );
    }

    /// <summary>Returns the selected item paths from the active Explorer view (or from the provided HWND).</summary>
    public static IReadOnlyList<string> GetSelectedItems(HWND hwnd = default)
    {
        return WithShellOrFallback(hwnd, ctx => GetSelectedItemsFromShell(ctx, hwnd), () => ExplorerUia.GetSelectedItems(hwnd, folderPath: null));
    }

    /// <summary>Returns the current folder path for the active Explorer view (or from the provided HWND).</summary>
    public static string? GetCurrentFolderPath(HWND hwnd = default)
    {
        return WithShellOrFallback(hwnd, ctx => GetCurrentFolderPathFromShell(ctx), () => ExplorerUia.GetCurrentFolderPath(hwnd));
    }

    /// <summary>Selects items in the active Explorer view (or from the provided HWND).</summary>
    public static bool SelectItems(string folderPath, IReadOnlyList<string> itemPaths, HWND hwnd = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        if (itemPaths is null || itemPaths.Count == 0)
            return false;

        string fullFolderPath = Path.GetFullPath(folderPath);
        var filteredItems = FilterItemPathsInFolder(fullFolderPath, itemPaths);
        if (filteredItems.Count == 0)
            return false;

        return WithShellOrFallback(hwnd, ctx => SelectItemsFromShell(ctx, fullFolderPath, filteredItems), () => ExplorerUia.SelectItems(hwnd, filteredItems));
    }

    /// <summary>Executes a context menu action (verb) on items in the active Explorer view (or from the provided HWND).</summary>
    public static bool ExecuteContextMenuAction(string folderPath, IReadOnlyList<string> itemPaths, string verb, HWND hwnd = default, ContextMenuInvokeMode mode = ContextMenuInvokeMode.MultiSelect)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        if (string.IsNullOrWhiteSpace(verb))
            throw new ArgumentException("Verb is required.", nameof(verb));

        if (itemPaths is null || itemPaths.Count == 0)
            return false;

        string fullFolderPath = Path.GetFullPath(folderPath);
        var filteredItems = FilterItemPathsInFolder(fullFolderPath, itemPaths);
        if (filteredItems.Count == 0)
            return false;

        return Sta.Run(() => ExecuteContextMenuActionInternal(fullFolderPath, filteredItems, verb, hwnd, mode));
    }

    #region Private Methods
    /// <summary>Opens a folder and selects items using native Shell APIs.</summary>
    private static bool OpenFolderAndSelectItemsNative(string folderPath, IReadOnlyList<string> itemPaths)
    {
        IntPtr folderPidl = IntPtr.Zero;
        List<IntPtr> itemPidls = new();

        try
        {
            uint attrs;
            int hr = SHParseDisplayName(folderPath, IntPtr.Zero, out folderPidl, 0, out attrs);
            if (hr != 0 || folderPidl == IntPtr.Zero)
                return false;

            var filteredItems = FilterItemPathsInFolder(Path.GetFullPath(folderPath), itemPaths);
            foreach (string fullItemPath in filteredItems)
            {
                hr = SHParseDisplayName(fullItemPath, IntPtr.Zero, out IntPtr itemPidl, 0, out attrs);
                if (hr != 0 || itemPidl == IntPtr.Zero)
                    continue;

                itemPidls.Add(itemPidl);
            }

            IntPtr[]? childPidls = null;
            if (itemPidls.Count > 0)
            {
                var relativePidls = new List<IntPtr>();
                foreach (IntPtr itemPidl in itemPidls)
                {
                    IntPtr relative = ILFindChild(folderPidl, itemPidl);
                    if (relative != IntPtr.Zero)
                        relativePidls.Add(relative);
                }

                if (relativePidls.Count > 0)
                    childPidls = relativePidls.ToArray();
            }

            uint count = (uint)(childPidls?.Length ?? 0);
            return SHOpenFolderAndSelectItems(folderPidl, count, childPidls, 0) == 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            foreach (IntPtr pidl in itemPidls)
            {
                if (pidl != IntPtr.Zero)
                    CoTaskMemFree(pidl);
            }

            if (folderPidl != IntPtr.Zero)
                CoTaskMemFree(folderPidl);
        }
    }

    /// <summary>Executes with Shell context if available, otherwise falls back.</summary>
    private static T WithShellOrFallback<T>(HWND hwnd, Func<ShellContext, T> withShell, Func<T> fallback)
    {
        return Sta.Run(() =>
        {
            if (TryGetShellContext(hwnd, out var context))
                return withShell(context);

            return fallback();
        });
    }

    /// <summary>Gets selected items from Shell context.</summary>
    private static IReadOnlyList<string> GetSelectedItemsFromShell(ShellContext context, HWND hwnd)
    {
        if (context.Document == null)
            return ExplorerUia.GetSelectedItems(hwnd, context.FolderPath);

        var selected = new List<string>();
        // Use foreach to iterate via IEnumVARIANT, which preserves selection order
        // (unlike indexed access via .Item(i) which returns display order).
        foreach (dynamic item in context.Document.SelectedItems())
        {
            string? path = item?.Path as string;
            if (!string.IsNullOrWhiteSpace(path))
                selected.Add(path);
        }

        return selected;
    }

    /// <summary>Gets current folder path from Shell context.</summary>
    private static string? GetCurrentFolderPathFromShell(ShellContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.FolderPath))
            return context.FolderPath;

        try
        {
            string? locationUrl = context.Window.LocationURL as string;
            if (!string.IsNullOrWhiteSpace(locationUrl))
            {
                var uri = new Uri(locationUrl);
                if (uri.IsFile)
                    return uri.LocalPath;
            }
        }
        catch
        {
            // Ignore parse failures.
        }

        return null;
    }

    /// <summary>Selects items in Shell context.</summary>
    private static bool SelectItemsFromShell(ShellContext context, string fullFolderPath, IReadOnlyList<string> itemPaths, bool allowFolderMismatch = false)
    {
        if (!allowFolderMismatch && !string.IsNullOrWhiteSpace(context.FolderPath) && !Path.Equals(Path.GetFullPath(context.FolderPath), fullFolderPath))
        {
            return false;
        }

        try
        {
            dynamic? folder = context.Document?.Folder;
            if (folder == null)
                return false;

            bool first = true;
            foreach (string fullItemPath in itemPaths)
            {
                string name = Path.GetFileName(fullItemPath);
                dynamic? item = folder.ParseName(name);
                if (item == null)
                    continue;

                int flags = first
                    ? (int)ShellViewSelectItemFlags.SVSI_SELECT | (int)ShellViewSelectItemFlags.SVSI_DESELECTOTHERS | (int)ShellViewSelectItemFlags.SVSI_FOCUSED | (int)ShellViewSelectItemFlags.SVSI_ENSUREVISIBLE
                    : (int)ShellViewSelectItemFlags.SVSI_SELECT;
                context.Document?.SelectItem(item, flags);
                first = false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Attempts to get the Shell context for the specified HWND.</summary>
    private static bool TryGetShellContext(HWND hwnd, out ShellContext context)
    {
        context = null!;
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
                {
                    bestMatch ??= window;
                }
            }

            if (bestMatch == null)
                return false;

            dynamic? doc = null;
            string? folderPath = null;
            try
            {
                doc = bestMatch.Document;
                dynamic? folder = doc?.Folder;
                string? path = folder?.Self?.Path as string;
                if (!string.IsNullOrWhiteSpace(path))
                    folderPath = path;
            }
            catch
            {
                // Ignore failures.
            }

            context = new ShellContext(bestMatch, doc, folderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Filters item paths to those located within the specified folder.</summary>
    private static List<string> FilterItemPathsInFolder(string fullFolderPath, IReadOnlyList<string> itemPaths)
    {
        var filtered = new List<string>();
        if (itemPaths == null || itemPaths.Count == 0)
            return filtered;

        foreach (string itemPath in itemPaths)
        {
            if (string.IsNullOrWhiteSpace(itemPath))
                continue;

            string fullItemPath = Path.GetFullPath(itemPath);
            if (!Path.Equals(Path.GetDirectoryName(fullItemPath) ?? string.Empty, fullFolderPath))
                continue;

            filtered.Add(fullItemPath);
        }

        return filtered;
    }

    /// <summary>Executes a context menu action (verb) with explicit mode handling.</summary>
    private static bool ExecuteContextMenuActionInternal(string fullFolderPath, IReadOnlyList<string> itemPaths, string verb, HWND hwnd, ContextMenuInvokeMode mode)
    {
        if (mode == ContextMenuInvokeMode.PerItem)
            return ExecuteContextMenuActionPerItem(fullFolderPath, itemPaths, verb, hwnd);

        // For "properties" verb, select items and then simulate Alt+Enter which is the universal shortcut.
        string normalizedVerb = verb.Replace("&", "").ToLowerInvariant();
        if (normalizedVerb == "properties" && itemPaths.Count > 0)
        {
            // First select all items in the Explorer window.
            if (TryGetShellContext(hwnd, out var ctx))
            {
                bool selected = SelectItemsFromShell(ctx, fullFolderPath, itemPaths, allowFolderMismatch: true);
                if (selected)
                {
                    // Give Explorer a moment to update selection, then send Alt+Enter.
                    Thread.Sleep(100);
                    SendAltEnter(hwnd);
                    return true;
                }
            }
        }

        if (TryGetShellContext(hwnd, out var context) && ExecuteContextMenuActionMultiSelectFromShell(context, fullFolderPath, itemPaths, verb))
            return true;

        return false;
    }

    /// <summary>Sends Alt+Enter keystroke to invoke Properties on selected items.</summary>
    private static void SendAltEnter(HWND hwnd)
    {
        // Get the target window - use foreground if hwnd is null.
        HWND target = hwnd == HWND.Null ? PInvoke.GetForegroundWindow() : hwnd;

        // Bring the window to foreground if needed.
        if (target != HWND.Null && target != PInvoke.GetForegroundWindow())
        {
            PInvoke.SetForegroundWindow(target);
            Thread.Sleep(50);
        }

        // Simulate Alt+Enter using SendInput.
        var inputs = new INPUT[4];

        // Alt down
        inputs[0].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[0].Anonymous.ki.wVk = VIRTUAL_KEY.VK_MENU;

        // Enter down
        inputs[1].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[1].Anonymous.ki.wVk = VIRTUAL_KEY.VK_RETURN;

        // Enter up
        inputs[2].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[2].Anonymous.ki.wVk = VIRTUAL_KEY.VK_RETURN;
        inputs[2].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        // Alt up
        inputs[3].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[3].Anonymous.ki.wVk = VIRTUAL_KEY.VK_MENU;
        inputs[3].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        PInvoke.SendInput(inputs.AsSpan(), Marshal.SizeOf<INPUT>());
    }

    /// <summary>Executes a context menu action (verb) on selected items in Shell context.</summary>
    private static bool ExecuteContextMenuActionMultiSelectFromShell(ShellContext context, string fullFolderPath, IReadOnlyList<string> itemPaths, string verb)
    {
        bool selected = SelectItemsFromShell(context, fullFolderPath, itemPaths, allowFolderMismatch: true);
        if (!selected)
            return false;

        try
        {
            dynamic? selection = context.Document?.SelectedItems();
            if (selection == null || selection?.Count == 0)
                return false;

            // Normalize the requested verb for comparison.
            string normalizedVerb = verb.Replace("&", "").ToLowerInvariant();

            // Get the first selected item to find available verbs and their display names.
            dynamic? firstItem = selection?.Item(0);
            if (firstItem == null)
                return false;

            dynamic? verbs = firstItem.Verbs();
            if (verbs == null || verbs?.Count == 0)
                return false;

            // Find the matching verb name to use with InvokeVerbEx.
            // We need the exact display name (with &) for InvokeVerbEx to work on the collection.
            string? matchingVerbName = null;
            for (int i = 0; i < verbs?.Count; i++)
            {
                dynamic verbItem = verbs.Item(i);
                string? verbName = verbItem.Name as string;
                if (string.IsNullOrWhiteSpace(verbName))
                    continue;

                string normalizedVerbName = verbName.Replace("&", "").ToLowerInvariant();
                if (string.Equals(normalizedVerbName, normalizedVerb, StringComparison.OrdinalIgnoreCase))
                {
                    matchingVerbName = verbName;
                    break;
                }
            }

            if (matchingVerbName == null)
                return false;

            // Try canonical verb first (lowercased, no ampersands), then display name.
            try
            {
                if (selection is null)
                    return false;

                selection.InvokeVerbEx(normalizedVerb);
                return true;
            }
            catch
            {
                // Fall back to display name with ampersands.
            }

            if (selection is null)
                return false;

            selection.InvokeVerbEx(matchingVerbName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Executes a context menu action (verb) per item using a Shell folder.</summary>
    private static bool ExecuteContextMenuActionPerItem(string fullFolderPath, IReadOnlyList<string> itemPaths, string verb, HWND hwnd)
    {
        dynamic? folder = null;
        if (TryGetShellContext(hwnd, out var context))
            folder = context.Document?.Folder;

        if (folder != null)
            return ExecuteContextMenuActionOnFolder(folder, itemPaths, verb);

        return ExecuteContextMenuActionFromFolderPath(fullFolderPath, itemPaths, verb);
    }

    /// <summary>Executes a context menu action (verb) on items via Shell namespace.</summary>
    private static bool ExecuteContextMenuActionFromFolderPath(string fullFolderPath, IReadOnlyList<string> itemPaths, string verb)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
                return false;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic? folder = shell.NameSpace(fullFolderPath);
            if (folder == null)
                return false;

            return ExecuteContextMenuActionOnFolder(folder, itemPaths, verb);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Executes a context menu verb for each item in a Shell folder.</summary>
    private static bool ExecuteContextMenuActionOnFolder(dynamic folder, IReadOnlyList<string> itemPaths, string verb)
    {
        bool anyInvoked = false;
        foreach (string fullItemPath in itemPaths)
        {
            string name = Path.GetFileName(fullItemPath);
            dynamic? item = folder.ParseName(name);
            if (item == null)
                continue;

            try
            {
                item.InvokeVerb(verb);
                anyInvoked = true;
            }
            catch
            {
                // Best-effort per item.
            }
        }

        return anyInvoked;
    }

    #endregion

    #region PInvoke Declarations

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[]? apidl, uint dwFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

    [DllImport("shell32.dll")]
    private static extern IntPtr ILFindChild(IntPtr pidlParent, IntPtr pidlChild);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pv);

    #endregion

    #region Enums and Helper Classes

    /// <summary>Context menu invocation modes.</summary>
    public enum ContextMenuInvokeMode
    {
        MultiSelect,
        PerItem,
    }

    /// <summary>Flags for selecting items in Shell views.</summary>
    private enum ShellViewSelectItemFlags
    {
        SVSI_SELECT = 0x00000001,
        SVSI_DESELECTOTHERS = 0x00000004,
        SVSI_FOCUSED = 0x00000010,
        SVSI_ENSUREVISIBLE = 0x00000008,
    }

    /// <summary>Represents the Shell context for an Explorer window.</summary>
    private sealed class ShellContext
    {
        public ShellContext(dynamic window, dynamic? document, string? folderPath)
        {
            Window = window;
            Document = document;
            FolderPath = folderPath;
        }

        public dynamic Window { get; }
        public dynamic? Document { get; }
        public string? FolderPath { get; }
    }

    /// <summary>Runs actions on a dedicated STA thread.</summary>
    private sealed class StaThreadRunner
    {
        private readonly BlockingCollection<WorkItem> _queue = new();
        private readonly Thread _thread;

        public StaThreadRunner()
        {
            _thread = new Thread(ThreadStart) { IsBackground = true, Name = "ExplorerStaThread" };
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

    /// <summary>Provides UI Automation interactions with Explorer windows.</summary>
    private static class ExplorerUia
    {
        private static readonly Guid ClsidCuiAutomation = new("FF48DBA4-60EF-4201-AA87-54103EEF594E");

        public static bool Refresh(HWND hwnd)
        {
            return WithRoot(
                hwnd,
                (automation, root) =>
                {
                    var refreshButton = FindFirstByNameAndControlType(automation, root, "Refresh", UIA_ControlTypeButtonId) ?? FindFirstByNameAndControlType(automation, root, "Reload", UIA_ControlTypeButtonId);
                    if (refreshButton == null)
                        return false;

                    var invokePattern = refreshButton.GetCurrentPattern(UIA_InvokePatternId) as IUIAutomationInvokePattern;
                    invokePattern?.Invoke();
                    return invokePattern != null;
                },
                fallback: false
            );
        }

        public static IReadOnlyList<string> GetSelectedItems(HWND hwnd, string? folderPath)
        {
            return WithSelectionContainer<IReadOnlyList<string>>(
                hwnd,
                (automation, container) =>
                {
                    var selectionPattern = container.GetCurrentPattern(UIA_SelectionPatternId) as IUIAutomationSelectionPattern;
                    if (selectionPattern == null)
                        return Array.Empty<string>();

                    var selection = selectionPattern.GetCurrentSelection();
                    if (selection == null || selection.Length == 0)
                        return Array.Empty<string>();

                    string? baseFolder = folderPath;
                    if (string.IsNullOrWhiteSpace(baseFolder))
                        baseFolder = GetCurrentFolderPath(hwnd);

                    var results = new List<string>();
                    for (int i = 0; i < selection.Length; i++)
                    {
                        var element = selection.GetElement(i);
                        if (element == null)
                            continue;

                        string name = element.CurrentName ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        results.Add(!string.IsNullOrWhiteSpace(baseFolder) ? Path.Combine(baseFolder, name) : name);
                    }

                    return results;
                },
                fallback: Array.Empty<string>()
            );
        }

        public static string? GetCurrentFolderPath(HWND hwnd)
        {
            return WithRoot(
                hwnd,
                (automation, root) =>
                {
                    var editCondition = automation.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ControlTypeEditId);
                    var edits = root.FindAll(TreeScope.Subtree, editCondition);
                    if (edits == null)
                        return null;

                    for (int i = 0; i < edits.Length; i++)
                    {
                        var edit = edits.GetElement(i);
                        if (edit == null)
                            continue;

                        var valuePattern = edit.GetCurrentPattern(UIA_ValuePatternId) as IUIAutomationValuePattern;
                        string? value = valuePattern?.CurrentValue;
                        if (string.IsNullOrWhiteSpace(value))
                            continue;

                        if (LooksLikeFolderPath(value))
                            return value;
                    }

                    return null;
                },
                fallback: null
            );
        }

        public static bool SelectItems(HWND hwnd, IReadOnlyList<string> itemPaths)
        {
            return WithSelectionContainer(
                hwnd,
                (automation, container) =>
                {
                    bool anySelected = false;
                    foreach (string fullItemPath in itemPaths)
                    {
                        string name = Path.GetFileName(fullItemPath);
                        var nameCondition = automation.CreatePropertyCondition(UIA_NamePropertyId, name);
                        var itemElement = container.FindFirst(TreeScope.Subtree, nameCondition);
                        if (itemElement == null)
                            continue;

                        var selectPattern = itemElement.GetCurrentPattern(UIA_SelectionItemPatternId) as IUIAutomationSelectionItemPattern;
                        selectPattern?.Select();
                        if (selectPattern != null)
                            anySelected = true;
                    }

                    return anySelected;
                },
                fallback: false
            );
        }

        private static T WithSelectionContainer<T>(HWND hwnd, Func<IUIAutomation, IUIAutomationElement, T> action, T fallback)
        {
            return WithRoot(
                hwnd,
                (automation, root) =>
                {
                    var container = FindSelectionContainer(automation, root);
                    if (container == null)
                        return fallback;

                    return action(automation, container);
                },
                fallback
            );
        }

        private static T WithRoot<T>(HWND hwnd, Func<IUIAutomation, IUIAutomationElement, T> action, T fallback)
        {
            try
            {
                var automation = CreateAutomation();
                if (automation == null)
                    return fallback;

                var root = TryGetRootElement(automation, hwnd);
                if (root == null)
                    return fallback;

                return action(automation, root);
            }
            catch
            {
                return fallback;
            }
        }

        private static IUIAutomation? CreateAutomation()
        {
            Type? automationType = Type.GetTypeFromCLSID(ClsidCuiAutomation);
            return automationType == null ? null : (IUIAutomation)Activator.CreateInstance(automationType)!;
        }

        private static IUIAutomationElement? TryGetRootElement(IUIAutomation automation, HWND hwnd)
        {
            HWND target = hwnd == HWND.Null ? PInvoke.GetForegroundWindow() : hwnd;
            if (target == HWND.Null)
                return null;

            return automation.ElementFromHandle(target);
        }

        private static IUIAutomationElement? FindSelectionContainer(IUIAutomation automation, IUIAutomationElement root)
        {
            var listCondition = automation.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ControlTypeListId);
            var listElement = root.FindFirst(TreeScope.Subtree, listCondition);
            if (listElement != null)
                return listElement;

            var gridCondition = automation.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ControlTypeDataGridId);
            return root.FindFirst(TreeScope.Subtree, gridCondition);
        }

        private static IUIAutomationElement? FindFirstByNameAndControlType(IUIAutomation automation, IUIAutomationElement root, string name, int controlTypeId)
        {
            var nameCondition = automation.CreatePropertyCondition(UIA_NamePropertyId, name);
            var typeCondition = automation.CreatePropertyCondition(UIA_ControlTypePropertyId, controlTypeId);
            var combined = automation.CreateAndCondition(nameCondition, typeCondition);
            return root.FindFirst(TreeScope.Subtree, combined);
        }

        private static bool LooksLikeFolderPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.StartsWith("\\\\", StringComparison.Ordinal))
                return true;

            if (value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' && (value[2] == '\\' || value[2] == '/'))
                return true;

            return Directory.Exists(value);
        }

        private const int UIA_ControlTypePropertyId = 30003;
        private const int UIA_NamePropertyId = 30005;
        private const int UIA_ValuePatternId = 10002;
        private const int UIA_SelectionPatternId = 10001;
        private const int UIA_SelectionItemPatternId = 10010;
        private const int UIA_InvokePatternId = 10000;

        private const int UIA_ControlTypeListId = 50008;
        private const int UIA_ControlTypeDataGridId = 50028;
        private const int UIA_ControlTypeEditId = 50004;
        private const int UIA_ControlTypeButtonId = 50000;

        [ComImport]
        [Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomation
        {
            IUIAutomationElement ElementFromHandle(nint hwnd);
            IUIAutomationCondition CreatePropertyCondition(int propertyId, object value);
            IUIAutomationCondition CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);
        }

        [ComImport]
        [Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationElement
        {
            object GetCurrentPattern(int patternId);
            IUIAutomationElement FindFirst(TreeScope scope, IUIAutomationCondition condition);
            IUIAutomationElementArray FindAll(TreeScope scope, IUIAutomationCondition condition);
            string CurrentName { get; }
        }

        [ComImport]
        [Guid("5ED5202E-B2AC-47A6-B638-4B0BF140D78E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationSelectionPattern
        {
            IUIAutomationElementArray GetCurrentSelection();
        }

        [ComImport]
        [Guid("A8EFA66A-0FDA-421A-9194-38021F3578EA")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationSelectionItemPattern
        {
            void Select();
        }

        [ComImport]
        [Guid("A94CD8B1-0844-4CD6-9D2D-640537AB39E9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationValuePattern
        {
            string CurrentValue { get; }
        }

        [ComImport]
        [Guid("FB377FBE-8EA6-46D5-9C73-6499642D3059")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationInvokePattern
        {
            void Invoke();
        }

        [ComImport]
        [Guid("14314595-B4BC-4055-95F2-58F2E42C9855")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationElementArray
        {
            int Length { get; }
            IUIAutomationElement GetElement(int index);
        }

        [ComImport]
        [Guid("352FFBA8-0973-437C-A61F-F64CAFD81DF1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationCondition { }

        private enum TreeScope
        {
            Element = 1,
            Children = 2,
            Descendants = 4,
            Subtree = 7,
        }
    }
    #endregion
}
