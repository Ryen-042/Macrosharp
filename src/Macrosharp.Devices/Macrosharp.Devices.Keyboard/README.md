# Macrosharp.Devices.Keyboard

A Windows keyboard automation library providing global keyboard hooks, hotkey management, and text expansion functionality using Win32 APIs.

## Features

- **Global Keyboard Hook** - Low-level keyboard hook (`WH_KEYBOARD_LL`) to intercept keyboard events system-wide
- **Hotkey Management** - Register and handle global hotkeys with modifier key combinations
- **Text Expansion** - Trigger text expansions based on typed abbreviations
- **Modifier Key Tracking** - Track the state of Ctrl, Shift, Alt, Win, and Backtick modifier keys

## Components

### KeyboardHookManager

Manages a global low-level keyboard hook to intercept all keyboard events.

```csharp
using var hookManager = new KeyboardHookManager();
hookManager.KeyDownHandler += (sender, e) =>
{
    Console.WriteLine($"Key down: {e.KeyCode}");
    if (e.KeyCode == VirtualKey.ESCAPE)
        e.Handled = true; // Suppress the key
};
hookManager.Start();
```

### HotkeyManager

Registers and handles global hotkeys with modifier combinations.

```csharp
var hotkeyManager = new HotkeyManager(keyboardHookManager);

// Register Ctrl+Shift+A
hotkeyManager.RegisterHotkey(VirtualKey.KEY_A, Modifiers.CTRL | Modifiers.SHIFT, () =>
{
    Console.WriteLine("Ctrl+Shift+A pressed!");
});

// Register with bound arguments
hotkeyManager.RegisterHotkey(VirtualKey.KEY_B, Modifiers.CTRL, 
    (message) => Console.WriteLine(message), "Hello from hotkey!");
```

### Modifiers

Static class for tracking and querying modifier key states.

```csharp
// Check if Ctrl+Shift is pressed
if (Modifiers.HasModifier(Modifiers.CTRL_SHIFT))
{
    // Handle Ctrl+Shift combination
}

// Check lock key states
bool capsOn = Modifiers.IsCapsLockOn;
bool numLockOn = Modifiers.IsNumLockOn;

// Get modifier string from mask
string modString = Modifiers.GetModifiersStringFromMask(Modifiers.CTRL_ALT); // "Ctrl+Alt"
```

## Subdirectories

### HotkeyBindings/

Contains configuration management for persisting hotkey definitions:
- `HotkeyAction.cs` - Represents an action bound to a hotkey
- `HotkeyConfigurationManager.cs` - Loads/saves hotkey configurations
- `HotkeyDefinition.cs` - Defines a hotkey with its key, modifiers, and action

### TextExpansion/

Text expansion functionality for replacing abbreviations with full text:
- `TextExpansionManager.cs` - Manages text expansion rules and detection
- `TextExpansionBuffer.cs` - Buffers typed characters for pattern matching
- `TextExpansionRule.cs` - Defines expansion rules (trigger → replacement)
- `TextExpansionConfigurationManager.cs` - Configuration persistence
- `PlaceholderProcessor.cs` - Processes placeholders in expansion text

## Dependencies

- `Macrosharp.Devices.Core` - Core types including `VirtualKey` enum and `KeysMapper`
- `Microsoft.Windows.CsWin32` - P/Invoke source generator for Win32 APIs

## Usage Example

```csharp
// Initialize keyboard hook and hotkey manager
using var keyboardHook = new KeyboardHookManager();
using var hotkeyManager = new HotkeyManager(keyboardHook);

// Register hotkeys
hotkeyManager.RegisterHotkey(VirtualKey.KEY_Q, Modifiers.CTRL_SHIFT, () =>
{
    Console.WriteLine("Quick action triggered!");
});

// Start listening
keyboardHook.Start();

// Run message loop (for console apps)
Application.Run(); // or your message loop
```

## Notes

- Requires Windows with access to low-level keyboard hooks
- Some applications with UI Access may not receive simulated input
- Hotkeys marked as `Handled = true` will be suppressed system-wide
