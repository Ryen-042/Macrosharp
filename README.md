# Macrosharp

Macrosharp is a Windows automation toolkit with native Win32 integrations, keyboard/mouse simulation, lightweight UI components, and a fast image editor window.

## Highlights
- **Win32-first**: direct P/Invoke wrappers for windowing, input, and GDI.
- **Keyboard & mouse automation**: hooks, hotkeys, and simulators.
- **Tray icon UI**: lightweight tray host and menu system.
- **Image editor window**: fast, keyboard-first editor with drawing, crop, undo/redo, and clipboard/file loading.

## Repository Layout
- src/Macrosharp.Devices — keyboard and mouse utilities
- src/Macrosharp.Win32 — Win32 abstractions and native bindings
- src/Macrosharp.UserInterfaces — UI components (TrayIcon, ImageEditorWindow, DynamicWindow)
- src/Macrosharp.Hosts — console host and demos
- src/assets — icons and media

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

## Image Editor Window
- Launch example: see the `ImageEditorWindow` region in src/Macrosharp.Hosts/Macrosharp.Hosts.Console/Program.cs.
- Shortcuts: W/L/C/Space (tools), Ctrl+Z/Y (undo/redo), Ctrl+O (open file), Ctrl+V (clipboard).

## Notes
- The UI components are built on Win32 APIs (no Windows Forms).
- Some features require Windows.

## License
See LICENSE if present, or contact the repository owner for licensing details.
