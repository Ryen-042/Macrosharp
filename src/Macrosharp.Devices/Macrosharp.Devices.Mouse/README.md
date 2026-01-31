# Macrosharp.Devices.Mouse

Global low-level mouse hook manager for capturing mouse input system-wide on Windows.

## Overview

This module provides a `WH_MOUSE_LL` hook implementation that captures mouse events globally (independent of application focus), enabling custom actions bound to:

- Single & multi-button presses
- Scroll wheel events (vertical and horizontal)
- Button combinations (e.g., Left+Right pressed together)
- Scroll-while-holding-button actions (e.g., scroll while holding Right)

## Architecture

| Class | Purpose |
|-------|---------|
| `MouseHookManager` | Core hook lifecycle management (`Start`/`Stop`/`Dispose`) |
| `MouseEvent` | Event args containing position, button, wheel delta, flags |
| `MouseButtonModifiers` | Static button state tracker (like keyboard `Modifiers`) |
| `MouseBindingManager` | High-level API for registering button/scroll bindings |
| `MouseBinding` | Struct representing a binding configuration |

## Quick Start

```csharp
using Macrosharp.Devices.Mouse;
using Macrosharp.Devices.Core;

// Create and start the hook
var mouseHook = new MouseHookManager();
var bindingManager = new MouseBindingManager(mouseHook);

// 1. Single button action
bindingManager.RegisterBinding(MouseButtons.Middle, () =>
    Console.WriteLine("Middle click!"));

// 2. Multi-button combination (Left+Right together)
bindingManager.RegisterBinding(
    triggerButton: MouseButtons.Left,
    heldButtons: MouseButtons.Right,
    action: () => Console.WriteLine("Left+Right combo!"));

// 3. Scroll while holding a button
bindingManager.RegisterScrollBinding(
    direction: ScrollDirection.Vertical,
    heldButtons: MouseButtons.Right,
    action: delta => Console.WriteLine($"Scroll {delta} while holding Right"));

// Start capturing
mouseHook.Start();

// ... application runs ...

// Cleanup
bindingManager.Dispose();
mouseHook.Dispose();
```

## API Reference

### MouseHookManager

The core hook manager that installs/uninstalls the Windows low-level mouse hook.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CaptureMouseMove` | `bool` | `false` | Enable mouse move events (high-frequency, disabled by default) |

#### Events

| Event | Description |
|-------|-------------|
| `MouseButtonDownHandler` | Fires when any button is pressed |
| `MouseButtonUpHandler` | Fires when any button is released |
| `MouseScrollHandler` | Fires on vertical or horizontal scroll |
| `MouseMoveHandler` | Fires on mouse movement (only if `CaptureMouseMove = true`) |

#### Methods

| Method | Description |
|--------|-------------|
| `Start()` | Installs the global mouse hook |
| `Stop()` | Uninstalls the hook |
| `Dispose()` | Calls `Stop()` and releases resources |

### MouseEvent

Event arguments passed to hook event handlers.

| Property | Type | Description |
|----------|------|-------------|
| `X`, `Y` | `int` | Screen coordinates |
| `EventType` | `MouseEventType` | Type of event (LeftDown, RightUp, Scroll, etc.) |
| `Button` | `MouseButtons` | The button involved (flags enum) |
| `ButtonState` | `MouseButtonState?` | Down or Up (null for scroll/move) |
| `WheelDelta` | `short` | Wheel delta for scroll events (+120 = up, -120 = down) |
| `ScrollDirection` | `ScrollDirection?` | Vertical or Horizontal |
| `Flags` | `uint` | Raw MSLLHOOKSTRUCT flags |
| `Timestamp` | `uint` | System time in milliseconds |
| `IsInjected` | `bool` | True if event was injected programmatically |
| `Handled` | `bool` | Set to `true` to suppress the event |

### MouseButtonModifiers

Static class tracking currently pressed buttons (like keyboard `Modifiers`).

| Member | Description |
|--------|-------------|
| `CurrentButtons` | Current button state as bitmask |
| `HasButton(MouseButtons)` | Check if button(s) are pressed |
| `HasExactButtons(int)` | Check for exact button combination |
| `GetButtonsStringFromMask(int)` | "Left+Right" style string |
| `Reset()` | Clear all button states |

#### Constants

```csharp
MouseButtonModifiers.LEFT          // 1
MouseButtonModifiers.RIGHT         // 2
MouseButtonModifiers.MIDDLE        // 4
MouseButtonModifiers.XBUTTON1      // 8
MouseButtonModifiers.XBUTTON2      // 16
MouseButtonModifiers.LEFT_RIGHT    // 3 (Left + Right)
```

### MouseBindingManager

High-level binding registration API.

#### Button Bindings

```csharp
// Simple button
RegisterBinding(MouseButtons button, Action action)

// With held buttons
RegisterBinding(MouseButtons trigger, MouseButtons held, Action action)
RegisterBinding(MouseButtons trigger, int heldMask, Action action)

// Unregister
UnregisterBinding(MouseButtons trigger, int heldMask = 0)
```

#### Scroll Bindings

```csharp
// Simple scroll
RegisterScrollBinding(ScrollDirection dir, Action<short> action)

// With held buttons
RegisterScrollBinding(ScrollDirection dir, MouseButtons held, Action<short> action)
RegisterScrollBinding(ScrollDirection dir, int heldMask, Action<short> action)

// Unregister
UnregisterScrollBinding(ScrollDirection dir, int heldMask = 0)
```

## Enums

### MouseButtons (Flags)

```csharp
None     = 0
Left     = 1
Right    = 2
Middle   = 4
XButton1 = 8
XButton2 = 16
```

### ScrollDirection

```csharp
Vertical   = 0  // Standard wheel
Horizontal = 1  // Tilt wheel
```

### MouseEventType

```csharp
LeftDown, LeftUp, RightDown, RightUp,
MiddleDown, MiddleUp, XButton1Down, XButton1Up,
XButton2Down, XButton2Up, Scroll, HorizontalScroll, Move
```

## Event Suppression

Set `Handled = true` on a `MouseEvent` to prevent it from reaching other applications:

```csharp
mouseHook.MouseButtonDownHandler += (sender, e) =>
{
    if (e.Button == MouseButtons.Middle)
    {
        e.Handled = true; // Suppress middle clicks globally
        DoCustomAction();
    }
};
```

## Performance Considerations

1. **Mouse Move Events**: Disabled by default (`CaptureMouseMove = false`) because they fire 100+ times/second during movement. Enable only if needed.

2. **Non-blocking Actions**: `MouseBindingManager` executes actions via `Task.Run()` to avoid blocking the hook callback. For low-level handlers, keep processing minimal.

3. **Hook Latency**: The hook is installed with `WH_MOUSE_LL` which processes in-process. Events typically add <1ms latency.

## Thread Safety

- Button state in `MouseButtonModifiers` is updated synchronously in the hook callback
- `MouseBindingManager` executes actions on background tasks to avoid blocking
- Hook installation/uninstallation should be done from the UI thread if applicable

## Cleanup

Always dispose managers before application exit to ensure the hook is uninstalled:

```csharp
// Recommended pattern
using var mouseHook = new MouseHookManager();
using var bindingManager = new MouseBindingManager(mouseHook);

mouseHook.Start();
// ... application runs ...
// Dispose called automatically
```

## Known Limitations

- **Double-click detection**: Not built-in. Track timestamps in your handler if needed.
- **Modifier keys**: Keyboard modifiers (Ctrl, Alt, etc.) are not tracked. Use `Macrosharp.Devices.Keyboard.Modifiers` if needed.
- **UAC elevation**: Some elevated windows may not receive hooked events from non-elevated processes.
