# Macrosharp — Feature Audit & Status

> Generated from a full source code audit. This document catalogs every feature,
> component, and subsystem found in the project, along with its implementation
> status and noteworthy details.

---

## 1. System Tray Icon (`Macrosharp.UserInterfaces.TrayIcon`)

| Item | Detail |
|------|--------|
| **Class** | `TrayIconHost` |
| **Status** | ✅ Fully implemented |

### What it does
Displays a persistent system-tray (notification area) icon with a rich popup
context menu. Supports dynamic icon swapping, tooltips, single-click, and
double-click discriminated actions.

### How it works
- Runs on a dedicated STA thread with a hidden message-only window.
- Uses GUID-based `NOTIFYICONDATAW` for stable icon identity across Explorer
  restarts (listens for `TaskbarCreated`).
- Menu items support icons (32×32 bitmaps via `CreateMenuBitmap`), submenus,
  separators, and click/double-click discrimination (250 ms timer, 300 ms
  suppression).
- `IconCycler` provides round-robin cycling through available icon paths.

### Current usage in Program.cs
Instantiated directly in `Main()` with a richer menu tree including:
Open Running Folder, Show Notification (9 toast variants), Notifications & Sounds
(show/hide notifications, mute/unmute reminder sounds, terminal keystroke visibility),
Burst Click (start with key capture, stop on demand),
Switch Icon, Show Hotkeys, Reload, Reminders (reload/add/edit/delete),
Configuration (open configs/project folder), and Clear Console Logs.

---

## 2. Toast Notifications (`Macrosharp.UserInterfaces.ToastNotifications`)

| Item | Detail |
|------|--------|
| **Class** | `ToastNotificationHost` |
| **Status** | ✅ Fully implemented |

### What it does
Sends Windows 10/11 toast notifications via WinRT. Supports simple text, long
duration, attribution, alarm/reminder scenarios, app logo override, progress
bars (indeterminate and determinate), and up to 5 action buttons.

### How it works
- Registers a custom AUMID (`"Macrosharp.Desktop"`) via `SetCurrentProcessExplicitAppUserModelID`.
- Creates a Start Menu `.lnk` shortcut stamped with the AUMID via COM
  `IPropertyStore` so the OS can route activations back.
- Builds toast XML with `ToastXmlBuilder`, creates `ToastNotification` from
  the XML doc, and shows via `ToastNotifier`.
- Fires `Activated`, `Dismissed`, `Failed` events. `Activated` carries the
  button argument string for dispatch.

### Current usage in Program.cs
Created in `Main()`. The `Activated` handler dispatches
`action=quit`, `action=open-folder`, `action=snooze`. The tray menu "Show
Notification" submenu exercises all notification types including the
"With Action Buttons" variant.

---

## 3. Keyboard Hook (`Macrosharp.Devices.Keyboard`)

| Item | Detail |
|------|--------|
| **Class** | `KeyboardHookManager` |
| **Status** | ✅ Fully implemented |

### What it does
Installs a global low-level keyboard hook (`WH_KEYBOARD_LL`) to intercept all
key-down and key-up events system-wide.

### How it works
- `Start()` calls `SetWindowsHookEx`. `Stop()` calls `UnhookWindowsHookEx`.
- The hook callback creates `KeyboardEvent` objects and fires `KeyDownHandler`
  / `KeyUpHandler` events.
- Updates the `Modifiers` static class on every modifier key press/release.
- Supports `KeyboardEvent.Handled` to suppress key propagation (returns 1
  from the hook to swallow the event).

### Modifiers class
Static modifier state tracker. Treats `OEM_3` (backtick/tilde) as a modifier
alongside Ctrl, Shift, Alt, Win. Provides bitmask constants and compound
masks (`CTRL_SHIFT`, `CTRL_ALT_WIN`, etc.). Tracks lock key states
(`IsCapsLockOn`, `IsScrollLockOn`, `IsNumLockOn`) via `GetKeyState`.

---

## 4. Hotkey Manager (`Macrosharp.Devices.Keyboard`)

| Item | Detail |
|------|--------|
| **Class** | `HotkeyManager` |
| **Status** | ✅ Fully implemented |

### What it does
Registry of key+modifier combinations mapped to `Action` callbacks. When the
keyboard hook fires a key-down whose `(VirtualKey, CurrentModifiers)` matches a
registered `Hotkey`, the action is invoked and the key event is suppressed.

### How it works
- `RegisterHotkey()` stores `Hotkey → Action` in a dictionary.
- `RegisterConditionalHotkey()` supports guard predicates (`Func<bool>`) so
  hotkeys can pass through when conditions are not met.
- `RegisterRepeatableHotkey()` and `RegisterConditionalRepeatableHotkey()`
  support hold-to-repeat scenarios.
- `OnKeyDown` ignores modifier keys, creates a `Hotkey` from the event, looks
  it up, evaluates optional conditions, suppresses matched keys (`e.Handled = true`),
  and either fires once (`_activeHotkey` guarded) or on repeats (repeatable mode).
- `OnKeyUp` clears `_activeHotkey` when the main key is released.
- Supports generic typed overloads up to 5 bound arguments.
- `GetRegisteredHotkeysSnapshot()` exposes metadata for UI display (description,
  source context, conditional/repeatable flags).

### Design note
Hotkey execution is dispatched via `Task.Run` to keep the low-level keyboard
hook callback responsive and avoid hook timeout/removal.

---

## 5. Text Expansion (`Macrosharp.Devices.Keyboard.TextExpansion`)

| Item | Detail |
|------|--------|
| **Classes** | `TextExpansionManager`, `TextExpansionConfigurationManager`, `TextExpansionBuffer`, `PlaceholderProcessor` |
| **Status** | ✅ Fully implemented |

### What it does
Watches typed text via the keyboard hook and automatically replaces configured
trigger abbreviations with their expansions. Supports immediate and
on-delimiter trigger modes, case sensitivity, placeholders (`$DATE$`,
`$CLIPBOARD$`, `$CURSOR$`, etc.), and live configuration reload.

### How it works
- `TextExpansionBuffer` accumulates keystrokes, auto-clears on window change.
- On each key-down, checks if the buffer ends with any trigger. If matched,
  sends backspaces, then types the expansion (via `TypeUnicodeText` or
  `PasteText` for long text), handles `$CURSOR$` positioning.
- `TextExpansionConfigurationManager` watches the JSON config file with
  `FileSystemWatcher` (500 ms debounce) and fires `ConfigurationChanged`.
- Toggle on/off via `IsEnabled` property.

### Configuration
Located at `src/text-expansions.json`. Rules define trigger, expansion,
mode (Immediate/OnDelimiter), case sensitivity, and enabled flag.

---

## 6. Mouse Hook (`Macrosharp.Devices.Mouse`)

| Item | Detail |
|------|--------|
| **Classes** | `MouseHookManager`, `MouseBindingManager`, `MouseButtonModifiers` |
| **Status** | ✅ Fully implemented |

### What it does
Global low-level mouse hook (`WH_MOUSE_LL`) with a binding manager for mouse
button/scroll actions, including multi-button chords and scroll-while-holding
combinations.

### How it works
- `MouseHookManager`: Fires `MouseButtonDown/Up/Scroll/Move` handlers.
  `CaptureMouseMove` defaults to false for performance.
- `MouseBindingManager`: Dictionary keyed by `MouseBinding` (trigger button +
  held buttons + scroll direction). Supports button and scroll bindings with
  generic typed argument overloads. `_activeBinding` prevents repeat-fire.
- `MouseButtonModifiers`: Static bitmask tracking which mouse buttons are
  currently held (mirrors keyboard `Modifiers` pattern).

### Current usage in Program.cs
`Main()` starts the global mouse hook and integrates mouse behavior through:
- Scroll-Lock keyboard-as-mouse controls (scroll, cursor move, click/hold).
- Conditional/repeatable hotkeys that trigger mouse-wheel zoom behavior.

The demo-style `MouseBindingManager` examples remain available in older
test flows but are no longer the primary runtime path.

---

## 7. Keyboard Simulator (`Macrosharp.Devices.Core`)

| Item | Detail |
|------|--------|
| **Class** | `KeyboardSimulator` (static) |
| **Status** | ✅ Fully implemented |

### Key methods
| Method | Purpose |
|--------|---------|
| `SimulateKeyPress(VirtualKey)` | Single key press+release via `SendInput` |
| `SimulateKeyPressSequence(list, delay)` | Sequence of keys with optional delays |
| `SimulateHotKeyPress(dict)` | Chord press (e.g., Shift+1 for '!') |
| `FindAndSendKeyToWindow(className, key)` | `PostMessage` key to a specific window |
| `SendBackspaces(count)` | Erase characters |
| `TypeUnicodeText(text)` | Type arbitrary Unicode via `KEYEVENTF_UNICODE` |
| `PasteText(text)` | Clipboard paste (saves/restores clipboard) |
| `MoveCursorLeft(count)` | Move text cursor left |
| `SimulateBurstClicksAsync(key, intervalMs, durationMs, token)` | Cancellation-aware burst loop with defaults (100ms interval, 0ms duration = infinite) |
| `TryValidateBurstClickSettings(...)` | Validates key, interval, and duration inputs for burst mode |
| `SimulateBurstClicks()` | Console helper that prompts and forwards to async burst loop |

---

## 8. Mouse Simulator (`Macrosharp.Devices.Core`)

| Item | Detail |
|------|--------|
| **Class** | `MouseSimulator` (static) |
| **Status** | ✅ Fully implemented |

### Key methods
| Method | Purpose |
|--------|---------|
| `SendMouseClick(x, y, button, op)` | Click/down/up at coordinates or current position |
| `SendMouseClickToWindow(className, ...)` | Click inside a specific window via `PostMessage` |
| `MoveCursor(dx, dy)` | Relative cursor movement via `SendInput` |
| `SendMouseScroll(steps, direction)` | Vertical/horizontal scroll via `SendInput` |

---

## 9. Window Management (`Macrosharp.Win32.Abstractions.WindowTools`)

| Item | Detail |
|------|--------|
| **Classes** | `WindowFinder`, `WindowModifier`, `Messaging` |
| **Status** | ✅ Fully implemented |

### WindowFinder
- `GetWindowClassName(hwnd)` — class name of a window (or foreground if default)
- `GetWindowTitle(hwnd)` — window title text
- `GetHwndByClassName(className, checkAll)` — find windows by class name
- `GetHwndByTitle(title, checkAll)` — find windows by title

### WindowModifier
- `ToggleAlwaysOnTopState(hwnd)` — toggles `WS_EX_TOPMOST`
- `AdjustWindowPositionAndSize(hwnd, dx, dy, dw, dh)` — delta-based move/resize
- `AdjustWindowOpacity(hwnd, opacityDelta)` — layered window alpha (5–255 range)

### Messaging
- `SendMessageToWindow(hwnd, msg, wParam, lParam)` — synchronous send
- `PostMessageToWindow(hwnd, msg, wParam, lParam)` — asynchronous post
- `PostMessageToThread(threadId, msg, ...)` — thread-targeted post

---

## 10. Explorer Integration (`Macrosharp.Win32.Abstractions.Explorer`)

| Item | Detail |
|------|--------|
| **Classes** | `ExplorerShellManager`, `ExplorerFileAutomation` |
| **Status** | ✅ Fully implemented |

### ExplorerShellManager
High-level Explorer interaction via Shell COM + UI Automation fallback on a
dedicated STA thread.
- `OpenFolder()`, `OpenAndSelectItems()`, `Refresh()`
- `GetSelectedItems(hwnd)`, `GetCurrentFolderPath(hwnd)`
- `SelectItems(folderPath, itemPaths, hwnd)`
- `ExecuteContextMenuAction(folderPath, items, verb, ...)`

### ExplorerFileAutomation
File operations on the active Explorer/desktop window:
- `CreateNewFile(hwnd)` — incremental "New File.txt" + enter edit mode
- `OfficeFilesToPdf(appName, hwnd)` — PowerPoint/Word/Excel → PDF via COM
- `GenericFileConverter(patterns, convertFunc, ...)` — user-confirmed conversion
- `FlattenDirectories(hwnd)` — merge subfolders into "Flattened"
- `ImagesToPdf(mode, ...)` — combine images into PDF (Normal or Resize mode)

---

## 11. Image Editor (`Macrosharp.UserInterfaces.ImageEditorWindow`)

| Item | Detail |
|------|--------|
| **Classes** | `ImageEditorWindowHost`, `ImageEditorWindow`, `ImageEditor`, `ImageEditorState` |
| **Status** | ✅ Fully implemented |

Full-featured lightweight image editor with Draw, Crop, ColorPicker, and Pan
tools. Supports zoom (0.1–32×), undo/redo (20 levels), rotate, flip, grayscale,
invert, open file/clipboard. Pure Win32 window with double-buffered GDI
rendering.

Entry points: `ImageEditorWindowHost.Run()`, `.RunWithFile(path)`,
`.RunWithClipboard()`.

---

## 12. Dynamic Window (`Macrosharp.UserInterfaces.DynamicWindow`)

| Item | Detail |
|------|--------|
| **Class** | `SimpleWindow` |
| **Status** | ✅ Fully implemented |

Pure Win32 dynamic input dialog with configurable labels, edit fields, key
capture mode (modifier support, up to 3 key presses), and DPI awareness.

---

## 13. Infrastructure Utilities (`Macrosharp.Infrastructure`)

| Item | Detail |
|------|--------|
| **Status** | ✅ Fully implemented |

### PathLocator
- `RootPath` — walks up to find `Macrosharp.sln`
- `GetSfxPath()`, `GetIconFilesFromAssets()`, `GetConfigPath(filename)`

### AudioPlayer
- `PlayAudio(path)`, `PlayStartAsync()`, `PlaySuccessAsync()`, `PlayFailure()`
- Uses `SoundPlayer`; wraps exceptions silently.

### MutexGuardLock
- `AcquireMutex(name)` → `ErrorOr<Mutex>`. Plays "denied.wav" on conflict.

### ImagePdfUtilities
- `FilterAndPrepareImages()`, `CreatePdfFromImages()` (PdfSharpCore)
- `ResizeImageToWidth()` (bicubic), `OrderByNaturalFileName()`,
  `CleanupTempDirectory()`

---

## 14. Hotkey Configuration System (`Macrosharp.Devices.Keyboard.HotkeyBindings`)

| Item | Detail |
|------|--------|
| **Classes** | `HotkeyDefinition`, `HotkeyConfigurationManager`, `HotkeyActionService` |
| **Status** | ⚠️ Implemented but not actively used |

JSON-based hotkey configuration with `FileSystemWatcher` for live reload.
`HotkeyActionService` provides an action registry mapping names to
`Action<IReadOnlyDictionary<string,string>>`. This system exists as an
alternative to code-based hotkey registration but is not currently wired into
the main flow.

---

## 15. Program.cs Entry Point

| Item | Detail |
|------|--------|
| **Status** | ✅ Unified runtime entry point with setup-method orchestration |

### Current structure
The current `Program.cs` is organized as a primary runtime pipeline in
`Main()` with focused setup methods for each subsystem:
1. Early initialization (AUMID/tray config/state).
2. Main configuration and watcher setup.
3. Toast host and reminders scheduler setup.
4. Tray icon host and dynamic menu actions (`BuildTrayMenu`).
5. Keyboard hook + hotkey manager + text expansion wiring (`SetupTextExpansion`, keyboard handler setup methods).
6. Mouse hook startup and Scroll-Lock keyboard-as-mouse handling (`SetupScrollLockMouseHandler`).
7. Hotkey registration by domain (`SetupHotkeyRegistrations`):
  - Application Control
  - Window Management
  - Miscellaneous (system/media/device controls)
  - File Management (Explorer-focused actions)
8. Startup banner, Win32 message loop, and deterministic cleanup.

### Runtime hotkey reference
- Exposed via tray action `Show Hotkeys` and hotkey `Ctrl+Win+/`.
- Backed by `GetRegisteredHotkeysSnapshot()` from `HotkeyManager`.
- Uses a filterable table with deterministic source/key ordering and item count in title.

### Coverage summary
- Spec-style hotkey set is now actively registered.
- Scroll-Lock-dependent controls are implemented.
- Explorer-focused file-management hotkeys are implemented.
- Window-management hotkeys (opacity, always-on-top, move, resize) are implemented.
- System-control hotkeys (sleep, shutdown, display switch, volume, brightness) are implemented.
- MPC-HC control hotkeys are implemented.
- Process suspend/resume, pause/resume event handling, and console visibility toggle are implemented.

---

## 16. Previously Missing Features — Current Status

All items from the previous "not yet implemented" list are now implemented in
the current codebase, including:
- Process suspend/resume (`NtSuspendProcess` / `NtResumeProcess`).
- System sleep, shutdown, display switching, volume, and brightness controls.
- MPC-HC media commands.
- Explorer file-path copy, image→ICO conversion, and MP3→WAV conversion.
- Console visibility toggle, global pause/resume handling, Scroll Lock toggle.
- Conditional and repeatable hotkey support in `HotkeyManager`.

### Operational prerequisites
- MP3→WAV conversion expects `ffmpeg` to be available on `PATH`.
- Brightness control depends on WMI monitor APIs and supported hardware.
