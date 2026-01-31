# Macrosharp.UserInterfaces.DynamicWindow

A lightweight Windows dialog library for creating dynamic input forms with optional keyboard key capture functionality. Built entirely with Win32 APIs, requiring no WinForms or WPF dependencies.

## Features

- **Dynamic Input Forms** - Create dialogs with any number of labeled input fields
- **Key Capture** - Optional keyboard key capture for hotkey configuration
- **Modifier Key Support** - Capture key combinations with Ctrl, Shift, Alt, and Win modifiers
- **Key Sequence Capture** - Capture up to 3 key presses in a single sequence
- **DPI Aware** - Proper scaling on high-DPI displays
- **Always-On-Top** - Window stays on top for quick input scenarios
- **Resizable** - Window width adjusts to content with constraints

## Components

### SimpleWindow

The main class for creating dynamic input dialogs.

```csharp
var window = new SimpleWindow(
    title: "Configure Hotkey",
    labelWidth: 150,
    inputFieldWidth: 250,
    itemsHeight: 20,
    xSep: 10,
    ySep: 10,
    xOffset: 10,
    yOffset: 10
);

// Create dialog with input fields
var labels = new List<string> { "Name:", "Description:" };
var placeholders = new List<string> { "My Hotkey", "Optional description" };

window.CreateDynamicInputWindow(labels, placeholders, enableKeyCapture: true);

// After dialog closes, retrieve user inputs
foreach (var input in window.userInputs)
{
    Console.WriteLine(input);
}

// If key capture was enabled, retrieve captured key info
Console.WriteLine($"Key: {window.capturedKeyName}");
Console.WriteLine($"VK Code: {window.capturedKeyVK}");
Console.WriteLine($"Scan Code: {window.capturedKeyScanCode}");
Console.WriteLine($"Sequence: {window.capturedKeySequence}");
```

## Usage Examples

### Basic Input Dialog

```csharp
var window = new SimpleWindow("Enter Details");

var labels = new List<string>
{
    "First Name:",
    "Last Name:",
    "Email:"
};

window.CreateDynamicInputWindow(labels);

// Retrieve inputs after dialog closes
string firstName = window.userInputs[0];
string lastName = window.userInputs[1];
string email = window.userInputs[2];
```

### With Placeholders

```csharp
var window = new SimpleWindow("User Registration");

var labels = new List<string> { "Username:", "Password:" };
var placeholders = new List<string> { "john_doe", "••••••••" };

window.CreateDynamicInputWindow(labels, placeholders);
```

### With Key Capture

```csharp
var window = new SimpleWindow("Register Hotkey");

var labels = new List<string> { "Action Name:" };
var placeholders = new List<string> { "Toggle Window" };

// Enable key capture mode
window.CreateDynamicInputWindow(labels, placeholders, enableKeyCapture: true);

// User can click "Press to Capture Key" to start capturing
// Then press keys (with optional modifiers) and click "Finish"

if (!string.IsNullOrEmpty(window.capturedKeySequence))
{
    Console.WriteLine($"Captured: {window.capturedKeySequence}");
    // Example output: "Ctrl+Shift+A" or "Ctrl+A, B, C" for sequences
}
```

## Key Capture Workflow

1. Click the **"Press to Capture Key"** button
2. Press a key (optionally with Ctrl, Shift, Alt, or Win modifiers)
3. The key combination is displayed in the status field
4. Optionally press more keys to create a sequence (up to 3 keys)
5. Click **"Finish capture"** to confirm
6. Press **Escape** to cancel capture mode

### Captured Key Properties

| Property | Description |
|----------|-------------|
| `capturedKeyVK` | Virtual key code of the last captured key |
| `capturedKeyScanCode` | Hardware scan code of the last captured key |
| `capturedKeyName` | Display name of the last captured key |
| `capturedKeySequence` | Full sequence string (e.g., "Ctrl+A, B") |

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Click OK button |
| `Escape` | Close dialog / Cancel key capture |
| `Tab` | Navigate between input fields |

## Constructor Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | string | - | Window title |
| `labelWidth` | int | 150 | Width of label column in pixels |
| `inputFieldWidth` | int | 250 | Width of input fields in pixels |
| `itemsHeight` | int | 20 | Height of each input row |
| `xSep` | int | 10 | Horizontal spacing between elements |
| `ySep` | int | 10 | Vertical spacing between rows |
| `xOffset` | int | 10 | Left/right margin |
| `yOffset` | int | 10 | Top/bottom margin |

## Dependencies

- `Macrosharp.Devices.Core` - Core types including `VirtualKey` and `KeysMapper`
- `Microsoft.Windows.CsWin32` - P/Invoke source generator for Win32 APIs

## Implementation Notes

- Uses standard Win32 window classes (`STATIC`, `EDIT`, `BUTTON`)
- Window is created with `WS_EX_TOPMOST` for always-on-top behavior
- Uses `SetProcessDpiAwareness` for proper DPI scaling
- Message loop runs until window is destroyed
- All captured modifier keys follow the order: Ctrl → Shift → Alt → Win
