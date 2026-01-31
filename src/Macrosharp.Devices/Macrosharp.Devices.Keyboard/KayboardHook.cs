using System.Runtime.InteropServices;
using Macrosharp.Devices.Core;
using Windows.Win32; // For PInvoke access to generated constants and methods
using Windows.Win32.Foundation; // For LPARAM, WPARAM, LRESULT, HHOOK, BOOL, HINSTANCE
using Windows.Win32.UI.WindowsAndMessaging; // For KBDLLHOOKSTRUCT, MSG, WM_*, WINDOWS_HOOK_ID enum

namespace Macrosharp.Devices.Keyboard;

/// <summaryRepresents the state of a key (down or up)</summary>
public enum KeyState
{
    Down,
    Up,
}

/// <summary>Event arguments for keyboard events, including the key and its state. Also includes a Handled property to suppress the key event.</summary>
public class KeyboardEvent : EventArgs
{
    /// <summary>Gets the virtual key code of the key that was pressed or released.</summary>
    public VirtualKey KeyCode { get; private set; }

    /// <summary>Gets the state of the key (Down or Up).</summary>
    public KeyState State { get; private set; }

    /// <summary>Gets or sets a value indicating whether the key event has been handled. If set to true, the key press will be suppressed and not passed to other applications.</summary>
    public bool Handled { get; set; }

    /// <summary>The raw flags value from KBDLLHOOKSTRUCT.</summary>
    public uint Flags { get; private set; }

    /// <summary>True if the key is an extended key (bit 0).</summary>
    public bool IsExtendedKey => (Flags & 0x01) != 0;

    /// <summary>True if the event was injected from a process running at lower integrity level (bit 1).</summary>
    // public bool IsLowerIntegrityInjected => (Flags & 0x02) != 0;

    /// <summary>True if the event was injected (bit 4).</summary>
    public bool IsInjected => (Flags & 0x10) != 0;

    /// <summary>True if the ALT key is pressed (bit 5).</summary>
    public bool IsAltDown => (Flags & 0x20) != 0;

    /// <summary>True if the key is being released (bit 7).</summary>
    // public bool IsKeyUp => (Flags & 0x80) != 0;

    /// <summary>True if the key is a transition key (bit 8).</summary>
    public bool IsTransitionKey => (Flags & 0x100) != 0;

    /// <summary>True if the Shift key is pressed.</summary>
    public bool IsShiftDown { get; private set; }

    /// <summary>Initializes a new instance of the <see cref="KeyboardEvent"/> class.</summary>
    /// <param name="key">The virtual key code.</param>
    /// <param name="state">The state of the key (Down or Up).</param>
    /// <param name="isShiftDown">Indicates if the Shift key was down during this event.</param>
    /// <param name="flags">The flags value from KBDLLHOOKSTRUCT.</param>
    public KeyboardEvent(VirtualKey key, KeyState state, bool isShiftDown, uint flags)
    {
        KeyCode = key;
        State = state;
        IsShiftDown = isShiftDown;
        Flags = flags;
        Handled = false;
    }

    /// <summary>Returns a string representation of the keyboard event.</summary>
    public override string ToString()
    {
        // var scanCode = PInvoke.MapVirtualKey((uint)KeyCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
        var isCapsLockOn = Modifiers.IsCapsLockOn;
        return $"KEY={KeysMapper.GetDisplayName(KeyCode, IsShiftDown, isCapsLockOn)}, VK={(int)KeyCode}, AS={KeysMapper.GetAsciiCode(KeyCode, IsShiftDown, isCapsLockOn)} | Shift={IsShiftDown}, ALT={IsAltDown} | Ext={IsExtendedKey}, Inj={IsInjected}";
    }
}

/// <summary>Manages a global low-level keyboard hook. This allows intercepting keyboard events across all applications.</summary>
public class KeyboardHookManager : IDisposable
{
    private HOOKPROC _proc;
    private HHOOK _hookID = default;

    /// <summary>Event that fires when a key is pressed down.</summary>
    public event EventHandler<KeyboardEvent>? KeyDownHandler;

    /// <summary>Event that fires when a key is released.</summary>
    public event EventHandler<KeyboardEvent>? KeyUpHandler;

    /// <summary>Initializes a new instance of the <see cref="KeyboardHookManager"/> class.</summary>
    public KeyboardHookManager()
    {
        _proc = HookCallback;
    }

    /// <summary>Installs the global keyboard hook.</summary>
    public void Start()
    {
        if (!_hookID.IsNull)
        {
            return; // Hook is already installed.
        }

        HINSTANCE hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
        if (hInstance == HINSTANCE.Null)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        _hookID = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD_LL, _proc, hInstance, 0);

        if (_hookID.IsNull)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>Uninstalls the global keyboard hook.</summary>
    public void Stop()
    {
        if (!_hookID.IsNull)
        {
            BOOL success = PInvoke.UnhookWindowsHookEx(_hookID);
            _hookID = default; // Reset the hook ID.

            if (success.Value == 0)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    /// <summary>The callback function for the low-level keyboard hook. This method is called by Windows whenever a keyboard event occurs.</summary>
    /// <param name="nCode">A hook code the hook procedure uses to determine how to process the message.</param>
    /// <param name="wParam">The identifier of the keyboard message.</param>
    /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
    /// <returns>The result of the next hook in the chain.</returns>
    private LRESULT HookCallback(int nCode, WPARAM wParam, LPARAM lParam)
    {
        // If nCode is less than HC_ACTION (0), the hook procedure must pass the message to the next hook in the chain without processing it.
        if (nCode < PInvoke.HC_ACTION)
            return PInvoke.CallNextHookEx(_hookID, nCode, wParam, lParam);

        KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)!;

        // Check if shift key is pressed:
        bool isShiftDown = (PInvoke.GetKeyState((int)VirtualKey.SHIFT) & 0x8000) != 0;

        if (wParam.Value == PInvoke.WM_KEYDOWN || wParam.Value == PInvoke.WM_SYSKEYDOWN)
        {
            KeyboardEvent kbEvent = new KeyboardEvent((VirtualKey)hookStruct.vkCode, KeyState.Down, isShiftDown, (uint)hookStruct.flags);

            Modifiers.UpdateModifierState(kbEvent);

            KeyDownHandler?.Invoke(this, kbEvent);
            if (kbEvent.Handled)
            {
                return (LRESULT)1; // Suppress the key event
            }
        }
        else if (wParam.Value == PInvoke.WM_KEYUP || wParam.Value == PInvoke.WM_SYSKEYUP)
        {
            KeyboardEvent kbEvent = new KeyboardEvent((VirtualKey)hookStruct.vkCode, KeyState.Up, isShiftDown, (uint)hookStruct.flags);

            Modifiers.UpdateModifierState(kbEvent);

            KeyUpHandler?.Invoke(this, kbEvent);
            if (kbEvent.Handled)
            {
                return (LRESULT)1;
            }
        }

        return PInvoke.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    /// <summary>Disposes of the hook and releases resources.</summary>
    public void Dispose()
    {
        Stop();
    }
}

// csharpier-ignore-start
/// <summary>A static class that stores and manages the current state of keyboard modifier and lock keys. Does not differentiate between left and right modifier keys.</summary>
public static class Modifiers
{
    // A hash set of all modifier keys.
    public static readonly HashSet<VirtualKey> ModifierKeys = new()
    {
        VirtualKey.LCONTROL, VirtualKey.RCONTROL, VirtualKey.CONTROL,
        VirtualKey.LSHIFT, VirtualKey.RSHIFT, VirtualKey.SHIFT,
        VirtualKey.LMENU, VirtualKey.RMENU, VirtualKey.MENU,
        VirtualKey.LWIN, VirtualKey.RWIN, VirtualKey.OEM_3
    };

    // Modifier keys masks
    // public const int FN = 1 << 5;    // Custom FN key
    public const int CTRL = 1 << 4;     // Represents either LCTRL or RCTRL
    public const int SHIFT = 1 << 3;    // Represents either LSHIFT or RSHIFT
    public const int ALT = 1 << 2;      // Represents either LALT or RALT
    public const int WIN = 1 << 1;      // Represents either LWIN or RWIN
    public const int BACKTICK = 1 << 0; // OEM_3 key (usually the backtick key, `~)

    // Combined modifier masks
    public const int CTRL_SHIFT_ALT = CTRL | SHIFT | ALT;
    // public const int CTRL_ALT_WIN_FN = CTRL | ALT | WIN | FN;
    public const int CTRL_ALT_WIN = CTRL | ALT | WIN;
    // public const int CTRL_WIN_FN = CTRL | WIN | FN;
    public const int CTRL_SHIFT = CTRL | SHIFT;
    public const int CTRL_ALT = CTRL | ALT;
    // public const int CTRL_FN = CTRL | FN;
    public const int CTRL_WIN = CTRL | WIN;
    public const int CTRL_BACKTICK = CTRL | BACKTICK;
    // public const int LCTRL_RCTRL = LCTRL | RCTRL;
    public const int SHIFT_ALT = SHIFT | ALT;
    // public const int SHIFT_FN = SHIFT | FN;
    public const int SHIFT_BACKTICK = SHIFT | BACKTICK;
    // public const int LSHIFT_RSHIFT = LSHIFT | RSHIFT;
    // public const int ALT_FN = ALT | FN;
    public const int ALT_BACKTICK = ALT | BACKTICK;
    // public const int LALT_RALT = LALT | RALT;
    // public const int WIN_FN = WIN | FN;
    public const int WIN_BACKTICK = WIN | BACKTICK;
    // public const int LWIN_RWIN = LWIN | RWIN;
    // public const int FN_BACKTICK = FN | BACKTICK;

    /// <summary>An integer packing the states of the keyboard modifier keys (pressed or not).</summary>
    public static int CurrentModifiers { get; private set; } = 0;

    // Lock key states
    public static bool IsCapsLockOn => (PInvoke.GetKeyState((int)VirtualKey.CAPITAL) & 0x0001) != 0;
    public static bool IsScrollLockOn => (PInvoke.GetKeyState((int)VirtualKey.SCROLL) & 0x0001) != 0;
    public static bool IsNumLockOn => (PInvoke.GetKeyState((int)VirtualKey.NUMLOCK) & 0x0001) != 0;

    /// <summary>Updates the state of the modifier keys based on a keyboard event. Should be called from the keyboard hook's callback.</summary>
    /// <param name="e">The keyboard event.</param>
    public static void UpdateModifierState(KeyboardEvent e)
    {
        int modifierMask = 0;

        switch (e.KeyCode)
        {
            case VirtualKey.LCONTROL:
            case VirtualKey.RCONTROL:
            case VirtualKey.CONTROL: // VirtualKey.CONTROL is rarely sent by hardware, but good to include
                modifierMask = CTRL;
                break;
            case VirtualKey.LSHIFT:
            case VirtualKey.RSHIFT:
            case VirtualKey.SHIFT:
                modifierMask = SHIFT;
                break;
            case VirtualKey.LMENU: // LALT
            case VirtualKey.RMENU: // RALT
            case VirtualKey.MENU:  // General ALT
                modifierMask = ALT;
                break;
            case VirtualKey.LWIN:
            case VirtualKey.RWIN:
                modifierMask = WIN;
                break;
            case VirtualKey.OEM_3:
                modifierMask = BACKTICK;
                break;
            // case (VirtualKey)255:
            //     modifierMask = FN;
            //     break;
            default:
                // Not a recognized modifier key that should toggle our flags.
                //TODO: Log unrecognized keys here
                return;
        }

        if (modifierMask != 0)
        {
            if (e.State == KeyState.Down)
            {
                CurrentModifiers |= modifierMask;
            }
            else
            {
                CurrentModifiers &= ~modifierMask;
            }
        }
    }

    /// <summary>Checks if a specific modifiers combination (represented by a bitmask) is currently pressed.</summary>
    /// <param name="modifiersMask">The modifiers mask to check against (e.g., Modifiers.CTRL, Modifiers.CTRL_SHIFT_ALT).</param>
    /// <returns>True if the specified modifiers are currently pressed, false otherwise.</returns>
    public static bool HasModifier(int modifiersMask)
    {
        return (CurrentModifiers & modifiersMask) == modifiersMask;
    }

    /// <summary>
    /// Returns a boolean array representing the states of the main modifier keys.
    /// </summary>
    /// <returns>[CTRL, SHIFT, ALT, WIN, BACKTICK]</returns>
    public static bool[] GetMainModifiersStates()
    {
        return new bool[]
        {
            HasModifier(CTRL),
            HasModifier(SHIFT),
            HasModifier(ALT),
            HasModifier(WIN),
            HasModifier(BACKTICK),
            // HasModifier(FN),
        };
    }

    /// <summary>Returns a string representation of the current modifier keys.</summary>
    /// <returns>A string containing the names of the currently pressed modifier keys, separated by '+' symbols.</returns>
    public static string GetModifiersStringFromMask(int modifierMask)
    {
        var modifiers = new System.Text.StringBuilder();

        if ((modifierMask & Modifiers.CTRL) != 0) modifiers.Append("Ctrl+");
        if ((modifierMask & Modifiers.SHIFT) != 0) modifiers.Append("Shift+");
        if ((modifierMask & Modifiers.ALT) != 0) modifiers.Append("Alt+");
        if ((modifierMask & Modifiers.WIN) != 0) modifiers.Append("Win+");
        if ((modifierMask & Modifiers.BACKTICK) != 0) modifiers.Append("Backtick+");
        // if (HasModifier(FN)) modifiers.Append("FN+");

        // Remove the trailing '+' if any
        if (modifiers.Length > 0)
            modifiers.Length -= 1;

        return modifiers.ToString();
    }

    /// <summary>Calculates the combined modifier bitmask from a list of modifier names.</summary>
    /// <param name="modifierNames">A list of string modifier names (e.g., ["Ctrl", "Alt"]).</param>
    /// <returns>An integer representing the combined bitmask of the modifiers.</returns>
    public static int GetModifierMaskFromModifierNames(List<string> modifierNames)
    {
        // Mapping from common modifier names to their bitmask values defined in Modifiers class.
        // This map is now local to this method or could be a static field within Hotkey.
        var _modifierMaskMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ctrl", CTRL },
            { "Shift", SHIFT },
            { "Alt", ALT },
            { "Win", WIN },
            { "Backtick", BACKTICK },
        };

        int mask = 0;
        foreach (var name in modifierNames)
        {
            if (_modifierMaskMap.TryGetValue(name, out int modifierBit))
            {
                mask |= modifierBit;
            }
            else
            {
                Console.WriteLine($"Warning: Unknown modifier name '{name}'. Ignoring this modifier.");
            }
        }
        return mask;
    }
}
// csharpier-ignore-end
