using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Macrosharp.Devices.Keyboard;


namespace Macrosharp.Devices.Keyboard.HotkeyBindings;

public class HotkeyDefinition
{
     // The main key, e.g., "Z", "F1", "Escape".
    public string Key { get; set; } = string.Empty;

    // List of modifier keys, e.g., ["Ctrl", "Alt"].
    public List<string> Modifiers { get; set; } = new List<string>();

    // List of lock keys that must be in a specific state, e.g., ["CAPITAL", "SCROLLLOCK"].
    public List<string> LockKeys { get; set; } = new List<string>();

    // The action configuration for this hotkey.
    public ActionConfig Action { get; set; } = new ActionConfig();
}

// Represents the action to be performed when a hotkey is pressed.
public class ActionConfig
{
    // The name of the action, e.g., "SimulateLeftClick", "MoveCursorRelative".
    [JsonPropertyName("Action")] // Map "Action" from JSON to "Name" in C#
    public string Name { get; set; } = string.Empty;

    // Dictionary of arguments for the action, e.g., {"DeltaX": "50", "DeltaY": "50"}.
    public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
}
