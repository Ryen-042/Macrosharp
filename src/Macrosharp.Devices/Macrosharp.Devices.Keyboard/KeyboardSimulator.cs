using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse; // For INPUT, KEYBDINPUT, MOUSE_EVENT_FLAGS, VirtualKey

namespace Macrosharp.Devices.Keyboard;

/// <summary>Provides methods for simulating keyboard input, including key presses, sequences, and hotkeys.</summary>
public static class KeyboardSimulator
{
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
