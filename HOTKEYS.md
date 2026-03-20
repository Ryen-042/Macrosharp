# Macrosharp — Hotkey Reference

> Complete hotkey reference organized by category.
> Includes key combinations, descriptions, preconditions, and implementation status.

---

## Status Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully implemented — infrastructure exists and ready to wire |
| ⚠️ | Partially implemented — some code exists, needs completion or new glue code |
| ❌ | Missing — no supporting implementation exists anywhere in the project |

---

## 1. Hotkeys that Ignore Lock Key State

> These hotkeys work regardless of Scroll Lock, Caps Lock, and Num Lock state.

### 1.1 Application Control

| Hotkey | Description | Preconditions | Status | Notes |
|--------|-------------|---------------|--------|-------|
| Win + Esc | Confirm and terminate application | None | ✅ | Shows a confirmation message box before exiting. |
| Alt + Win + Esc | Terminate application immediately | None | ✅ | Exits without showing a confirmation dialog. |
| Esc | Stop active burst click | Burst click is currently running | ✅ | Stops the burst click loop immediately. ESC is only suppressed while burst click is active. |
| Win + ? | Show "application is running" notification | None | ✅ | Toast infrastructure fully implemented. Use "With Action Buttons" content from tray menu |
| Win + Shift + Delete | Clear console output | None | ✅ | `Console.Clear()` exists in tray menu action |
| Win + Shift + Insert | Toggle terminal output visibility | None | ✅ | `SystemActions.ToggleConsoleVisibility()`; plays On/Off sound |
| Ctrl + Alt + Win + P | Pause/resume all keyboard and mouse event handling | None | ✅ | `_paused` flag checked at top of every hook handler; plays On/Off sound |
| Ctrl + Alt + Win + B | Toggle burst click (start/stop) | None | ✅ | Starts burst click with key/duration/interval prompt when inactive; stops active burst immediately when running. |

### 1.2 Window Management

| Hotkey | Description | Preconditions | Status | Notes |
|--------|-------------|---------------|--------|-------|
| \` + = _or_ \` + Add | Increase active window opacity | None | ✅ | `WindowModifier.AdjustWindowOpacity(delta: +25)` exists |
| \` + - _or_ \` + Subtract | Decrease active window opacity | None | ✅ | `WindowModifier.AdjustWindowOpacity(delta: -25)` exists |
| Ctrl + Win + A | Toggle always-on-top for focused window | None | ✅ | `WindowModifier.ToggleAlwaysOnTopState()` exists |
| \` + ↑/→/↓/← | Move active window (medium distance) | None | ✅ | `WindowModifier.AdjustWindowPositionAndSize()` exists |
| \` + Shift + ↑/→/↓/← | Move active window (small distance) | None | ✅ | Same API, smaller deltas |
| \` + Alt + ↑/→/↓/← | Resize active window | None | ✅ | Same API, width/height deltas |
| Ctrl + Pause | Suspend process of active window | None | ✅ | `ProcessControl.SuspendActiveWindowProcess()`; plays Off sound |
| Ctrl + Shift + Pause | Resume suspended process of active window | None | ✅ | `ProcessControl.ResumeActiveWindowProcess()`; plays On sound |

### 1.3 Miscellaneous

| Hotkey | Description | Preconditions | Status | Notes |
|--------|-------------|---------------|--------|-------|
| \` + \\ | Open image processing window | None | ✅ | `ImageEditorWindowHost.RunWithClipboard()` exists |
| \` + W | Seek forward in top MPC-HC window | MPC-HC class "MediaPlayerClassicW" must exist | ✅ | `SendMpcCommand(905)` — WM_COMMAND small jump forward |
| \` + S | Seek backward in top MPC-HC window | Same as above | ✅ | `SendMpcCommand(906)` — WM_COMMAND small jump backward |
| \` + Space | Play/pause top MPC-HC window | Same as above | ✅ | `SendMpcCommand(889)` — WM_COMMAND play/pause |
| Win + CapsLock | Toggle Scroll Lock | None | ✅ | `KeyboardSimulator.SimulateKeyPress(VirtualKey.SCROLL)` |
| Ctrl + Alt + Win + S | Sleep mode | None | ✅ | `SystemActions.Sleep()`; plays logoff sound sync before sleeping |
| Ctrl + Alt + Win + Q | Shutdown | None | ✅ | `SystemActions.Shutdown()` behind `MessageBox` Yes/No confirmation |
| Ctrl + Alt + Win + Num1 | Switch to internal display | None | ✅ | `SystemActions.SwitchDisplay(1)` + bonk sound |
| Ctrl + Alt + Win + Num2 | Switch to external display | None | ✅ | `SystemActions.SwitchDisplay(2)` + bonk sound |
| Ctrl + Alt + Win + Num3 | Extend display | None | ✅ | `SystemActions.SwitchDisplay(3)` + bonk sound |
| Ctrl + Alt + Win + Num4 | Clone display | None | ✅ | `SystemActions.SwitchDisplay(4)` + bonk sound |
| Ctrl + Shift + = _or_ Ctrl + Shift + Add | Increase system volume | None | ✅ | Simulate `VK_VOLUME_UP` via `KeyboardSimulator` |
| Ctrl + Shift + - _or_ Ctrl + Shift + Subtract | Decrease system volume | None | ✅ | Simulate `VK_VOLUME_DOWN` via `KeyboardSimulator` |
| \` + F2 | Decrease screen brightness | None | ✅ | `BrightnessControl.DecreaseBrightness()`; logs level to console |
| \` + F3 | Increase screen brightness | None | ✅ | `BrightnessControl.IncreaseBrightness()`; logs level to console |

---

## 2. Hotkeys that Require Scroll Lock to be ON

> These hotkeys are active only when Scroll Lock is toggled ON.
> They **must not** suppress key events when Scroll Lock is OFF.

### 2.1 Keyboard/Mouse Control

| Hotkey | Description | Preconditions | Status | Notes |
|--------|-------------|---------------|--------|-------|
| W | Scroll up (medium) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: 3)` |
| S | Scroll down (medium) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: -3)` |
| A | Scroll left (medium) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: -3, direction: 0)` |
| D | Scroll right (medium) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: 3, direction: 0)` |
| Alt + W | Scroll up (large) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: 8)` |
| Alt + S | Scroll down (large) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: -8)` |
| Alt + A | Scroll left (large) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: -8, direction: 0)` |
| Alt + D | Scroll right (large) | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseScroll(steps: 8, direction: 0)` |
| Q | Left mouse click | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseClick(LeftButton)` |
| E | Right mouse click | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseClick(RightButton)` |
| 2 | Middle mouse click | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseClick(MiddleButton)` |
| Ctrl + E | Zoom in | Scroll Lock ON | ✅ | Simulate Ctrl+ScrollUp |
| Ctrl + Q | Zoom out | Scroll Lock ON | ✅ | Simulate Ctrl+ScrollDown |
| \` + Q | Hold/release left mouse button | Scroll Lock ON | ✅ | `MouseSimulator.SendMouseClick(LeftButton, MouseDown)` then `MouseUp` on repeat |
| \` + E | Hold/release right mouse button | Scroll Lock ON | ✅ | Same pattern for RightButton |
| \` + 2 | Hold/release middle mouse button | Scroll Lock ON | ✅ | Same pattern for MiddleButton |
| ; | Move cursor right (medium) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 20, dy: 0)` |
| ' | Move cursor down (medium) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: 20)` |
| / | Move cursor left (medium) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: -20, dy: 0)` |
| . | Move cursor up (medium) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: -20)` |
| Shift + ; | Move cursor right (small) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 3, dy: 0)` |
| Shift + ' | Move cursor down (small) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: 3)` |
| Shift + / | Move cursor left (small) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: -3, dy: 0)` |
| Shift + . | Move cursor up (small) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: -3)` |
| Alt + ; | Move cursor right (large) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 80, dy: 0)` |
| Alt + ' | Move cursor down (large) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: 80)` |
| Alt + / | Move cursor left (large) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: -80, dy: 0)` |
| Alt + . | Move cursor up (large) | Scroll Lock ON | ✅ | `MouseSimulator.MoveCursor(dx: 0, dy: -80)` |

---

## 3. Hotkeys that Require Explorer/Desktop to be Focused

> These hotkeys are active only when a File Explorer or Desktop window is in the
> foreground. Key events **must not** be suppressed in other applications.

### 3.1 File Management

| Hotkey | Description | Preconditions | Status | Notes |
|--------|-------------|---------------|--------|-------|
| Ctrl + Shift + M | Create new file | Explorer or Desktop focused | ✅ | `ExplorerFileAutomation.CreateNewFile()` exists |
| Shift + F2 | Copy full path of selected files to clipboard | Explorer focused | ✅ | `ExplorerHotkeys.GetSelectedFilePaths()` + `KeyboardSimulator.SetClipboardText()`; plays success sound |
| \` + P | Convert selected PowerPoint files to PDF | Explorer focused | ✅ | `ExplorerFileAutomation.OfficeFilesToPdf("PowerPoint")` exists |
| \` + O | Convert selected Word files to PDF | Explorer focused | ✅ | `ExplorerFileAutomation.OfficeFilesToPdf("Word")` exists |
| \` + E | Convert selected Excel files to PDF | Explorer focused | ✅ | `ExplorerFileAutomation.OfficeFilesToPdf("Excel")` exists |
| Ctrl + Shift + P | Merge selected images into PDF (Normal mode) | Explorer focused | ✅ | `ExplorerFileAutomation.ImagesToPdf()` exists |
| Ctrl + Shift + Alt + P | Merge selected images into PDF (Resize mode) | Explorer focused | ✅ | `SimpleWindow` dialog prompts for Target Width / Width Threshold / Min Width / Min Height, then calls `ExplorerFileAutomation.ImagesToPdf(Resize, ...)` |
| Ctrl + Alt + Win + I | Convert selected images to .ico | Explorer focused | ✅ | `ExplorerHotkeys.ConvertSelectedImagesToIco()` |
| Ctrl + Alt + Win + M | Convert selected .mp3 files to .wav | Explorer focused | ✅ | `ExplorerHotkeys.ConvertSelectedMp3ToWav()` |

---

## 4. Conflict Analysis & Design Issues

### 4.1 Key Combination Collisions

| Conflict | Analysis | Resolution |
|----------|----------|------------|
| \` + W/S (window mgmt: MPC-HC seek) vs W/S (Scroll Lock: scroll) | Different modifier state: \` prefix vs plain key. \` is tracked as a modifier, so `Backtick+W` ≠ `W`. **No collision.** | N/A |
| \` + E (Explorer: Excel→PDF) vs \` + E (Scroll Lock: right hold) | Both use Backtick+E. When Scroll Lock is ON and Explorer is focused, both could match. | Scroll Lock hotkeys should be checked first; Explorer hotkeys should only match when Scroll Lock is OFF. The Explorer PDF hotkeys don't make sense during Scroll Lock mode. |
| Ctrl + Shift + P (Images→PDF) vs VS Code Command Palette | Global hook suppresses the key in ALL apps. Ctrl+Shift+P is heavily used in IDEs. | Must check if Explorer is focused before suppressing. Use conditional guard. |
| Shift + F2 (Copy path) vs Excel/IDE rename | Same issue — Shift+F2 has meaning in many applications. | Must check if Explorer is focused. Use conditional guard. |
| Ctrl + Shift + M (Create file) vs VS Code toggle problems panel | Same pattern. | Conditional guard for Explorer focus. |

### 4.2 Hotkeys That Could Fire in Wrong Context

| Hotkey | Risk | Mitigation |
|--------|------|------------|
| All Explorer hotkeys (Shift+F2, Ctrl+Shift+M/P, \`+P/O/E) | Will suppress keys in non-Explorer apps | Conditional guard: check `IsExplorerOrDesktopFocused()` |
| All Scroll Lock hotkeys (W/S/A/D/Q/E/2, ;/'/./) | Will suppress typing when Scroll Lock is unexpectedly on | Conditional guard: check `Modifiers.IsScrollLockOn` |
| \` + Space (MPC-HC play/pause) | Will interfere with typing after backtick | Only fires when backtick modifier is held; backtick is consumed as modifier |

### 4.3 Handler Ordering Issues

| Issue | Detail |
|-------|--------|
| Scroll Lock check timing | Scroll Lock hotkeys must be handled in a separate `KeyDownHandler` event handler that runs alongside (not through) `HotkeyManager`, because `HotkeyManager` always suppresses matched keys. The raw handler can conditionally suppress based on `IsScrollLockOn`. |
| Backtick modifier conflict with typing | The backtick key is treated as a modifier by the `Modifiers` class. This means pressing backtick alone won't type '\`' — it's consumed as a modifier. The current code has a TODO about this. Users who need to type backtick would need Scroll Lock OFF and no other modifier combo active. |
| `_activeHotkey` prevents repeat-fire | For scroll and cursor movement hotkeys, holding a key should ideally repeat. The `_activeHotkey` mechanism blocks this. Scroll Lock hotkeys handled via raw `KeyDownHandler` can allow repeat naturally. |

### 4.4 Usability Concerns

| Concern | Detail |
|---------|--------|
| Scroll Lock as mode toggle | Using Scroll Lock as a mode modifier for keyboard-as-mouse is a well-established pattern (used by AutoHotkey scripts). However, it may surprise users if triggered accidentally. The Win+CapsLock toggle provides a deliberate activation path. |
| Ctrl+Alt+Win+Q for Shutdown | This is a destructive action behind a 4-key combo. Consider adding a confirmation dialog. |
| Ctrl+Alt+Win+S for Sleep | Less destructive but still disruptive. A 4-key combo provides reasonable protection. |
| Mouse hold toggle | Backtick+Q toggles left mouse hold; same key releases. Need state tracking per button. |

### 4.5 Technical Notes

| Note | Detail |
|------|--------|
| HotkeyManager needs conditional support | Adding `Func<bool>? condition` parameter enables Explorer-focused hotkeys to pass through when condition is false (key not suppressed). This is a backward-compatible change. |
| MPC-HC WM_COMMAND codes | Forward seek: WM_COMMAND with `wParam = 905` (small jump forward). Backward: `wParam = 906`. Play/Pause: `wParam = 889`. These are internal MPC-HC command IDs. |
| NativeMethods additions needed | `GetConsoleWindow`, `GetWindowThreadProcessId`, `ExitWindowsEx`, `OpenProcess`, `EXIT_WINDOW_FLAGS` |
