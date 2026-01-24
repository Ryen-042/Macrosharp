# Macrosharp Image Editor Window

## Overview
The Image Editor Window is a lightweight, keyboard-first image editor focused on fast edits with minimal UI friction. It uses a Win32 window with custom rendering and avoids Windows Forms to keep references minimal.

## Features
- Real-time drawing with brush size control
- Crop tool with confirm/cancel flow
- Color picker
- Pan and zoom (view-only)
- Undo/redo (bounded)
- Rotate, invert, grayscale, and flip operations
- Status bar (toggleable)
- Load image from file or clipboard

## Modes / Tools
- **Draw**: Freehand drawing with brush radius
- **Crop**: Drag to select, `Enter` to apply, `Esc` to cancel
- **Color Picker**: Click to sample pixel color
- **Pan**: Click-drag to move the view

## Keyboard & Mouse Bindings
### File & Clipboard
- **Ctrl + O**: Open image file dialog
- **Ctrl + V**: Load image from clipboard (bitmap or file path text)

### Undo / Redo
- **Ctrl + Z**: Undo
- **Ctrl + Y**: Redo

### Editing
- **R**: Rotate 90°
- **T**: Grayscale
- **I**: Invert colors
- **H**: Flip horizontal
- **V**: Flip vertical

### Tools
- **W**: Draw tool
- **L**: Crop tool
- **C**: Color picker
- **Space**: Pan tool
- **Esc**: Cancel current tool

### View
- **Mouse Wheel**: Zoom in/out
- **Ctrl + 0**: Reset view to fit
- **F1**: Toggle status bar

### Mouse
- **Left Drag**: Draw (Draw tool)
- **Right Drag**: Erase (Draw tool)
- **Middle Drag / Space + Drag**: Pan (Pan tool)
- **Crop Drag**: Define crop rectangle (Crop tool)

## Notes
- The editor uses a custom Win32 render loop (no Windows Forms).
- Zoom is view-only. The raster image is modified only by edit operations.
- Loading from clipboard supports bitmap data or a text file path.
