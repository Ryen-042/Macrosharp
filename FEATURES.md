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
Instantiated inside `StartKeyboardSimulator()` with a full menu tree including:
Open Script Folder, Show Notification (submenu with 9 toast variants), Switch
Icon, Reload (Hotkeys/Configs submenu), Clear Console Logs, Toggle Silent Mode.

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
Created in `StartKeyboardSimulator()`. The `Activated` handler dispatches
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
| **Status** | ✅ Fully implemented (needs extension for conditional hotkeys) |

### What it does
Registry of key+modifier combinations mapped to `Action` callbacks. When the
keyboard hook fires a key-down whose `(VirtualKey, CurrentModifiers)` matches a
registered `Hotkey`, the action is invoked and the key event is suppressed.

### How it works
- `RegisterHotkey()` stores `Hotkey → Action` in a dictionary.
- `OnKeyDown` ignores modifier keys, creates a `Hotkey` from the event, looks
  it up, calls the action, sets `e.Handled = true`, and marks it as
  `_activeHotkey` to prevent repeat-fire while held.
- `OnKeyUp` clears `_activeHotkey` when the main key is released.
- Supports generic typed overloads up to 5 bound arguments.

### Design note
The manager does **not** currently support:
- **Guard conditions** — needed for Scroll-Lock-dependent and Explorer-focused
  hotkeys that should only fire in specific contexts.
- **Repeat-fire** — needed for scroll/cursor-movement hotkeys where holding the key should trigger repeatedly.

These will be added as part of the hotkey spec implementation.

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
`StartMouseHook()` demonstrates middle click, chord clicks (Left+Right),
XButton actions, scroll+Right for zoom, Ctrl+Scroll for volume, and
double-click detection.

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
| `SimulateBurstClicks()` | Interactive burst click tool |

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
| **Status** | ⚠️ Demo/test collection — needs refactoring |

### Current structure
The file contains multiple isolated demo methods:
1. `Main()` — calls `StartKeyboardSimulator()` and `StartMouseHook()`, plus
   many commented-out demo blocks.
2. `StartKeyboardHook()` — basic keyboard hook demo (Ctrl+Alt+Z, Shift+A, F1–F3).
3. `StartMouseSimulator()` — mouse action demos (F1–F8).
4. `StartKeyboardSimulator()` — **most complete**: tray icon, toast
   notifications, text expansion, demo hotkeys (1–4, Z/X/C). This is the
   de facto "main" method.
5. `StartTextExpansion()` — standalone text expansion demo.
6. `StartMouseHook()` — mouse hook binding demo.

### What's missing
- **No hotkeys from the spec are registered.** All registrations are demo/test
  bindings (keys 1–4, Z/X/C, Escape, Ctrl+Alt+T).
- No Scroll-Lock-dependent keyboard/mouse control.
- No Explorer-focused file management hotkeys.
- No window management hotkeys (opacity, always-on-top, move, resize).
- No system control hotkeys (sleep, shutdown, display switch, volume, brightness).
- No MPC-HC media control.
- No process suspend/resume.
- No pause/resume all event handling.
- No console visibility toggle.

---

## 16. Missing Functionality (Not Yet Implemented Anywhere)

| Feature | Required by | Notes |
|---------|-------------|-------|
| Process suspend/resume | Ctrl+Pause / Ctrl+Shift+Pause | Need `NtSuspendProcess`/`NtResumeProcess` from ntdll.dll |
| System sleep | Ctrl+Alt+Win+S | Need `SetSuspendState` from PowrProf.dll |
| System shutdown | Ctrl+Alt+Win+Q | Need `ExitWindowsEx` from user32.dll |
| Display/monitor switch | Ctrl+Alt+Win+Num1-4 | Use `displayswitch.exe` with `/internal`, `/external`, `/extend`, `/clone` |
| Screen brightness | Backtick+F2/F3 | WMI `WmiMonitorBrightness` or dxva2 monitor APIs |
| System volume | Ctrl+Shift+=/- | Simulate `VK_VOLUME_UP`/`VK_VOLUME_DOWN` |
| MPC-HC media control | Backtick+W/S/Space | `WM_COMMAND` to "MediaPlayerClassicW" window |
| Copy full path of selected files | Shift+F2 | `ExplorerShellManager.GetSelectedItems()` + clipboard |
| Image-to-ICO conversion | Ctrl+Alt+Win+I | System.Drawing resize + ICO format save |
| MP3-to-WAV conversion | Ctrl+Alt+Win+M | `GenericFileConverter` with ffmpeg |
| Console visibility toggle | Win+Shift+Insert | `GetConsoleWindow()` + `ShowWindow()` |
| Pause/resume all event handling | Ctrl+Alt+Win+P | Global flag to skip hook processing |
| Toggle Scroll Lock | Win+CapsLock | Simulate Scroll Lock key press |
| Conditional hotkey guard | Multiple | HotkeyManager needs `Func<bool>? condition` support |
