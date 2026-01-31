using System.Runtime.InteropServices;
using Macrosharp.Devices.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse; // For INPUT, KEYBDINPUT, MOUSE_EVENT_FLAGS, VirtualKey

namespace Macrosharp.Devices.Core;

/// <summary>Provides methods for simulating keyboard input, including key presses, sequences, and hotkeys.</summary>
public static partial class KeyboardSimulator
{
    // Manual P/Invoke for GlobalAlloc since CsWin32's version has signature issues
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalAlloc", SetLastError = true)]
    private static partial nint GlobalAllocManual(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalFree", SetLastError = true)]
    private static partial nint GlobalFreeManual(nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalLock", SetLastError = true)]
    private static partial nint GlobalLockManual(nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalUnlock", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlockManual(nint hMem);

    /// <summary>Simulates a key press (down and then up) for the specified key.</summary>
    /// <param name="key">The virtual key code of the key to press (e.g., VirtualKey.KEY_A).</param>
    /// <param name="keyScancode">The hardware scan code for the key. If 0, the system will determine it from the virtual key.</param>
    /// <param name="times">The number of times the key should be pressed. Defaults to 1.</param>
    public static void SimulateKeyPress(VirtualKey key, int keyScancode = 0, int times = 1, int delayMilliseconds = 10)
    {
        // Define key down and key up inputs
        INPUT[] inputs = new INPUT[2];

        // Key Down
        inputs[0].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[0].Anonymous.ki.wVk = (VIRTUAL_KEY)key;
        inputs[0].Anonymous.ki.wScan = (ushort)keyScancode;
        inputs[0].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY; // General flag for extended keys
        if (keyScancode != 0)
        {
            inputs[0].Anonymous.ki.dwFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;
        }

        // Key Up
        inputs[1].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[1].Anonymous.ki.wVk = (VIRTUAL_KEY)key;
        inputs[1].Anonymous.ki.wScan = (ushort)keyScancode;
        inputs[1].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP | KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY;
        if (keyScancode != 0)
        {
            inputs[1].Anonymous.ki.dwFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;
        }

        for (int i = 0; i < times; i++)
        {
            uint sent = PInvoke.SendInput(inputs, Marshal.SizeOf<INPUT>());
            EnsureSendInput(sent, inputs.Length);

            if (delayMilliseconds > 0)
            {
                Thread.Sleep(delayMilliseconds); // Small delay between presses to ensure separation
            }
        }
    }

    /// <summary>
    /// Simulates a sequence of key presses with an optional delay between each.
    /// Each item in the sequence can be a VirtualKey, a Tuple of VirtualKey and ScanCode,
    /// or a List of VirtualKey (for hotkey combinations).
    /// </summary>
    /// <param name="keysSequence">A list of VirtualKey, Tuple(VirtualKey, int), or List(VirtualKey) representing the sequence of keys.
    /// If an int is provided as the second item in the tuple, it's treated as a scancode.
    /// If a List<VirtualKey> is provided, it's treated as a hotkey combination to be pressed simultaneously.</param>
    /// <param name="delayMilliseconds">The delay in milliseconds between each key press or hotkey combination. Defaults to 200ms.</param>
    public static void SimulateKeyPressSequence(List<object> keysSequence, int delayMilliseconds = 200)
    {
        foreach (var item in keysSequence)
        {
            if (item is VirtualKey vk)
            {
                SimulateKeyPress(vk);
            }
            else if (item is Tuple<VirtualKey, int> keyTuple)
            {
                SimulateKeyPress(keyTuple.Item1, keyTuple.Item2);
            }
            else if (item is List<VirtualKey> hotkeyCombination) // Handle hotkey combinations
            {
                var hotkeyKeys = new Dictionary<VirtualKey, int>();
                foreach (var keyInCombination in hotkeyCombination)
                {
                    hotkeyKeys.Add(keyInCombination, 0); // Scancode is 0 for simplicity, let system determine
                }
                SimulateHotKeyPress(hotkeyKeys);
            }
            else
            {
                Console.WriteLine($"Warning: Unrecognized item type in key sequence: {item?.GetType().Name}. Skipping.");
                continue;
            }

            if (delayMilliseconds > 0)
            {
                Thread.Sleep(delayMilliseconds);
            }
        }
    }

    /// <summary>
    /// Searches for a window with the specified class name, and, if found, sends the specified key using the passed function.
    /// If no send function is specified, PostMessage is used.
    /// </summary>
    /// <param name="targetClassName">The class name of the target window.</param>
    /// <param name="key">The virtual key code to send.</param>
    /// <param name="sendFunction">An optional function that simulates the key. If null, PostMessage is used.</param>
    /// <returns>1 if the window was found and the key was sent, 0 otherwise.</returns>
    public static int FindAndSendKeyToWindow(string targetClassName, VirtualKey key, Action<HWND, VirtualKey>? sendFunction = null)
    {
        // Find the window by class name
        HWND hwnd = PInvoke.FindWindow(targetClassName, null);

        if (hwnd.IsNull)
        {
            Console.WriteLine($"Window with class name '{targetClassName}' not found.");
            return 0;
        }

        if (sendFunction != null)
        {
            // If a custom send function is provided, use it.
            sendFunction(hwnd, key);
            Console.WriteLine($"Key '{key}' sent to window '{targetClassName}' using custom function.");
        }
        else
        {
            // Default to PostMessage if no custom function is provided.
            // Note: PostMessage with WM_KEYDOWN/WM_KEYUP can be less reliable than SendInput for some applications,
            // as it doesn't always mimic actual user input fully (e.g., keyboard state, focus).
            // It's fire-and-forget and doesn't wait for processing.
            PInvoke.PostMessage(hwnd, PInvoke.WM_KEYDOWN, new WPARAM((nuint)key), new LPARAM(0));
            PInvoke.PostMessage(hwnd, PInvoke.WM_KEYUP, new WPARAM((nuint)key), new LPARAM(0));
            Console.WriteLine($"Key '{key}' sent to window '{targetClassName}' using PostMessage.");
        }

        return 1;
    }

    /// <summary>
    /// Simulates a hotkey press by sending a key down event for each of the specified keys
    /// and then sending key up events for all of them. The order of release is the reverse of press.
    /// </summary>
    /// <param name="keysToPress">A dictionary where keys are VirtualKey and values are optional scancodes (0 if not specified).</param>
    public static void SimulateHotKeyPress(Dictionary<VirtualKey, int> keysToPress)
    {
        if (keysToPress == null || keysToPress.Count == 0)
        {
            Console.WriteLine("No keys specified for hotkey simulation.");
            return;
        }

        // Prepare key down inputs
        INPUT[] keyDownInputs = new INPUT[keysToPress.Count];
        int i = 0;
        foreach (var entry in keysToPress)
        {
            keyDownInputs[i].type = INPUT_TYPE.INPUT_KEYBOARD;
            keyDownInputs[i].Anonymous.ki.wVk = (VIRTUAL_KEY)entry.Key;
            keyDownInputs[i].Anonymous.ki.wScan = (ushort)entry.Value;
            keyDownInputs[i].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY; // General flag for extended keys
            if (entry.Value != 0)
            {
                keyDownInputs[i].Anonymous.ki.dwFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;
            }
            i++;
        }

        // Send all key down events
        uint sentDown = PInvoke.SendInput(keyDownInputs, Marshal.SizeOf<INPUT>());
        EnsureSendInput(sentDown, keyDownInputs.Length);

        Thread.Sleep(50); // Small delay to allow keys to register as 'down'

        // Prepare key up inputs (in reverse order for typical hotkey behavior)
        INPUT[] keyUpInputs = new INPUT[keysToPress.Count];
        i = keysToPress.Count - 1;
        foreach (var entry in keysToPress.Reverse()) // Reverse to release in opposite order
        {
            keyUpInputs[i].type = INPUT_TYPE.INPUT_KEYBOARD;
            keyUpInputs[i].Anonymous.ki.wVk = (VIRTUAL_KEY)entry.Key;
            keyUpInputs[i].Anonymous.ki.wScan = (ushort)entry.Value;
            keyUpInputs[i].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP | KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY;
            if (entry.Value != 0)
            {
                keyUpInputs[i].Anonymous.ki.dwFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;
            }
            i--;
        }

        // Send all key up events
        uint sentUp = PInvoke.SendInput(keyUpInputs, Marshal.SizeOf<INPUT>());
        EnsureSendInput(sentUp, keyUpInputs.Length);
    }

    private static void EnsureSendInput(uint sent, int expected)
    {
        if (sent != expected)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>
    /// Sends a specified number of backspace key presses to delete characters.
    /// </summary>
    /// <param name="count">The number of backspace keys to send.</param>
    /// <param name="delayMs">Delay in milliseconds between each backspace. Default is 2ms.</param>
    public static void SendBackspaces(int count, int delayMs = 2)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            SimulateKeyPress(VirtualKey.BACK, delayMilliseconds: delayMs);
        }
    }

    /// <summary>
    /// Types Unicode text using KEYEVENTF_UNICODE flag, which allows typing any Unicode character.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="delayMs">Optional delay in milliseconds between characters.</param>
    public static void TypeUnicodeText(string text, int delayMs = 0)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var inputs = new List<INPUT>();

        foreach (char c in text)
        {
            // Key Down
            INPUT keyDown = new INPUT { type = INPUT_TYPE.INPUT_KEYBOARD };
            keyDown.Anonymous.ki.wVk = 0;
            keyDown.Anonymous.ki.wScan = c;
            keyDown.Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_UNICODE;
            inputs.Add(keyDown);

            // Key Up
            INPUT keyUp = new INPUT { type = INPUT_TYPE.INPUT_KEYBOARD };
            keyUp.Anonymous.ki.wVk = 0;
            keyUp.Anonymous.ki.wScan = c;
            keyUp.Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_UNICODE | KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;
            inputs.Add(keyUp);
        }

        // Send all inputs at once for efficiency
        if (inputs.Count > 0)
        {
            uint sent = PInvoke.SendInput(inputs.ToArray(), Marshal.SizeOf<INPUT>());
            // Note: we don't throw on partial send for text input, just log
            if (sent != inputs.Count)
            {
                Console.WriteLine($"Warning: Only {sent}/{inputs.Count} key events were sent.");
            }
        }

        if (delayMs > 0)
        {
            Thread.Sleep(delayMs);
        }
    }

    /// <summary>
    /// Pastes text using the clipboard. Saves and restores the previous clipboard content.
    /// </summary>
    /// <param name="text">The text to paste.</param>
    /// <param name="delayAfterPasteMs">Delay in milliseconds after paste before restoring clipboard. Default is 50ms.</param>
    public static void PasteText(string text, int delayAfterPasteMs = 50)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string? previousClipboard = null;

        try
        {
            // Save current clipboard content
            previousClipboard = GetClipboardText();

            // Set new clipboard content
            SetClipboardText(text);

            // Small delay to ensure clipboard is set
            Thread.Sleep(10);

            // Send Ctrl+V
            SimulateHotKeyPress(new Dictionary<VirtualKey, int> { { VirtualKey.CONTROL, 0 }, { VirtualKey.KEY_V, 0 } });

            // Wait for paste to complete
            if (delayAfterPasteMs > 0)
            {
                Thread.Sleep(delayAfterPasteMs);
            }
        }
        finally
        {
            // Restore previous clipboard content
            if (previousClipboard != null)
            {
                try
                {
                    SetClipboardText(previousClipboard);
                }
                catch
                {
                    // Ignore errors restoring clipboard
                }
            }
        }
    }

    /// <summary>
    /// Moves the text cursor left by sending left arrow key presses.
    /// </summary>
    /// <param name="count">Number of positions to move left.</param>
    /// <param name="delayMs">Delay in milliseconds between key presses.</param>
    public static void MoveCursorLeft(int count, int delayMs = 2)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            SimulateKeyPress(VirtualKey.LEFT, delayMilliseconds: delayMs);
        }
    }

    /// <summary>
    /// Gets text from the clipboard using Win32 APIs.
    /// </summary>
    /// <returns>The clipboard text, or null if unavailable.</returns>
    private static string? GetClipboardText()
    {
        if (!PInvoke.OpenClipboard(default))
            return null;

        try
        {
            if (!PInvoke.IsClipboardFormatAvailable(13)) // CF_UNICODETEXT
                return null;

            nint hData = PInvoke.GetClipboardData(13);
            if (hData == nint.Zero)
                return null;

            nint pData = GlobalLockManual(hData);
            if (pData == nint.Zero)
                return null;

            try
            {
                return Marshal.PtrToStringUni(pData);
            }
            finally
            {
                GlobalUnlockManual(hData);
            }
        }
        finally
        {
            PInvoke.CloseClipboard();
        }
    }

    /// <summary>
    /// Sets text to the clipboard using Win32 APIs.
    /// </summary>
    /// <param name="text">The text to set.</param>
    private static void SetClipboardText(string text)
    {
        if (!PInvoke.OpenClipboard(default))
            throw new InvalidOperationException("Could not open clipboard.");

        try
        {
            PInvoke.EmptyClipboard();

            // Allocate global memory for the text (including null terminator)
            int byteCount = (text.Length + 1) * 2; // Unicode = 2 bytes per char
            nint hGlobal = GlobalAllocManual(GMEM_MOVEABLE, (nuint)byteCount);

            if (hGlobal == nint.Zero)
                throw new OutOfMemoryException("Could not allocate memory for clipboard.");

            nint pGlobal = GlobalLockManual(hGlobal);
            if (pGlobal == nint.Zero)
            {
                GlobalFreeManual(hGlobal);
                throw new InvalidOperationException("Could not lock global memory.");
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                // Add null terminator
                Marshal.WriteInt16(pGlobal, text.Length * 2, 0);
            }
            finally
            {
                GlobalUnlockManual(hGlobal);
            }

            // Set the clipboard data (clipboard takes ownership of the memory)
            // CF_UNICODETEXT = 13
            if (PInvoke.SetClipboardData(13, (Windows.Win32.Foundation.HANDLE)hGlobal).IsNull)
            {
                GlobalFreeManual(hGlobal);
                throw new InvalidOperationException("Could not set clipboard data.");
            }
        }
        finally
        {
            PInvoke.CloseClipboard();
        }
    }

    /// <summary>
    /// Simulates burst clicks by capturing a key and a delay from the user,
    /// then simulating the key press in a loop.
    /// NOTE: For this console application context, key capture is simulated via console input.
    /// A real GUI application would use UI elements for key and delay input.
    /// </summary>
    public static void SimulateBurstClicks()
    {
        Console.WriteLine("\n--- Burst Click Simulator ---");
        Console.WriteLine("Enter the VirtualKey code for the key to simulate (e.g., 65 for 'KEY_A', 32 for 'VK_SPACE'):");
        string? keyInput = Console.ReadLine();
        VirtualKey keyToSimulate;

        if (!Enum.TryParse(keyInput, true, out keyToSimulate))
        {
            Console.WriteLine("Invalid key entered. Please use a valid VirtualKey enum name (e.g., 65 for 'KEY_A', 32 for 'VK_SPACE').");
            return;
        }

        Console.WriteLine("Enter the delay between presses in milliseconds (e.g., 50, 100):");
        string? delayInput = Console.ReadLine();
        int delayMs;

        if (!int.TryParse(delayInput, out delayMs) || delayMs < 0)
        {
            Console.WriteLine("Invalid delay entered. Please enter a non-negative integer.");
            return;
        }

        Console.WriteLine("Enter the number of times to press the key (e.g., 10, 50):");
        string? timesInput = Console.ReadLine();
        int times;

        if (!int.TryParse(timesInput, out times) || times <= 0)
        {
            Console.WriteLine("Invalid number of times. Please enter a positive integer.");
            return;
        }

        Console.WriteLine($"Simulating {times} presses of {keyToSimulate} with {delayMs}ms delay. Press Ctrl+C to stop prematurely.");

        for (int i = 0; i < times; i++)
        {
            SimulateKeyPress(keyToSimulate);

            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }

        Console.WriteLine("Burst click simulation finished.");
    }
}
