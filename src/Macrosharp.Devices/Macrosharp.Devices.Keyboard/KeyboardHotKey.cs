using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Macrosharp.Devices.Core;

namespace Macrosharp.Devices.Keyboard;

/// <summary>Represents a single hotkey, composed of a main key and a combination of modifier keys.</summary>
public struct Hotkey : IEquatable<Hotkey>
{
    /// <summary>The main key of the hotkey (e.g., VirtualKey.Z for Ctrl+Shift+Z).</summary>
    public VirtualKey MainKey { get; }

    /// <summary>The bitmask representing the required modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</summary>
    public int HotKeyModifiers { get; }

    /// <summary>Initializes a new instance of the <see cref="Hotkey"/> struct.</summary>
    /// <param name="key">The main virtual key.</param>
    /// <param name="modifiers">The bitmask of required modifier keys (use <see cref="HotKeyModifiers"/> constants).</param>
    public Hotkey(VirtualKey key, int modifiers = 0)
    {
        MainKey = key;
        HotKeyModifiers = modifiers;
    }

    /// <summary>Compares two <see cref="Hotkey"/> instances for equality.</summary>
    public bool Equals(Hotkey other)
    {
        return MainKey == other.MainKey && HotKeyModifiers == other.HotKeyModifiers;
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    public override bool Equals(object? obj)
    {
        return obj is Hotkey other && Equals(other);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(MainKey, HotKeyModifiers);
    }

    /// <summary>Returns a string representation of the hotkey.</summary>
    public override string ToString()
    {
        string keyName = KeysMapper.GetDisplayName(MainKey, (HotKeyModifiers & Modifiers.SHIFT) != 0, false); // isCapsLockOn is false for display

        string registeredModifiersString = Modifiers.GetModifiersStringFromMask(HotKeyModifiers);

        return string.IsNullOrEmpty(registeredModifiersString) ? keyName
            : string.IsNullOrEmpty(keyName) ? registeredModifiersString
            : $"{registeredModifiersString}+{keyName}";
    }
}

/// <summary>Manages the registration and detection of global hotkeys.</summary>
public class HotkeyManager : IDisposable
{
    private readonly KeyboardHookManager _keyboardHookManager;

    // Stores registered hotkeys and their associated actions.
    private readonly Dictionary<Hotkey, Action> _registeredHotkeys = new();

    // To prevent a hotkey from firing repeatedly while held down.
    private Hotkey? _activeHotkey = null;

    /// <summary>Initializes a new instance of the <see cref="HotkeyManager"/> class.</summary>
    /// <param name="keyboardHookManager">An instance of the keyboard hook manager to subscribe to.</param>
    public HotkeyManager(KeyboardHookManager keyboardHookManager)
    {
        _keyboardHookManager = keyboardHookManager ?? throw new ArgumentNullException(nameof(keyboardHookManager));
        _keyboardHookManager.KeyDownHandler += OnKeyDown;
        _keyboardHookManager.KeyUpHandler += OnKeyUp;
    }

    /// <summary>Registers a new hotkey with an associated action.</summary>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey(VirtualKey key, int modifiers, Action action)
    {
        return RegisterInternal(key, modifiers, action);
    }

    /// <summary>Registers a new hotkey with an associated action and one bound argument.</summary>
    /// <typeparam name="T1">The type of the argument.</typeparam>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <param name="arg1">The argument bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey<T1>(VirtualKey key, int modifiers, Action<T1> action, T1 arg1)
    {
        return RegisterInternal(key, modifiers, () => action(arg1));
    }

    /// <summary>Registers a new hotkey with an associated action and two bound arguments.</summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <param name="arg1">The first argument bound at registration time.</param>
    /// <param name="arg2">The second argument bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey<T1, T2>(VirtualKey key, int modifiers, Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        return RegisterInternal(key, modifiers, () => action(arg1, arg2));
    }

    /// <summary>Registers a new hotkey with an associated action and three bound arguments.</summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <param name="arg1">The first argument bound at registration time.</param>
    /// <param name="arg2">The second argument bound at registration time.</param>
    /// <param name="arg3">The third argument bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey<T1, T2, T3>(VirtualKey key, int modifiers, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        return RegisterInternal(key, modifiers, () => action(arg1, arg2, arg3));
    }

    /// <summary>Registers a new hotkey with an associated action and four bound arguments.</summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument.</typeparam>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <param name="arg1">The first argument bound at registration time.</param>
    /// <param name="arg2">The second argument bound at registration time.</param>
    /// <param name="arg3">The third argument bound at registration time.</param>
    /// <param name="arg4">The fourth argument bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey<T1, T2, T3, T4>(VirtualKey key, int modifiers, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        return RegisterInternal(key, modifiers, () => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>Registers a new hotkey with an associated action and five bound arguments.</summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument.</typeparam>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="action">The action to execute when the hotkey is pressed.</param>
    /// <param name="arg1">The first argument bound at registration time.</param>
    /// <param name="arg2">The second argument bound at registration time.</param>
    /// <param name="arg3">The third argument bound at registration time.</param>
    /// <param name="arg4">The fourth argument bound at registration time.</param>
    /// <param name="arg5">The fifth argument bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    public bool RegisterHotkey<T1, T2, T3, T4, T5>(VirtualKey key, int modifiers, Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        return RegisterInternal(key, modifiers, () => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>Registers a new hotkey with a parameterless action.</summary>
    /// <param name="key">The main virtual key for the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys (e.g., Modifiers.CTRL | Modifiers.SHIFT).</param>
    /// <param name="boundAction">The parameterless action bound at registration time.</param>
    /// <returns>True if the hotkey was registered successfully; false if it was already registered.</returns>
    private bool RegisterInternal(VirtualKey key, int modifiers, Action boundAction)
    {
        Hotkey hotkey = new Hotkey(key, modifiers);
        if (_registeredHotkeys.ContainsKey(hotkey))
        {
            Console.WriteLine($"Hotkey '{hotkey}' is already registered.");
            return false;
        }

        _registeredHotkeys.Add(hotkey, boundAction);
        return true;
    }

    /// <summary>Un-registers a previously registered hotkey.</summary>
    /// <param name="key">The main virtual key of the hotkey.</param>
    /// <param name="modifiers">The bitmask of modifier keys.</param>
    /// <returns>True if the hotkey was unregistered successfully; false if it was not found.</returns>
    public bool UnregisterHotkey(VirtualKey key, int modifiers)
    {
        Hotkey hotkey = new Hotkey(key, modifiers);
        bool removed = _registeredHotkeys.Remove(hotkey);
        if (removed)
        {
            Console.WriteLine($"Unregistered hotkey: {hotkey}");
            if (_activeHotkey.HasValue && _activeHotkey.Value.Equals(hotkey))
            {
                _activeHotkey = null; // Clear active hotkey if it was the one being unregistered
            }
        }
        else
        {
            Console.WriteLine($"Hotkey '{hotkey}' not found for un-registration.");
        }
        return removed;
    }

    /// <summary>Handles key down events from the keyboard hook.</summary>
    private void OnKeyDown(object? sender, KeyboardEvent e)
    {
        // Don't process if the event has already been handled by another hook or application.
        if (e.Handled)
            return;

        // Ignore modifier key presses themselves when checking for hotkeys.
        if (Modifiers.ModifierKeys.Contains(e.KeyCode))
        {
            //TODO: Handle the tilde/backtick key specifically, being a modifier key causes it be sent multiple times.
            // if (e.KeyCode == VirtualKey.OEM_3)
            // {
            //     e.Handled = true;
            // }

            return; // No need to updated modifiers state as it's handled by the hook manager.
        }

        // Create a Hotkey object representing the current key press combined with active modifiers
        Hotkey hotkey = new Hotkey(e.KeyCode, Modifiers.CurrentModifiers);

        // Check if this combination matches any registered hotkey and it's not currently active
        if (_registeredHotkeys.TryGetValue(hotkey, out Action? action))
        {
            if (_activeHotkey == null || !_activeHotkey.Value.Equals(hotkey))
            {
                // This hotkey has not been activated yet since the last key up, or it's a new hotkey.
                // Execute the action.
                Console.WriteLine($"Hotkey '{hotkey}' pressed. Executing action...");
                action.Invoke();
                e.Handled = true; // Mark as handled to suppress the key press
                _activeHotkey = hotkey; // Mark this hotkey as active
            }
            else
            {
                // Hotkey is already active (held down), do not re-trigger action
                // Optionally, you might still want to handle the key to suppress repeat
                // e.Handled = true;
                Console.WriteLine($"Hotkey '{hotkey}' is already active. Ignoring repeat press.");
            }
        }
    }

    /// <summary>Handles key up events from the keyboard hook.</summary>
    private void OnKeyUp(object? sender, KeyboardEvent e)
    {
        // When any key is released, clear the active hotkey if it was the key that was released.
        // This allows the same hotkey to be pressed again.
        if (_activeHotkey.HasValue && _activeHotkey.Value.MainKey == e.KeyCode)
        {
            _activeHotkey = null;
        }
    }

    /// <summary>Disposes the <see cref="HotkeyManager"/> and unsubscribes from keyboard hook events.</summary>
    public void Dispose()
    {
        if (_keyboardHookManager != null)
        {
            _keyboardHookManager.KeyDownHandler -= OnKeyDown;
            _keyboardHookManager.KeyUpHandler -= OnKeyUp;
        }
        _registeredHotkeys.Clear();
        _activeHotkey = null;

        Console.WriteLine("HotkeyManager disposed.");
    }
}
