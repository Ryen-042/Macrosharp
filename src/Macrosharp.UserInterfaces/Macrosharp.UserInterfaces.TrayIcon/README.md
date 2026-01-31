# Macrosharp.UserInterfaces.TrayIcon

A Windows system tray (notification area) icon library built with Win32 APIs. Provides a lightweight, dependency-free way to create tray icons with context menus, click handlers, and icon cycling support.

## Features

- **System Tray Icon** - Create and manage notification area icons
- **Context Menu** - Build hierarchical context menus with icons
- **Click Handlers** - Handle single-click and double-click events
- **Dynamic Updates** - Update tooltip and icon at runtime
- **Icon Cycling** - Cycle through multiple icons for animated states
- **Explorer Restart Recovery** - Automatically restores tray icon when Explorer restarts
- **Thread-Safe** - Runs on a dedicated STA thread with proper lifecycle management

## Components

### TrayIconHost

The main class for creating and managing a system tray icon.

```csharp
// Define menu items
var menuItems = new List<TrayMenuItem>
{
    TrayMenuItem.ActionItem("Show Window", () => ShowMainWindow()),
    TrayMenuItem.ActionItem("Settings", () => OpenSettings(), iconPath: "settings.ico"),
    TrayMenuItem.Separator(),
    TrayMenuItem.Submenu("Options", new List<TrayMenuItem>
    {
        TrayMenuItem.ActionItem("Option 1", () => SetOption1()),
        TrayMenuItem.ActionItem("Option 2", () => SetOption2()),
    }),
};

// Create tray icon host
var trayIcon = new TrayIconHost(
    tooltip: "My Application",
    iconPath: "app.ico",
    menuItems: menuItems,
    defaultClickIndex: 0,      // Single-click triggers "Show Window"
    defaultDoubleClickIndex: 0 // Double-click also triggers "Show Window"
);

// Start the tray icon
trayIcon.Start();

// Update tooltip dynamically
trayIcon.UpdateTooltip("My Application - Running");

// Update icon dynamically
trayIcon.UpdateIcon("app-active.ico");

// Stop and dispose
trayIcon.Stop();
```

### TrayMenuItem

Represents a menu item in the context menu. Supports action items, submenus, and separators.

```csharp
// Action item with click handler
TrayMenuItem.ActionItem("Open", () => OpenApp());

// Action item with an icon
TrayMenuItem.ActionItem("Settings", () => OpenSettings(), iconPath: "settings.ico");

// Submenu with nested items
TrayMenuItem.Submenu("Advanced", new List<TrayMenuItem>
{
    TrayMenuItem.ActionItem("Debug Mode", () => ToggleDebug()),
    TrayMenuItem.ActionItem("Logs", () => OpenLogs()),
});

// Separator line
TrayMenuItem.Separator();
```

### IconCycler

Utility class for cycling through multiple icons in a round-robin fashion. Useful for animated tray icons or state indicators.

```csharp
var iconPaths = new List<string>
{
    "icon-state1.ico",
    "icon-state2.ico",
    "icon-state3.ico",
};

var cycler = new IconCycler(iconPaths);

// Get the next icon in sequence
string? nextIcon = cycler.GetNext(); // "icon-state1.ico"
nextIcon = cycler.GetNext();         // "icon-state2.ico"

// Reset to beginning
cycler.Reset();

// Check current state
bool hasIcons = cycler.HasIcons;
int count = cycler.Count;
string? current = cycler.Current;
```

## Usage Example

```csharp
// Complete example with all features
var menuItems = new List<TrayMenuItem>
{
    TrayMenuItem.ActionItem("Open Dashboard", OpenDashboard),
    TrayMenuItem.ActionItem("Pause", TogglePause, iconPath: "pause.ico"),
    TrayMenuItem.Separator(),
    TrayMenuItem.Submenu("Status", new List<TrayMenuItem>
    {
        TrayMenuItem.ActionItem("Active", () => SetStatus("active")),
        TrayMenuItem.ActionItem("Away", () => SetStatus("away")),
        TrayMenuItem.ActionItem("Busy", () => SetStatus("busy")),
    }),
};

using var trayIcon = new TrayIconHost(
    tooltip: "Macrosharp",
    iconPath: @"C:\path\to\icon.ico",
    menuItems: menuItems,
    defaultClickIndex: 0,
    defaultDoubleClickIndex: 0
);

if (trayIcon.Start())
{
    Console.WriteLine("Tray icon created successfully");
    
    // Application main loop
    Application.Run();
}
else
{
    Console.WriteLine("Failed to create tray icon (non-interactive session?)");
}
```

## Dependencies

- `Microsoft.Windows.CsWin32` - P/Invoke source generator for Win32 APIs

## Implementation Notes

- Uses `Shell_NotifyIcon` for tray icon management
- Runs on a dedicated STA thread for proper COM apartment model
- Handles `TaskbarCreated` message to restore icon after Explorer restart
- Menu items with icons use GDI bitmap rendering
- Single-click has a delay to distinguish from double-click
- Automatically adds a "Quit" menu item at the end

## Limitations

- Requires an interactive user session (won't work in Windows Services)
- Icon paths must be valid `.ico` files
- Maximum tooltip length is 128 characters (truncated if longer)
