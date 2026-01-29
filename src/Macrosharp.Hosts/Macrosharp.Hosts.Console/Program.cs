// using System;
// using System.Runtime.CompilerServices;
// using Macrosharp.Win32.Abstractions.WindowTools;
// using System.Runtime.InteropServices;
// using Macrosharp.UserInterfaces.DynamicWindow;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Macrosharp.Devices.Keyboard;
using Macrosharp.UserInterfaces.DynamicWindow;
using Macrosharp.UserInterfaces.TrayIcon;
using Macrosharp.Win32.Abstractions.Explorer;
using Windows.Win32; // For PInvoke access to generated constants and methods
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using static Macrosharp.Devices.Keyboard.KeyboardHookManager;

namespace Macrosharp.Hosts.ConsoleHost;

public class Program
{
    static void Main()
    {
        #region AcquireMutex
        // var mutex = MutexGuardLock.AcquireMutex("Macrosharp");
        // var mutex2 = MutexGuardLock.AcquireMutex("Macrosharp");

        // if (mutex.IsError) {
        //     Console.WriteLine(mutex.Errors[0].Description);
        //     Environment.Exit(1);
        // }

        // Console.WriteLine("Mutex acquired");
        // mutex.Value.ReleaseMutex();
        // Development.PrintObjectMembers(mutex);
        #endregion

        #region GetWindowHandleByClassName
        // To get the first window with the specified class name
        //Task.Delay(2000).Wait();

        //var hWnd = PInvoke.GetForegroundWindow();
        //if (hWnd == HWND.Null)
        //{
        //    Console.WriteLine("No foreground window found");
        //    return;
        //}

        //var className = WindowFinder.GetWindowClassName();
        //if (className == null)
        //{
        //    Console.WriteLine("Failed to get window class name");
        //    return;
        //}
        //Console.WriteLine($"Foreground window class name: {className}");

        //var result = WindowFinder.GetHwndByClassName(className);
        //if (result.Count > 0)
        //    Console.WriteLine($"Found window handle: {result[0]}");
        //else
        //{
        //    Console.WriteLine("No window found");
        //    return;
        //}

        ////To get all windows with the specified class name
        //var allWindows = WindowFinder.GetHwndByClassName(className, true);
        //Console.WriteLine($"Found {allWindows.Count} windows");

        //// Get Window by its title
        //var allHandles = WindowFinder.GetHwndByTitle("Untitled - Paint", true);
        //if (allHandles.Count > 0)
        //    Console.WriteLine($"Found {allHandles.Count} window handles with title: \"Untitled - Paint\"");
        //else
        //    Console.WriteLine("No window found with title: \"Untitled - Paint\"");

        #endregion

        #region GetHwndByTitle & ToggleAlwaysOnTopState
        //var hwnd = WindowFinder.GetHwndByTitle("Untitled - Paint");

        //if (hwnd == HWND.Null)
        //{
        //    Console.WriteLine("No window found with the specified title");
        //    return;
        //}

        //Console.WriteLine($"Found window handle: {hwnd}");

        //var isTopMost = WindowModifier.ToggleAlwaysOnTopState(hwnd);
        //Console.WriteLine($"The always-on-top state of the window is now: {(isTopMost == 1 ? "On" : "Off")}");
        #endregion

        #region MessageBox
        //var result = PInvoke.MessageBox(HWND.Null, "This is a test message.", "Test", MESSAGEBOX_STYLE.MB_ICONINFORMATION);
        //Console.WriteLine($"User clicked button with value: {result}");
        #endregion

        #region AdjustWindowPositionAndSize
        //var hwnd = WindowFinder.GetHwndByTitle("Untitled - Paint");
        //if (hwnd != HWND.Null)
        //{
        // Move the window window 50 pixels right and 30 pixels down
        //WindowModifier.AdjustWindowPositionAndSize(hwnd, deltaX: 50, deltaY: 30);
        //Console.ReadLine();

        // Increase window width by 100 and decrease its height by 50 pixels
        //WindowModifier.AdjustWindowPositionAndSize(hwnd, deltaWidth: 250, deltaHeight: -250);
        //Console.ReadLine();

        // Move a window diagonally and make it slightly larger
        //WindowModifier.AdjustWindowPositionAndSize(hwnd, deltaX: 20, deltaY: 20, deltaWidth: 10, deltaHeight: 10);
        //}
        #endregion

        #region AdjustWindowOpacity
        //var hwnd = WindowFinder.GetHwndByTitle("Untitled - Paint");
        //if (hwnd != HWND.Null)
        //{
        //    WindowModifier.AdjustWindowOpacity(hwnd, opacityDelta: -50);
        //}
        #endregion

        #region SendMessageToWindow & PostMessageToWindow
        //var allHandles = WindowFinder.GetHwndByTitle("Untitled - Paint");
        //if (allHandles == null) {
        //    Console.WriteLine("No window found with the specified title");
        //    return;
        //}

        //var hwnd = allHandles[0];
        //if (hwnd != HWND.Null)
        //{
        //    //Close a window
        //    Messaging.SendMessageToWindow(hwnd, PInvoke.WM_CLOSE);
        //    Messaging.PostMessageToWindow(hwnd, PInvoke.WM_CLOSE);

        //    //Set window text
        //    Messaging.SendMessageToWindow(hwnd, PInvoke.WM_SETTEXT, default, "New Window Title");

        //    //Send a custom message
        //    var WM_MYCUSTOMMESSAGE = PInvoke.WM_APP + 1; // Custom message should be in the range of WM_APP to 0xBFFF
        //    Messaging.SendMessageToWindow(hwnd, WM_MYCUSTOMMESSAGE, (WPARAM)1, (LPARAM)2);
        //}
        #endregion

        #region Keyboard
        // KeyboardListener listener = new KeyboardListener();
        // listener.StartListening();
        #endregion

        #region Notes
        // PInvoke.GetModuleHandle(null) causes compiler error. But PInvoke.GetModuleHandle((string?)null) works.
        #endregion

        #region WindowSample
        // WindowSample.Main();

        // var window = new SimpleWindow("Dynamic Input Window", 150, 250, 50, 20, 10, 50, 50);
        // window.CreateDynamicInputWindow(["Character", "Delay", "Another Input"], ["A", "0", "Another Placeholder"], true);
        // Console.WriteLine($"Pressed Keys: {(window.userInputs.Count > 0 ? window.userInputs[0] : "0")}\nCaptured VK: {window.capturedKeyVK}\nCaptured ScanCode: {window.capturedKeyScanCode}\nCaptured Key Name: {window.capturedKeyName}");
        #endregion

        #region KeyMapper
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_A, false)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_A, false)}"); // 97 ('a')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_A, true)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_A, true)}"); // 65 ('A')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_1, false)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_1, false)}"); // 49 ('1')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_1, true)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_1, true)}"); // 33 ('!')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.OEM_1, false)} | {KeysMapper.GetAsciiCode(VirtualKey.OEM_1, false)}"); // 59 (';')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.OEM_1, true)} | {KeysMapper.GetAsciiCode(VirtualKey.OEM_1, true)}"); // 58 (':')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.RETURN, false)} | {KeysMapper.GetAsciiCode(VirtualKey.RETURN, false)}"); // 13 (RETURN)
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.LCONTROL, false)} | {KeysMapper.GetAsciiCode(VirtualKey.LCONTROL, false)}"); // 0 (LCONTROL)

        // // Press 'Caps Lock' to toggle the case of letters
        // PInvoke.keybd_event((byte)VirtualKey.CAPITAL, 0, 0, 0);
        // PInvoke.keybd_event((byte)VirtualKey.CAPITAL, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, 0);
        // Console.WriteLine("\n\nCaps Lock toggled. Now the next letter keys will be uppercase.");

        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_A, false)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_A, false)}"); // 97 ('a')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_A, true)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_A, true)}"); // 65 ('A')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_1, false)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_1, false)}"); // 49 ('1')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.KEY_1, true)} | {KeysMapper.GetAsciiCode(VirtualKey.KEY_1, true)}"); // 33 ('!')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.OEM_1, false)} | {KeysMapper.GetAsciiCode(VirtualKey.OEM_1, false)}"); // 59 (';')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.OEM_1, true)} | {KeysMapper.GetAsciiCode(VirtualKey.OEM_1, true)}"); // 58 (':')
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.RETURN, false)} | {KeysMapper.GetAsciiCode(VirtualKey.RETURN, false)}"); // 13 (RETURN)
        // Console.WriteLine($"{KeysMapper.GetDisplayName(VirtualKey.LCONTROL, false)} | {KeysMapper.GetAsciiCode(VirtualKey.LCONTROL, false)}"); // 0 (LCONTROL)
        #endregion

        #region KeyboardListener
        // StartKeyboardHook();
        #endregion

        #region Mouse & Keyboard Simulators
        // StartMouseSimulator();
        // StartKeyboardSimulator();
        #endregion

        #region ImageEditorWindow
        // Launch the lightweight image editor window
        // Usage: press W/L/C/Space to switch tools, Ctrl+Z/Y to undo/redo, F1 to toggle status bar.
        // Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.Run("Macrosharp Image Editor");

        // Example: open an image file via Ctrl+O dialog, or paste from clipboard via Ctrl+V.
        // If you want to load a known file path programmatically, call the editor API directly:
        // var window = new UserInterfaces.ImageEditorWindow.ImageEditorWindow("Macrosharp Image Editor");
        // window.Run(); // inside the editor: Ctrl+O / Ctrl+V

        // Direct launch with a file
        // Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.RunWithFile("C:\\Images\\sample.png");

        // Direct launch from clipboard
        // Macrosharp.UserInterfaces.ImageEditorWindow.ImageEditorWindowHost.RunWithClipboard();
        #endregion

        #region ExplorerController
        // HWND activeHwnd = PInvoke.GetForegroundWindow();
        // HWND activeHwnd = default;
        // Console.WriteLine($"Active HWND: {activeHwnd}");

        // string? currentFolder = ExplorerController.GetCurrentFolderPath(activeHwnd);
        // Console.WriteLine($"Current folder: {currentFolder ?? "<unknown>"}");

        // var selectedItems = ExplorerController.GetSelectedItems(activeHwnd);
        // Console.WriteLine("Selected items:");
        // foreach (var item in selectedItems)
        //     Console.WriteLine($" - {item}");

        // ExplorerController.Refresh(activeHwnd);

        // string demoFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // ExplorerController.OpenFolder(demoFolder);
        // string? currentFolder = ExplorerController.GetCurrentFolderPath();

        // // Select items in the current folder
        // if (!string.IsNullOrWhiteSpace(currentFolder) && Directory.Exists(currentFolder))
        // {
        //     var candidates = Directory.EnumerateFileSystemEntries(currentFolder).ToList();
        //     if (candidates.Count > 0)
        //     {
        //         ExplorerController.SelectItems(currentFolder, candidates, activeHwnd);
        //         Console.WriteLine($"Selected {candidates.Count} items in folder: {currentFolder}");
        //     }
        // }

        // if (Directory.Exists(demoFolder))
        // {
        // var demoCandidates = Directory.EnumerateFileSystemEntries(demoFolder).ToList();
        // if (demoCandidates.Count > 0)
        // {
        // ExplorerShellManager.OpenAndSelectItems(demoFolder, demoCandidates);

        // Example: invoke a context menu verb on the selected items.
        // Common verbs include: "open", "properties", "delete".
        // Multi-select uses the active Explorer window, so ensure the folder is open and focused.
        // bool invoked = ExplorerShellManager.ExecuteContextMenuAction(demoFolder, demoCandidates, "properties", mode: ExplorerShellManager.ContextMenuInvokeMode.MultiSelect);

        // Per-item invocation (explicit):
        // bool invoked = ExplorerController.ExecuteContextMenuAction(demoFolder, demoCandidates, "properties", mode: ExplorerController.ContextMenuInvokeMode.PerItem);

        // Console.WriteLine($"Context menu action invoked: {invoked}");
        // }
        // }
        #endregion

        #region ExplorerFileAutomation
        // Ensure an Explorer window is focused for these tests.
        // HWND targetHwnd = PInvoke.GetForegroundWindow();

        // 1) Create a new incremental text file in the active Explorer/desktop and enter edit mode.
        // int created = ExplorerFileAutomation.CreateNewFile(targetHwnd);
        // Console.WriteLine($"CreateNewFile result: {created}");

        // 2) Convert selected Office files to PDF (PowerPoint/Word/Excel supported).
        // ExplorerFileAutomation.OfficeFilesToPdf("PowerPoint");
        // ExplorerFileAutomation.OfficeFilesToPdf("Word");
        // ExplorerFileAutomation.OfficeFilesToPdf("Excel");

        // 3) Convert selected files with a custom converter.
        // Example: convert .txt files to .bak copies in the same folder.
        // ExplorerFileAutomation.GenericFileConverter(new[] { ".txt" }, (input, output) => File.Copy(input, output), newExtension: ".bak");

        // Example: convert .mp3 files to .wav using ffmpeg.
        // ExplorerFileAutomation.GenericFileConverter(
        //     new[] { ".mp3" },
        //     (input, output) =>
        //     {
        //         var ffmpeg = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, CreateNoWindow = true };
        //         ffmpeg.ArgumentList.Add("-loglevel");
        //         ffmpeg.ArgumentList.Add("error");
        //         ffmpeg.ArgumentList.Add("-hide_banner");
        //         ffmpeg.ArgumentList.Add("-nostats");
        //         ffmpeg.ArgumentList.Add("-i");
        //         ffmpeg.ArgumentList.Add(input);
        //         ffmpeg.ArgumentList.Add(output);

        //         using var process = Process.Start(ffmpeg);
        //         process?.WaitForExit();
        //     },
        //     newExtension: ".wav",
        //     hwnd: targetHwnd
        // );

        // 4) Flatten selected folders into a new "Flattened" directory.
        // ExplorerFileAutomation.FlattenDirectories();

        // 5) Combine selected images into a PDF (Normal = no resize, Resize = resize mode).
        // ExplorerFileAutomation.ImagesToPdf();
        ExplorerFileAutomation.ImagesToPdf(mode: ExplorerFileAutomation.ImagesToPdfMode.Resize, targetWidth: 690, widthThreshold: 1200, minWidth: 100, minHeight: 100, hwnd: default);

        #endregion

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void HookManager_KeyPressed(object? sender, KeyboardEvent e)
    {
        // Console.WriteLine($"Key Pressed: {e.Key} ({(ushort)e.Key}) | IsExtendedKey: {e.IsExtendedKey} | IsInjected: {e.IsInjected} | IsAltDown: {e.IsAltDown}");
        var scanCode = PInvoke.MapVirtualKey((uint)e.KeyCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);

        string pressedModifiers = Modifiers.GetModifiersStringFromMask(Modifiers.CurrentModifiers);
        if (string.IsNullOrEmpty(pressedModifiers))
            pressedModifiers = "None";

        Console.WriteLine($"Pressed Modifiers: {Modifiers.CurrentModifiers}, {pressedModifiers} | Pressed Key: {e} | CapsLck={Modifiers.IsCapsLockOn}, NumLck={Modifiers.IsNumLockOn}, ScrollLck={Modifiers.IsScrollLockOn}");

        // If 'Q' is pressed, post a quit message to terminate the message loop.
        if (e.KeyCode == VirtualKey.KEY_Q)
        {
            PInvoke.PostQuitMessage(0);
            e.Handled = true; // Optionally suppress 'Q' from reaching other apps if you quit cleanly.
        }
    }

    private static void SuppressKEY_A(object? sender, KeyboardEvent e)
    {
        Console.WriteLine($"Key Pressed (event 2): {e.KeyCode} ({(ushort)e.KeyCode}) | IsExtendedKey: {e.IsExtendedKey} | IsInjected: {e.IsInjected} | IsAltDown: {e.IsAltDown}");

        //We can define a handler for suppressing specific keys.
        if (e.KeyCode == VirtualKey.KEY_A)
        {
            e.Handled = true; // Prevents 'A' from reaching other applications.
            Console.WriteLine(" 'A' key press suppressed!");
        }
    }

    private static void HookManager_KeyReleased(object? sender, KeyboardEvent e)
    {
        // Accessing these properties to ensure they are updated.
        // _ = Modifiers.IsNumLockOn;
        // _ = Modifiers.IsCapsLockOn;
        // _ = Modifiers.IsScrollLockOn;
    }

    private static void StartKeyboardHook()
    {
        Console.WriteLine("Starting Keyboard Hook and Hotkey Manager...");
        Console.WriteLine("Press ESCAPE or 'q' to quit and unhook.");
        Console.WriteLine("Registered Hotkeys: Ctrl+Alt+Z | Shift+A");
        Console.WriteLine("Press F1 to click at current mouse position.");
        Console.WriteLine("Press F2 to move cursor by (50, 50).");
        Console.WriteLine("Press F3 to scroll down.");

        using KeyboardHookManager hookManager = new KeyboardHookManager();
        using HotkeyManager hotkeyManager = new HotkeyManager(hookManager);

        // EventHandler<KeyPressedArgs>[] keyPressedEventHandlers = [HookManager_KeyPressed, HookManager_KeyPressed2];
        // foreach (var handler in keyPressedEventHandlers)
        //     _hookManager.KeyPressed += handler;

        hookManager.KeyDownHandler += HookManager_KeyPressed;
        // _hookManager.KeyPressed += SuppressKEY_A;

        hookManager.KeyUpHandler += HookManager_KeyReleased;

        try
        {
            hookManager.Start();
            Console.WriteLine("Hook installed successfully.");

            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_Z,
                Modifiers.CTRL | Modifiers.ALT,
                () =>
                {
                    Console.WriteLine("!!! Ctrl+Alt+Z Hotkey Activated !!!");
                }
            );

            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_A,
                Modifiers.SHIFT,
                () =>
                {
                    Console.WriteLine("!!! Shift+A Hotkey Activated !!!");
                }
            );

            // Register an exit hotkey (e.g., Escape)
            // Note: For single keys without modifiers, pass 0 for modifiers.
            hotkeyManager.RegisterHotkey(
                VirtualKey.ESCAPE,
                0,
                () =>
                {
                    Console.WriteLine("Escape pressed. Exiting...");
                    PInvoke.PostQuitMessage(0);
                }
            );

            // Register mouse manipulation hotkeys
            hotkeyManager.RegisterHotkey(
                VirtualKey.F1,
                0,
                () =>
                {
                    Console.WriteLine("F1 pressed. Simulating Left Click at current position.");
                    MouseSimulator.SendMouseClick(); // Left click at current position
                }
            );

            hotkeyManager.RegisterHotkey(
                VirtualKey.F2,
                0,
                () =>
                {
                    Console.WriteLine("F2 pressed. Moving cursor by (50, 50).");
                    MouseSimulator.MoveCursor(50, 50);
                }
            );

            hotkeyManager.RegisterHotkey(
                VirtualKey.F3,
                0,
                () =>
                {
                    Console.WriteLine("F3 pressed. Scrolling mouse wheel up.");
                    MouseSimulator.SendMouseScroll(steps: 1); // Scroll up by one step
                }
            );

            // Keep the application running and process messages.
            MSG msg;

            while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0) != 0) // Use HWND.Null for nint.Zero where appropriate
            {
                if (msg.message == PInvoke.WM_QUIT)
                {
                    break; // Exit the loop if WM_QUIT is received
                }
                PInvoke.TranslateMessage(in msg);
                PInvoke.DispatchMessage(in msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            // Ensure the hook is stopped when the application exits.
            if (hookManager != null)
            {
                hookManager.Stop();
                hookManager.Dispose();
                hotkeyManager.Dispose();

                Console.WriteLine("Hook uninstalled.");
            }
        }

        Console.WriteLine("Application exiting.");
    }

    private static void StartMouseSimulator()
    {
        Console.WriteLine("Starting Keyboard Hook and Hotkey Manager for Mouse Simulator examples...");
        Console.WriteLine("Press F1 to Left Click at current mouse position.");
        Console.WriteLine("Press F2 to Right Click at current mouse position (Mouse Down only).");
        Console.WriteLine("Press F3 to Middle Click at (100, 150) screen coordinates.");
        Console.WriteLine("Press F4 to simulate XButton1 click.");
        Console.WriteLine("Press F5 to move cursor by (50, 50).");
        Console.WriteLine("Press F6 to scroll mouse wheel up.");
        Console.WriteLine("Press F7 to scroll mouse wheel down.");
        Console.WriteLine("Press F8 to scroll mouse wheel left (horizontal).");
        Console.WriteLine("Press Escape to exit.");

        // Use 'using' statements to ensure proper disposal of hook managers.
        using (var keyboardHookManager = new KeyboardHookManager())
        using (var hotkeyManager = new HotkeyManager(keyboardHookManager))
        {
            // Start the keyboard hook to listen for hotkeys.
            keyboardHookManager.Start();

            // --- Register Hotkeys for MouseSimulator Actions ---

            // F1: Left Click at current position
            hotkeyManager.RegisterHotkey(
                VirtualKey.F1,
                0,
                () =>
                {
                    Console.WriteLine("\nF1 pressed: Simulating Left Click at current mouse position.");
                    MouseSimulator.SendMouseClick(button: MouseButton.LeftButton, op: MouseEventOperation.MouseDown);
                }
            );

            // F2: Right Click (Mouse Down only) at current position
            hotkeyManager.RegisterHotkey(
                VirtualKey.F2,
                0,
                () =>
                {
                    Console.WriteLine("\nF2 pressed: Simulating Right Mouse Down only.");
                    MouseSimulator.SendMouseClick(button: MouseButton.RightButton, op: MouseEventOperation.Click);
                }
            );

            // F3: Middle Click at specific screen coordinates
            hotkeyManager.RegisterHotkey(
                VirtualKey.F3,
                0,
                () =>
                {
                    Console.WriteLine("\nF3 pressed: Simulating Middle Click at (100, 150).");
                    MouseSimulator.SendMouseClick(x: 100, y: 150, button: MouseButton.MiddleButton, op: MouseEventOperation.Click);
                }
            );

            // F4: XButton1 Click (example for a typical side mouse button)
            hotkeyManager.RegisterHotkey(
                VirtualKey.F4,
                0,
                () =>
                {
                    Console.WriteLine("\nF4 pressed: Simulating XButton1 click.");
                    MouseSimulator.SendMouseClick(button: MouseButton.XButton1, op: MouseEventOperation.Click);
                }
            );

            // F5: Move Cursor
            hotkeyManager.RegisterHotkey(
                VirtualKey.F5,
                0,
                () =>
                {
                    Console.WriteLine("\nF5 pressed: Moving cursor by (50, 50) from current position.");
                    MouseSimulator.MoveCursor(dx: 50, dy: 50);
                }
            );

            // F6: Scroll Up
            hotkeyManager.RegisterHotkey(
                VirtualKey.F6,
                0,
                () =>
                {
                    Console.WriteLine("\nF6 pressed: Scrolling mouse wheel up.");
                    MouseSimulator.SendMouseScroll(steps: 1, direction: 1); // Vertical scroll, 1 step up
                }
            );

            // F7: Scroll Down
            hotkeyManager.RegisterHotkey(
                VirtualKey.F7,
                0,
                () =>
                {
                    Console.WriteLine("\nF7 pressed: Scrolling mouse wheel down.");
                    MouseSimulator.SendMouseScroll(steps: -1, direction: 1); // Vertical scroll, 1 step down
                }
            );

            // F8: Scroll Left (Horizontal)
            hotkeyManager.RegisterHotkey(
                VirtualKey.F8,
                0,
                () =>
                {
                    Console.WriteLine("\nF8 pressed: Scrolling mouse wheel left.");
                    MouseSimulator.SendMouseScroll(steps: -1, direction: 0); // Horizontal scroll, 1 step left
                }
            );

            // Escape: Exit the application
            hotkeyManager.RegisterHotkey(
                VirtualKey.ESCAPE,
                0,
                () =>
                {
                    Console.WriteLine("\nEscape pressed. Exiting application.");
                    // This will post a WM_QUIT message to the current thread's message queue,
                    // causing GetMessage to return 0 and break the loop.
                    PInvoke.PostQuitMessage(0);
                }
            );

            // Message loop to keep the application running and process Windows messages.
            // This is essential for the low-level keyboard hook to function correctly.
            MSG msg;
            while (PInvoke.GetMessage(out msg, new HWND(), 0, 0).Value != 0)
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }

    private static void StartKeyboardSimulator()
    {
        Console.WriteLine("Starting Keyboard Hook and Hotkey Manager for Keyboard Simulator examples...");
        Console.WriteLine("Press '1' to simulate a single 'A' key press.");
        Console.WriteLine("Press '2' to simulate 'Hello World!' sequence with delays.");
        Console.WriteLine("Press '3' to find 'Notepad' and send 'Hello Notepad!' (using default PostMessage).");
        Console.WriteLine("Press '4' to start the interactive Burst Click Simulator.");
        Console.WriteLine("Press Escape to exit.");

        var iconPaths = GetTrayIconPaths();
        var iconEnumerator = iconPaths.Count > 0 ? GetIconEnumerator(iconPaths) : null;
        string? GetNextIcon() => iconEnumerator is null ? null : MoveNext(iconEnumerator);

        bool isSilentMode = false;
        TrayIconHost? trayHost = null;

        void OpenScriptFolder()
        {
            string folder = AppContext.BaseDirectory;
            Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
        }

        void ShowNotification()
        {
            Console.WriteLine("Tray action: show notification requested (placeholder).");
        }

        void SwitchIcon()
        {
            string? nextIcon = GetNextIcon();
            if (!string.IsNullOrWhiteSpace(nextIcon))
            {
                trayHost?.UpdateIcon(nextIcon);
            }
        }

        void ReloadHotkeys() => Console.WriteLine("Tray action: reload hotkeys.");
        void ReloadConfigs() => Console.WriteLine("Tray action: reload configs.");

        void ClearConsoleLogs()
        {
            Console.Clear();
            Console.WriteLine("Console cleared by tray action.");
        }

        void ToggleSilentMode()
        {
            isSilentMode = !isSilentMode;
            Console.WriteLine($"Silent mode toggled: {(isSilentMode ? "On" : "Off")}");
        }

        var trayMenu = new List<TrayMenuItem>
        {
            TrayMenuItem.ActionItem("Open Script Folder", OpenScriptFolder, iconPath: GetNextIcon()),
            TrayMenuItem.ActionItem("Show Notification", ShowNotification, iconPath: GetNextIcon()),
            TrayMenuItem.ActionItem("Switch Icon", SwitchIcon, iconPath: GetNextIcon()),
            TrayMenuItem.Submenu("Reload", new List<TrayMenuItem> { TrayMenuItem.ActionItem("Hotkeys", ReloadHotkeys, iconPath: GetNextIcon()), TrayMenuItem.ActionItem("Configs", ReloadConfigs, iconPath: GetNextIcon()) }, iconPath: GetNextIcon()),
            TrayMenuItem.ActionItem("Clear Console Logs", ClearConsoleLogs, iconPath: GetNextIcon()),
            TrayMenuItem.ActionItem("Toggle Silent Mode", ToggleSilentMode, iconPath: GetNextIcon()),
        };

        trayHost = new TrayIconHost("Macropy", GetNextIcon(), trayMenu, defaultClickIndex: 2, defaultDoubleClickIndex: 0);
        trayHost.Start();

        // Use 'using' statements to ensure proper disposal of hook managers.
        using (var keyboardHookManager = new KeyboardHookManager())
        using (var hotkeyManager = new HotkeyManager(keyboardHookManager))
        {
            // Start the keyboard hook to listen for hotkeys.
            keyboardHookManager.Start();

            // --- Register Hotkeys for KeyboardSimulator Actions ---

            // F1: Simulate a single 'A' key press
            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_1,
                0,
                () =>
                {
                    Console.WriteLine("\nF1 pressed: Simulating a single 'A' key press.");
                    KeyboardSimulator.SimulateKeyPress(VirtualKey.KEY_A);
                }
            );

            // F2: Simulate a sequence of key presses ("Hello World!")
            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_2,
                0,
                () =>
                {
                    Console.WriteLine("\nF2 pressed: Simulating 'Hello World!' sequence.");
                    var keysSequence = new List<object>
                    {
                        VirtualKey.KEY_H,
                        VirtualKey.KEY_E,
                        VirtualKey.KEY_L,
                        VirtualKey.KEY_L,
                        VirtualKey.KEY_O,
                        VirtualKey.SPACE,
                        VirtualKey.KEY_W,
                        VirtualKey.KEY_O,
                        VirtualKey.KEY_R,
                        VirtualKey.KEY_L,
                        VirtualKey.KEY_D,
                        // Simulate Shift+1 for '!' using a List<VirtualKey> to represent the hotkey
                        new List<VirtualKey> { VirtualKey.SHIFT, VirtualKey.KEY_1 },
                    };
                    KeyboardSimulator.SimulateKeyPressSequence(keysSequence, 0);
                }
            );

            // F3: Find Notepad and send "Hello Notepad!"
            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_3,
                0,
                () =>
                {
                    Console.WriteLine("\nF3 pressed: Finding 'Notepad' and sending 'Hello Notepad!'.");
                    // You might need to adjust "Notepad" to your system's actual Notepad window class name
                    // (e.g., "Notepad" or "Edit"). You can use tools like Spy++ to find it.
                    // For a simpler test, ensure Notepad is open before pressing F3.

                    // Example of sending 'H' to Notepad
                    int result1 = KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_H);
                    if (result1 == 1)
                    {
                        Console.WriteLine("Sent 'H' to Notepad successfully.");
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_E);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_L);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_L);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_O);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.SPACE);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_N);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_O);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_T);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_E);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_P);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_A);
                        KeyboardSimulator.FindAndSendKeyToWindow("Notepad", VirtualKey.KEY_D);

                        // Sending '!' directly with SimulateHotKeyPress. You could also do this with the modified SimulateKeyPressSequence
                        KeyboardSimulator.SimulateHotKeyPress(new Dictionary<VirtualKey, int> { { VirtualKey.SHIFT, 0 }, { VirtualKey.KEY_1, 0 } });
                    }
                }
            );

            // F4: Start interactive Burst Click Simulator
            hotkeyManager.RegisterHotkey(
                VirtualKey.KEY_4,
                0,
                () =>
                {
                    // This call will block and prompt the user for input in the console.
                    KeyboardSimulator.SimulateBurstClicks();
                }
            );

            // Escape: Exit the application
            hotkeyManager.RegisterHotkey(
                VirtualKey.ESCAPE,
                0,
                () =>
                {
                    Console.WriteLine("\nEscape pressed. Exiting application.");

                    // Ensure tray icon is disposed
                    trayHost?.Dispose();

                    // This will post a WM_QUIT message to the current thread's message queue,
                    PInvoke.PostQuitMessage(0);
                }
            );

            // Message loop to keep the application running and process Windows messages.
            MSG msg;
            while (PInvoke.GetMessage(out msg, new HWND(), 0, 0).Value != 0)
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }

    private static List<string> GetTrayIconPaths()
    {
        string baseDir = AppContext.BaseDirectory;
        string iconsPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "assets", "Icons"));

        if (!Directory.Exists(iconsPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(iconsPath, "*.ico", SearchOption.AllDirectories).ToList();
    }

    private static IEnumerator<string> GetIconEnumerator(IReadOnlyList<string> icons)
    {
        int index = 0;
        while (true)
        {
            yield return icons[index];
            index = (index + 1) % icons.Count;
        }
    }

    private static string MoveNext(IEnumerator<string> enumerator)
    {
        if (!enumerator.MoveNext())
        {
            enumerator.Reset();
            enumerator.MoveNext();
        }

        return enumerator.Current;
    }
}

//TODO: Update to test Hotkeys configurations.
