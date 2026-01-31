# Macrosharp

Macrosharp is a Windows automation toolkit focused on native Win32 integrations for input automation, windowing UI, and fast, low-dependency utilities. It provides global keyboard hooks, hotkey management, keyboard/mouse simulation, tray icon hosting, dynamic input windows, and a full-featured image editor window — all without WinForms or WPF.

## What Macrosharp Implements

### Keyboard Automation

- **Global low-level keyboard hook** using `WH_KEYBOARD_LL` to observe and optionally suppress key events system-wide.
- **Hotkey management** that combines modifier state tracking with action execution.
- **Modifier tracking** for Ctrl, Shift, Alt, Win, and Backtick with a shared state helper.
- **Text expansion** with buffer-based detection, placeholder processing, and configurable rules.

### Mouse Automation

- **Mouse simulation** for programmatic clicks and movement via Win32 input APIs.
- **Mouse button abstraction** to map logical button intent to native input flags.

### UI Components (Win32-First)

- **Tray icon host** with a dedicated STA thread, context menu hierarchy, optional menu icons, single/double-click actions, and Explorer restart recovery.
- **Dynamic input window** for prompt-like dialogs with multiple labeled fields, placeholders, and optional key-capture mode.
- **Image editor window** with a full rendering pipeline using GDI and double-buffering.

### Image Editor Window Features

- **Tools**: Draw, Crop, Pan, and Color Picker.
- **Transformations**: Rotate 90°, flip horizontal/vertical, invert, grayscale.
- **View controls**: Zoom at cursor, pan, fit-to-window toggle.
- **State management**: Undo/redo and revert to original image.
- **Clipboard & file loading** with native file dialogs and clipboard access.
- **Keyboard-first workflow** with a help overlay and status bar.

### Win32 Interop & Native Utilities

- **Direct P/Invoke bindings** using CsWin32-generated APIs.
- **Safe handle patterns** for GDI resources and Win32 object lifetimes.
- **Native helper abstractions** for core Win32 calls and constants.

## Repository Layout

- src/Macrosharp.Devices
	- Keyboard and mouse automation (hooks, hotkeys, simulators, text expansion)
- src/Macrosharp.Win32
	- Win32 abstractions and native bindings
- src/Macrosharp.UserInterfaces
	- UI components: TrayIcon, ImageEditorWindow, DynamicWindow
- src/Macrosharp.Hosts
	- Console host and demo entry points
- src/assets
	- Icons and media resources

## Build

```powershell
# from repo root
cd .\src
dotnet build .\Macrosharp.sln
```

## Run (Console Host)

```powershell
# from repo root
cd .\src\Macrosharp.Hosts\Macrosharp.Hosts.Console
dotnet run
```

## Image Editor Window Quick Start

- Launch example: see the ImageEditorWindow section in src/Macrosharp.Hosts/Macrosharp.Hosts.Console/Program.cs.
- Key bindings: W/L/C/P (tools), Ctrl+Z/Y (undo/redo), Ctrl+O (open file), Ctrl+V (clipboard), Space (fit zoom), F1 (status bar), Shift+? (help).

## Design Goals

- **Minimal dependencies** and no UI frameworks.
- **Native performance** via Win32 APIs and GDI.
- **Composable utilities** usable from console apps or services (where UI is supported).
- **Keyboard-first UX** for automation and fast interaction.

## Notes

- UI components require an interactive Windows user session.
- Low-level input hooks and simulated input may be restricted by some apps (e.g., elevated UI or protected windows).

## License

See LICENSE if present, or contact the repository owner for licensing details.
