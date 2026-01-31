# Macrosharp.UserInterfaces.ImageEditorWindow

A lightweight, keyboard-first image editor built entirely with Win32 GDI APIs for quick screenshot annotation and basic image editing. Features a tool-based architecture with zoom, pan, crop, draw, color picker, and various image transformation capabilities.

## Features

- **Tool-Based Architecture** - Draw, Crop, Pan, and ColorPicker tools
- **Zoom & Pan** - Mouse wheel zoom with anchor point, Ctrl+click pan
- **Image Transformations** - Rotate, flip, grayscale, invert
- **Undo/Redo Support** - Full edit history with Ctrl+Z/Ctrl+Y
- **Clipboard Integration** - Open images from clipboard with Ctrl+V
- **File Support** - Open images via file dialog or command line
- **Double-Buffered Rendering** - Flicker-free display using GDI
- **Keyboard Shortcuts** - Full keyboard navigation for power users
- **Status Bar** - Toggleable display of image info, cursor position, and zoom level
- **Help Overlay** - Built-in help screen (Shift+?)

## Usage

```csharp
// Simple usage - open editor with a file
using var editor = new ImageEditorWindow("Screenshot Editor");
editor.QueueOpenFromFile(@"C:\Screenshots\capture.png");
editor.Run();

// Open with clipboard content
using var editor = new ImageEditorWindow("Clipboard Image");
editor.QueueOpenFromClipboard();
editor.Run();

// Start with blank canvas
using var editor = new ImageEditorWindow("New Image");
editor.Run();
```

## Tools

| Tool | Key | Description |
|------|-----|-------------|
| **Draw** | `W` | Freehand drawing with configurable brush size and colors |
| **Crop** | `L` | Drag to select region, `Enter` to apply, `Esc` to cancel |
| **ColorPicker** | `C` | Click to sample pixel color for brush |
| **Pan** | `P` | Click-drag to move the view |

## Keyboard Shortcuts

### File & Clipboard
| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open image from file dialog |
| `Ctrl+V` | Paste image from clipboard |

### Undo / Redo
| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+R` | Reset to original image |

### Image Transformations
| Shortcut | Action |
|----------|--------|
| `R` | Rotate 90° clockwise |
| `H` | Flip horizontal |
| `V` | Flip vertical |
| `T` | Apply grayscale filter |
| `I` | Invert colors |

### Tool Selection
| Shortcut | Tool |
|----------|------|
| `W` | Draw tool |
| `L` | Crop tool |
| `C` | Color picker |
| `P` | Pan tool |
| `Escape` | Cancel current operation |

### View Controls
| Shortcut | Action |
|----------|--------|
| `Space` | Toggle fit-to-screen zoom |
| `Ctrl+Click` | Pan view (works with any tool) |
| `Ctrl+0` | Reset view |
| `Mouse Wheel` | Zoom in/out at cursor position |
| `F1` | Toggle status bar |
| `Shift+?` | Toggle help overlay |

### Brush Controls (Draw Tool)
| Shortcut | Action |
|----------|--------|
| `0` | Black brush color |
| `1-9` | Preset brush colors |
| `+` | Increase brush size |
| `-` | Decrease brush size |

## Mouse Controls

| Action | Tool | Description |
|--------|------|-------------|
| Left Drag | Draw | Freehand drawing |
| Right Drag | Draw | Erase (draw with background color) |
| Drag | Crop | Define crop rectangle |
| Click | ColorPicker | Sample pixel color |
| Drag | Pan | Move the view |
| Ctrl+Drag | Any | Pan view (universal) |
| Wheel | Any | Zoom at cursor position |

## Architecture

```
ImageEditorWindow/
├── ImageEditorWindow.cs      # Win32 window container
├── ImageEditor.cs            # Core editor logic & tool coordination
├── ImageEditorState.cs       # Image state with undo/redo
├── ImageBuffer.cs            # Pixel buffer management
├── EditorInput.cs            # Input event abstraction
├── ImageEditorIO.cs          # File and clipboard I/O
├── FileDialog.cs             # Win32 file dialog wrapper
├── GdiText.cs                # GDI text rendering utilities
├── SafeBrushHandle.cs        # Safe handle for GDI brushes
└── Tools/
    ├── IEditorTool.cs        # Tool interface
    ├── DrawTool.cs           # Freehand drawing
    ├── CropTool.cs           # Rectangle selection and crop
    ├── ColorPickerTool.cs    # Color sampling
    └── PanTool.cs            # View panning
```

## Dependencies

- `Microsoft.Windows.CsWin32` - P/Invoke source generator for Win32 APIs

## Implementation Notes

- Uses `StretchDIBits` for image rendering with zoom support
- Double-buffered rendering via compatible memory DC and bitmap
- View transform maintains anchor point during zoom operations
- Image state manages both raster (committed) and matrix (preview) buffers
- All GDI resources are properly disposed to prevent leaks
- Window automatically resizes to fit loaded images
- Zoom range: 10% to 3200%
- The editor uses a custom Win32 render loop (no Windows Forms/WPF)
- Loading from clipboard supports bitmap data or text file paths
