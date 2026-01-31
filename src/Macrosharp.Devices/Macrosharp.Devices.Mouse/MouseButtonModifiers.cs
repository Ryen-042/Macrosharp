using Macrosharp.Devices.Core;

namespace Macrosharp.Devices.Mouse;

/// <summary>
/// A static class that tracks the current state of mouse buttons (pressed or released).
/// Similar to keyboard Modifiers, this enables combination detection.
/// </summary>
public static class MouseButtonModifiers
{
    // Button masks matching MouseButtons enum values
    /// <summary>Left button mask.</summary>
    public const int LEFT = (int)MouseButtons.Left;

    /// <summary>Right button mask.</summary>
    public const int RIGHT = (int)MouseButtons.Right;

    /// <summary>Middle button mask.</summary>
    public const int MIDDLE = (int)MouseButtons.Middle;

    /// <summary>XButton1 mask.</summary>
    public const int XBUTTON1 = (int)MouseButtons.XButton1;

    /// <summary>XButton2 mask.</summary>
    public const int XBUTTON2 = (int)MouseButtons.XButton2;

    // Common combinations
    /// <summary>Left + Right buttons.</summary>
    public const int LEFT_RIGHT = LEFT | RIGHT;

    /// <summary>Left + Middle buttons.</summary>
    public const int LEFT_MIDDLE = LEFT | MIDDLE;

    /// <summary>Right + Middle buttons.</summary>
    public const int RIGHT_MIDDLE = RIGHT | MIDDLE;

    /// <summary>All three main buttons.</summary>
    public const int LEFT_RIGHT_MIDDLE = LEFT | RIGHT | MIDDLE;

    /// <summary>
    /// An integer packing the states of the mouse buttons (pressed or not).
    /// Use HasButton() to check specific buttons.
    /// </summary>
    public static int CurrentButtons { get; private set; } = 0;

    /// <summary>
    /// A hash set of all button values for quick lookup.
    /// </summary>
    public static readonly HashSet<MouseButtons> AllButtons = new() { MouseButtons.Left, MouseButtons.Right, MouseButtons.Middle, MouseButtons.XButton1, MouseButtons.XButton2 };

    /// <summary>
    /// Updates the state of the mouse buttons based on a mouse event.
    /// Should be called from the mouse hook's callback.
    /// </summary>
    /// <param name="e">The mouse event.</param>
    public static void UpdateButtonState(MouseEvent e)
    {
        if (e.ButtonState == null || e.Button == MouseButtons.None)
            return;

        int buttonMask = (int)e.Button;

        if (e.ButtonState == MouseButtonState.Down)
        {
            CurrentButtons |= buttonMask;
        }
        else // Up
        {
            CurrentButtons &= ~buttonMask;
        }
    }

    /// <summary>
    /// Checks if specific button(s) are currently pressed.
    /// </summary>
    /// <param name="buttons">The button(s) to check (can be a combination).</param>
    /// <returns>True if all specified buttons are currently pressed.</returns>
    public static bool HasButton(MouseButtons buttons)
    {
        int mask = (int)buttons;
        return (CurrentButtons & mask) == mask;
    }

    /// <summary>
    /// Checks if specific button(s) are currently pressed using a bitmask.
    /// </summary>
    /// <param name="buttonMask">The button bitmask to check.</param>
    /// <returns>True if all specified buttons are currently pressed.</returns>
    public static bool HasButton(int buttonMask)
    {
        return (CurrentButtons & buttonMask) == buttonMask;
    }

    /// <summary>
    /// Checks if the current buttons exactly match the specified mask (no extra buttons pressed).
    /// </summary>
    /// <param name="buttonMask">The exact button combination to match.</param>
    /// <returns>True if exactly these buttons are pressed.</returns>
    public static bool HasExactButtons(int buttonMask)
    {
        return CurrentButtons == buttonMask;
    }

    /// <summary>
    /// Returns a boolean array representing the states of all mouse buttons.
    /// </summary>
    /// <returns>[Left, Right, Middle, XButton1, XButton2]</returns>
    public static bool[] GetButtonStates()
    {
        return new bool[] { HasButton(MouseButtons.Left), HasButton(MouseButtons.Right), HasButton(MouseButtons.Middle), HasButton(MouseButtons.XButton1), HasButton(MouseButtons.XButton2) };
    }

    /// <summary>
    /// Returns a string representation of a button mask.
    /// </summary>
    /// <param name="buttonMask">The button bitmask.</param>
    /// <returns>A string like "Left+Right" or "Middle".</returns>
    public static string GetButtonsStringFromMask(int buttonMask)
    {
        if (buttonMask == 0)
            return "None";

        var buttons = new System.Text.StringBuilder();

        if ((buttonMask & LEFT) != 0)
            buttons.Append("Left+");
        if ((buttonMask & RIGHT) != 0)
            buttons.Append("Right+");
        if ((buttonMask & MIDDLE) != 0)
            buttons.Append("Middle+");
        if ((buttonMask & XBUTTON1) != 0)
            buttons.Append("XButton1+");
        if ((buttonMask & XBUTTON2) != 0)
            buttons.Append("XButton2+");

        // Remove trailing '+'
        if (buttons.Length > 0)
            buttons.Length -= 1;

        return buttons.ToString();
    }

    /// <summary>
    /// Returns the current buttons as a MouseButtons flags value.
    /// </summary>
    public static MouseButtons GetCurrentButtonsAsFlags()
    {
        return (MouseButtons)CurrentButtons;
    }

    /// <summary>
    /// Calculates a button bitmask from a list of button names.
    /// </summary>
    /// <param name="buttonNames">A list of button names (e.g., ["Left", "Right"]).</param>
    /// <returns>An integer representing the combined bitmask.</returns>
    public static int GetButtonMaskFromNames(IEnumerable<string> buttonNames)
    {
        var buttonMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Left", LEFT },
            { "Right", RIGHT },
            { "Middle", MIDDLE },
            { "XButton1", XBUTTON1 },
            { "XButton2", XBUTTON2 },
        };

        int mask = 0;
        foreach (var name in buttonNames)
        {
            if (buttonMap.TryGetValue(name, out int buttonBit))
            {
                mask |= buttonBit;
            }
            else
            {
                Console.WriteLine($"Warning: Unknown button name '{name}'. Ignoring.");
            }
        }
        return mask;
    }

    /// <summary>
    /// Resets all button states to unpressed.
    /// Useful when the application loses focus or for testing.
    /// </summary>
    public static void Reset()
    {
        CurrentButtons = 0;
    }
}
