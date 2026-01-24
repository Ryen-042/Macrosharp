using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.TrayIcon;

public sealed class TrayIconHost : IDisposable
{
    private const int MenuIdBase = 1023;
    private const int SingleClickDelayMs = 250;
    private const int SingleClickSuppressionMs = 300;
    private const int NotifyIconTipMaxLength = 128;
    private const int MaxAddRetryCount = 3;
    private const int MenuOffsetX = 10;
    private const int MenuOffsetY = 10;

    private readonly IReadOnlyList<TrayMenuItem> menuItems;
    private readonly int defaultClickIndex;
    private readonly int defaultDoubleClickIndex;
    private readonly string className;

    private readonly object lifecycleLock = new();

    private string tooltip;
    private string? iconPath;

    private Thread? trayThread;
    private HWND hwnd;
    private WNDPROC? wndProc;
    private uint trayCallbackMessage;
    private uint taskbarCreatedMessage;
    private Guid trayGuid;
    private HICON currentIcon;
    private NOTIFYICONDATAW notifyIconData;
    private bool isRunning;
    private bool isStopping;

    private Timer? singleClickTimer;
    private long suppressSingleClickUntilTicks;

    private Action? defaultClickAction;
    private Action? defaultDoubleClickAction;

    private readonly Dictionary<uint, MenuAction> menuActions = new();
    private int nextMenuId;

    private ManualResetEventSlim? readySignal;

    public TrayIconHost(string tooltip, string? iconPath, IReadOnlyList<TrayMenuItem> menuItems, int defaultClickIndex = -1, int defaultDoubleClickIndex = -1)
    {
        this.tooltip = tooltip;
        this.iconPath = iconPath;
        this.menuItems = menuItems;
        this.defaultClickIndex = defaultClickIndex;
        this.defaultDoubleClickIndex = defaultDoubleClickIndex;
        className = $"Macrosharp.TrayIcon.{Guid.NewGuid():N}";
    }

    public bool Start()
    {
        lock (lifecycleLock)
        {
            if (isRunning)
            {
                return true;
            }

            // Windows services run in session 0 and cannot display UI such as tray icons. If you
            // need tray functionality while running as a service, create a tray companion app
            // and communicate with the service using IPC (e.g., named pipes).
            if (!Environment.UserInteractive)
            {
                Console.WriteLine("Tray icon creation skipped: process is not running in an interactive user session.");
                return false;
            }

            readySignal = new ManualResetEventSlim(false);
            trayThread = new Thread(TrayThreadProc) { IsBackground = true, Name = "Macrosharp.TrayIcon" };
            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.Start();
            readySignal.Wait();

            return isRunning;
        }
    }

    public void Stop()
    {
        lock (lifecycleLock)
        {
            if (!isRunning || isStopping)
            {
                return;
            }

            isStopping = true;

            if (hwnd != HWND.Null)
            {
                PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
            }
        }

        trayThread?.Join();
    }

    public void UpdateTooltip(string newTooltip)
    {
        tooltip = newTooltip;
        if (!isRunning)
        {
            return;
        }

        SetNotifyIconTooltip(ref notifyIconData, tooltip);
        TryModifyTrayIcon();
    }

    public void UpdateIcon(string newIconPath)
    {
        iconPath = newIconPath;
        if (!isRunning)
        {
            return;
        }

        UpdateNotifyIconImage();
        TryModifyTrayIcon();
    }

    public void Dispose()
    {
        Stop();
    }

    private void TrayThreadProc()
    {
        try
        {
            trayCallbackMessage = PInvoke.WM_USER + 1;
            taskbarCreatedMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");
            trayGuid = Guid.NewGuid();

            ResolveDefaultActions();

            CreateHiddenWindow();
            InitializeNotifyIconData();

            if (!TryAddTrayIcon())
            {
                Console.WriteLine("Tray icon could not be created after retries.");
            }

            readySignal?.Set();
            isRunning = true;

            MSG msg = new();
            while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tray icon thread failed: {ex}");
        }
        finally
        {
            CleanupTrayIcon();
            DestroyWindow();
            readySignal?.Set();

            isRunning = false;
            isStopping = false;
        }
    }

    private unsafe void CreateHiddenWindow()
    {
        wndProc = new WNDPROC(WndProc);

        unsafe
        {
            fixed (char* classNamePtr = className)
            {
                var wndClass = new WNDCLASSW
                {
                    style = 0,
                    lpfnWndProc = wndProc,
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION),
                    hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                    hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH).Value),
                    lpszClassName = new PCWSTR(classNamePtr),
                };

                PInvoke.RegisterClass(wndClass);
            }
        }

        hwnd = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, className, string.Empty, WINDOW_STYLE.WS_OVERLAPPEDWINDOW, 0, 0, 0, 0, HWND.Null, default, PInvoke.GetModuleHandle((string?)null), null);
    }

    private void DestroyWindow()
    {
        if (hwnd != HWND.Null)
        {
            PInvoke.DestroyWindow(hwnd);
            hwnd = HWND.Null;
        }

        PInvoke.UnregisterClass(className, default);
    }

    private unsafe void InitializeNotifyIconData()
    {
        currentIcon = LoadTrayIcon();
        notifyIconData = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = hwnd,
            uID = 1,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID,
            uCallbackMessage = trayCallbackMessage,
            hIcon = currentIcon,
            guidItem = trayGuid,
        };

        SetNotifyIconTooltip(ref notifyIconData, tooltip);
    }

    private bool TryAddTrayIcon()
    {
        for (int attempt = 0; attempt < MaxAddRetryCount; attempt++)
        {
            if (PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in notifyIconData))
            {
                return true;
            }

            Console.WriteLine("Tray icon add failed, retrying...");
            Thread.Sleep(100);
            trayGuid = Guid.NewGuid();
            notifyIconData.guidItem = trayGuid;
        }

        return false;
    }

    private void TryModifyTrayIcon()
    {
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in notifyIconData))
        {
            Console.WriteLine("Tray icon modify failed, attempting re-add.");
            TryAddTrayIcon();
        }
    }

    /// <summary>
    /// Removes the tray icon from the system taskbar and releases associated resources.
    /// </summary>
    /// <remarks>
    /// This method performs two operations:
    /// <list type="bullet">
    /// <item><description>Removes the notification icon from the system using Shell_NotifyIcon with NIM_DELETE message</description></item>
    /// <item><description>Destroys the icon handle and sets it to null to prevent resource leaks</description></item>
    /// </list>
    /// This method is safe to call multiple times as it checks for valid handles before attempting deletion.
    /// </remarks>
    private void CleanupTrayIcon()
    {
        if (notifyIconData.hWnd != HWND.Null)
        {
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in notifyIconData);
        }

        if (currentIcon != HICON.Null)
        {
            PInvoke.DestroyIcon(currentIcon);
            currentIcon = HICON.Null;
        }
    }

    private unsafe void UpdateNotifyIconImage()
    {
        if (currentIcon != HICON.Null)
        {
            PInvoke.DestroyIcon(currentIcon);
        }

        currentIcon = LoadTrayIcon();
        notifyIconData.hIcon = currentIcon;
        notifyIconData.uFlags |= NOTIFY_ICON_DATA_FLAGS.NIF_ICON;
    }

    private HICON LoadTrayIcon()
    {
        int iconWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON);
        int iconHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON);

        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            using var loaded = PInvoke.LoadImage(null, iconPath, GDI_IMAGE_TYPE.IMAGE_ICON, iconWidth, iconHeight, IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);
            if (loaded is not null && !loaded.IsInvalid)
            {
                IntPtr handle = loaded.DangerousGetHandle();
                loaded.SetHandleAsInvalid();
                return new HICON(handle);
            }

            Console.WriteLine($"Failed to load icon from '{iconPath}', falling back to default.");
        }

        return PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
    }

    private unsafe LRESULT WndProc(HWND windowHandle, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message == trayCallbackMessage)
        {
            uint rawMessage = (uint)lParam.Value.ToInt64();
            uint lowWord = rawMessage & 0xFFFF;
            uint eventMessage = rawMessage;

            if (rawMessage != PInvoke.NIN_SELECT && rawMessage != (PInvoke.WM_USER + 1) && rawMessage != (PInvoke.WM_USER + 7) && rawMessage != PInvoke.WM_CONTEXTMENU)
            {
                eventMessage = lowWord;
            }

            // Ignore mouse move and hover messages
            if (eventMessage == PInvoke.WM_MOUSEMOVE || eventMessage == (PInvoke.WM_USER + 7) || eventMessage == 0x406)
            {
                return (LRESULT)0;
            }

            Console.WriteLine($"Tray callback message: 0x{eventMessage:X}");
            switch (eventMessage)
            {
                case PInvoke.NIN_SELECT:
                case PInvoke.WM_LBUTTONUP:
                    Console.WriteLine("Tray single-click received.");
                    HandleSingleClick();
                    return (LRESULT)0;
                case PInvoke.WM_LBUTTONDBLCLK:
                    Console.WriteLine("Tray double-click received.");
                    HandleDoubleClick();
                    return (LRESULT)0;
                case PInvoke.WM_USER + 1:
                case PInvoke.WM_CONTEXTMENU:
                    Console.WriteLine("Tray context menu request received (keyboard).");
                    ShowContextMenu(useTrayAnchor: true, lParam);
                    return (LRESULT)0;
                //case TrayNative.NIN_CONTEXTMENU: // To make mouse hover trigger context menu
                case PInvoke.WM_RBUTTONUP:
                    Console.WriteLine("Tray context menu request received.");
                    ShowContextMenu(useTrayAnchor: true, lParam);
                    return (LRESULT)0;
            }
        }

        if (message == taskbarCreatedMessage)
        {
            Console.WriteLine("Explorer restarted; restoring tray icon.");
            TryAddTrayIcon();
            return (LRESULT)0;
        }

        switch (message)
        {
            case PInvoke.WM_COMMAND:
                uint commandId = (uint)LOWORD(wParam);
                ExecuteMenuCommand(commandId);
                return (LRESULT)0;
            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                return (LRESULT)0;
            case PInvoke.WM_CLOSE:
                CleanupTrayIcon();
                PInvoke.DestroyWindow(hwnd);
                return (LRESULT)0;
        }

        return PInvoke.DefWindowProc(windowHandle, message, wParam, lParam);
    }

    private void HandleSingleClick()
    {
        if (defaultClickAction is null)
        {
            return;
        }

        long now = Environment.TickCount64;
        if (now < suppressSingleClickUntilTicks)
        {
            return;
        }

        singleClickTimer?.Dispose();
        singleClickTimer = new Timer(
            _ =>
            {
                ExecuteAction(defaultClickAction);
            },
            null,
            SingleClickDelayMs,
            Timeout.Infinite
        );
    }

    private void HandleDoubleClick()
    {
        if (defaultDoubleClickAction is null)
        {
            return;
        }

        suppressSingleClickUntilTicks = Environment.TickCount64 + SingleClickSuppressionMs;
        singleClickTimer?.Dispose();
        ExecuteAction(defaultDoubleClickAction);
    }

    private unsafe void ShowContextMenu(bool useTrayAnchor, LPARAM lParam)
    {
        using var menu = BuildMenu();

        if (menu.MenuHandle == HMENU.Null)
        {
            Console.WriteLine("Tray context menu handle is null.");
            return;
        }

        int menuCount = PInvoke.GetMenuItemCount(menu.MenuHandle);
        Console.WriteLine($"Tray context menu item count: {menuCount}");

        Point cursorPos;
        if (useTrayAnchor && TryGetTrayAnchor(out cursorPos))
        {
            // Use tray icon position.
        }
        else if (!PInvoke.GetCursorPos(out cursorPos))
        {
            Console.WriteLine("Tray context menu failed: GetCursorPos returned false.");
            return;
        }

        cursorPos.X += MenuOffsetX;
        cursorPos.Y -= MenuOffsetY;

        PInvoke.SetForegroundWindow(hwnd);
        PInvoke.SetLastError(0);
        using var safeMenuHandle = new SafeMenuHandle(menu.MenuHandle);
        BOOL commandResult = PInvoke.TrackPopupMenuEx(safeMenuHandle, (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD), cursorPos.X, cursorPos.Y, hwnd, null);
        uint commandId = unchecked((uint)commandResult.Value);
        if (commandId != 0)
        {
            ExecuteMenuCommand(commandId);
        }
        else
        {
            int error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                Console.WriteLine($"Tray context menu selection returned 0. LastError={error}.");
            }
        }
        PInvoke.PostMessage(hwnd, PInvoke.WM_NULL, 0, 0);
    }

    private unsafe bool TryGetTrayAnchor(out Point point)
    {
        point = default;

        var identifier = new NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
            hWnd = hwnd,
            uID = notifyIconData.uID,
            guidItem = notifyIconData.guidItem,
        };

        var rect = new RECT();
        HRESULT result = PInvoke.Shell_NotifyIconGetRect(in identifier, out rect);
        if (result.Succeeded)
        {
            point.X = rect.left;
            point.Y = rect.bottom;
            return true;
        }

        return false;
    }

    private MenuBuildResult BuildMenu()
    {
        menuActions.Clear();
        nextMenuId = MenuIdBase;
        defaultClickAction = null;
        defaultDoubleClickAction = null;

        var menuHandle = PInvoke.CreatePopupMenu();
        var bitmaps = new List<HBITMAP>();

        int clickableIndex = 0;
        for (int i = 0; i < menuItems.Count; i++)
        {
            AppendMenuItem(menuHandle, menuItems[i], bitmaps, ref clickableIndex);
        }

        if (menuItems.Count > 0)
        {
            AppendMenuSeparator(menuHandle);
        }

        AppendMenuItem(menuHandle, TrayMenuItem.ActionItem("Quit", Stop), bitmaps, ref clickableIndex);

        return new MenuBuildResult(menuHandle, bitmaps);
    }

    private void AppendMenuItem(HMENU menuHandle, TrayMenuItem item, List<HBITMAP> bitmaps, ref int clickableIndex)
    {
        if (item.IsSeparator)
        {
            AppendMenuSeparator(menuHandle);
            return;
        }

        bool hasChildren = item.Children is { Count: > 0 };
        uint menuId = hasChildren ? 0u : (uint)nextMenuId++;

        if (!hasChildren && item.Action is not null)
        {
            menuActions[menuId] = new MenuAction(item.Action);
            if (clickableIndex == defaultClickIndex)
            {
                defaultClickAction = item.Action;
            }

            if (clickableIndex == defaultDoubleClickIndex)
            {
                defaultDoubleClickAction = item.Action;
            }

            clickableIndex++;
        }

        HMENU? subMenu = null;
        if (hasChildren)
        {
            subMenu = PInvoke.CreatePopupMenu();
            foreach (var child in item.Children!)
            {
                AppendMenuItem(subMenu.Value, child, bitmaps, ref clickableIndex);
            }
        }

        InsertMenuItem(menuHandle, item, menuId, subMenu, bitmaps);
    }

    private void AppendMenuSeparator(HMENU menuHandle)
    {
        var menuItemInfo = new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
            fMask = MENU_ITEM_MASK.MIIM_FTYPE,
            fType = MENU_ITEM_TYPE.MFT_SEPARATOR,
        };

        unsafe
        {
            using var safeMenuHandle = new SafeMenuHandle(menuHandle);
            PInvoke.InsertMenuItem(safeMenuHandle, uint.MaxValue, true, in menuItemInfo);
        }
    }

    private void InsertMenuItem(HMENU menuHandle, TrayMenuItem item, uint menuId, HMENU? subMenu, List<HBITMAP> bitmaps)
    {
        unsafe
        {
            fixed (char* textPtr = item.Text)
            {
                MENU_ITEM_MASK mask = MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_FTYPE;
                if (menuId != 0)
                {
                    mask |= MENU_ITEM_MASK.MIIM_ID;
                }

                if (subMenu.HasValue)
                {
                    mask |= MENU_ITEM_MASK.MIIM_SUBMENU;
                }

                HBITMAP menuBitmap = HBITMAP.Null;
                if (!string.IsNullOrWhiteSpace(item.IconPath) && File.Exists(item.IconPath))
                {
                    menuBitmap = CreateMenuBitmap(item.IconPath!);
                    if (menuBitmap != HBITMAP.Null)
                    {
                        bitmaps.Add(menuBitmap);
                        mask |= MENU_ITEM_MASK.MIIM_BITMAP;
                    }
                }

                var menuItemInfo = new MENUITEMINFOW
                {
                    cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
                    fMask = mask,
                    fType = MENU_ITEM_TYPE.MFT_STRING,
                    wID = menuId,
                    hSubMenu = subMenu ?? HMENU.Null,
                    hbmpItem = menuBitmap,
                    dwTypeData = textPtr,
                    cch = (uint)item.Text.Length,
                };

                using var safeMenuHandle = new SafeMenuHandle(menuHandle);
                PInvoke.InsertMenuItem(safeMenuHandle, uint.MaxValue, true, in menuItemInfo);
            }
        }
    }

    private HBITMAP CreateMenuBitmap(string iconPath)
    {
        int width = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON);
        int height = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON);

        using var iconHandle = PInvoke.LoadImage(null, iconPath, GDI_IMAGE_TYPE.IMAGE_ICON, width, height, IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);
        if (iconHandle is null || iconHandle.IsInvalid)
        {
            return HBITMAP.Null;
        }

        IntPtr iconPtr = iconHandle.DangerousGetHandle();
        iconHandle.SetHandleAsInvalid();
        var hIcon = new HICON(iconPtr);
        HDC screenDc = PInvoke.GetDC(HWND.Null);
        if (screenDc == HDC.Null)
        {
            PInvoke.DestroyIcon(hIcon);
            return HBITMAP.Null;
        }

        HDC memoryDc = PInvoke.CreateCompatibleDC(screenDc);
        HBITMAP bitmap = PInvoke.CreateCompatibleBitmap(screenDc, width, height);
        HGDIOBJ oldBitmap = PInvoke.SelectObject(memoryDc, bitmap);

        uint color = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_MENU);
        HBRUSH brush = PInvoke.CreateSolidBrush(new COLORREF(color));
        var rect = new RECT
        {
            left = 0,
            top = 0,
            right = width,
            bottom = height,
        };
        using var safeBrush = new SafeBrushHandle(brush);
        PInvoke.FillRect(memoryDc, rect, safeBrush);
        PInvoke.DeleteObject(brush);

        PInvoke.DrawIconEx(memoryDc, 0, 0, hIcon, width, height, 0, HBRUSH.Null, DI_FLAGS.DI_NORMAL);

        PInvoke.SelectObject(memoryDc, oldBitmap);
        PInvoke.DeleteDC(memoryDc);
        PInvoke.ReleaseDC(HWND.Null, screenDc);
        PInvoke.DestroyIcon(hIcon);

        return bitmap;
    }

    private void ExecuteMenuCommand(uint commandId)
    {
        Console.WriteLine($"Tray menu command: {commandId}");
        if (!menuActions.TryGetValue(commandId, out var action))
        {
            return;
        }

        ExecuteAction(action.Action);
    }

    private void ExecuteAction(Action? action)
    {
        if (action is null)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                Console.WriteLine("Tray action invoked.");
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tray icon action failed: {ex}");
            }
        });
    }

    private static unsafe void SetNotifyIconTooltip(ref NOTIFYICONDATAW data, string? tooltip)
    {
        string safeTooltip = tooltip ?? string.Empty;
        if (safeTooltip.Length >= NotifyIconTipMaxLength)
        {
            safeTooltip = safeTooltip[..(NotifyIconTipMaxLength - 1)];
        }

        data.szTip = safeTooltip;
    }

    private sealed class SafeMenuHandle : SafeHandle
    {
        public unsafe SafeMenuHandle(HMENU handle)
            : base(IntPtr.Zero, false)
        {
            SetHandle((IntPtr)handle.Value);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle() => true;
    }

    private sealed class SafeBrushHandle : SafeHandle
    {
        // csharpier-ignore
        public unsafe SafeBrushHandle(HBRUSH handle): base(IntPtr.Zero, false)
        {
            SetHandle((IntPtr)handle.Value);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle() => true;
    }

    private static int LOWORD(WPARAM value) => (short)(value.Value & 0xFFFF);

    private void ResolveDefaultActions()
    {
        defaultClickAction = null;
        defaultDoubleClickAction = null;

        int index = 0;
        ResolveDefaultActions(menuItems, ref index);
    }

    private void ResolveDefaultActions(IReadOnlyList<TrayMenuItem> items, ref int index)
    {
        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                continue;
            }

            if (item.Children is { Count: > 0 })
            {
                ResolveDefaultActions(item.Children, ref index);
                continue;
            }

            if (item.Action is null)
            {
                continue;
            }

            if (index == defaultClickIndex)
            {
                defaultClickAction = item.Action;
            }

            if (index == defaultDoubleClickIndex)
            {
                defaultDoubleClickAction = item.Action;
            }

            index++;
        }
    }

    private sealed class MenuAction
    {
        public MenuAction(Action action)
        {
            Action = action;
        }

        public Action Action { get; }
    }

    private sealed class MenuBuildResult : IDisposable
    {
        public MenuBuildResult(HMENU menuHandle, List<HBITMAP> bitmaps)
        {
            MenuHandle = menuHandle;
            Bitmaps = bitmaps;
        }

        public HMENU MenuHandle { get; }
        private List<HBITMAP> Bitmaps { get; }

        public void Dispose()
        {
            foreach (var bitmap in Bitmaps)
            {
                if (bitmap != HBITMAP.Null)
                {
                    PInvoke.DeleteObject(bitmap);
                }
            }

            if (MenuHandle != HMENU.Null)
            {
                PInvoke.DestroyMenu(MenuHandle);
            }
        }
    }
}
