using Macrosharp.Devices.Core;

namespace Macrosharp.Devices.Mouse;

/// <summary>
/// Represents a mouse binding (button or scroll action with optional held buttons).
/// </summary>
public readonly struct MouseBinding : IEquatable<MouseBinding>
{
    /// <summary>The trigger button for button bindings.</summary>
    public MouseButtons TriggerButton { get; }

    /// <summary>The bitmask of buttons that must be held for this binding to activate.</summary>
    public int HeldButtons { get; }

    /// <summary>The scroll direction for scroll bindings (null for button bindings).</summary>
    public ScrollDirection? ScrollDirection { get; }

    /// <summary>True if this is a scroll binding, false if it's a button binding.</summary>
    public bool IsScrollBinding => ScrollDirection.HasValue;

    /// <summary>Creates a button binding.</summary>
    /// <param name="triggerButton">The button that triggers the action.</param>
    /// <param name="heldButtons">Buttons that must be held when trigger is pressed.</param>
    public MouseBinding(MouseButtons triggerButton, int heldButtons = 0)
    {
        TriggerButton = triggerButton;
        HeldButtons = heldButtons;
        ScrollDirection = null;
    }

    /// <summary>Creates a scroll binding.</summary>
    /// <param name="direction">The scroll direction (Vertical or Horizontal).</param>
    /// <param name="heldButtons">Buttons that must be held when scrolling.</param>
    public MouseBinding(ScrollDirection direction, int heldButtons = 0)
    {
        TriggerButton = MouseButtons.None;
        HeldButtons = heldButtons;
        ScrollDirection = direction;
    }

    /// <inheritdoc/>
    public bool Equals(MouseBinding other)
    {
        return TriggerButton == other.TriggerButton &&
               HeldButtons == other.HeldButtons &&
               ScrollDirection == other.ScrollDirection;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MouseBinding other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(TriggerButton, HeldButtons, ScrollDirection);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string heldStr = HeldButtons > 0 ? MouseButtonModifiers.GetButtonsStringFromMask(HeldButtons) + "+" : "";

        if (IsScrollBinding)
        {
            return $"{heldStr}Scroll{ScrollDirection}";
        }
        else
        {
            return $"{heldStr}{TriggerButton}";
        }
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(MouseBinding left, MouseBinding right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(MouseBinding left, MouseBinding right) => !left.Equals(right);
}

/// <summary>
/// Manages registration and execution of mouse bindings.
/// Similar to HotkeyManager for keyboard.
/// </summary>
public class MouseBindingManager : IDisposable
{
    private readonly MouseHookManager _mouseHookManager;

    // Stores registered button bindings and their actions
    private readonly Dictionary<MouseBinding, Action> _registeredButtonBindings = new();

    // Stores registered scroll bindings with their scroll-delta actions
    private readonly Dictionary<MouseBinding, Action<short>> _registeredScrollBindings = new();

    // Tracks currently active binding to prevent repeated firing while held
    private MouseBinding? _activeBinding = null;

    /// <summary>Initializes a new instance of the <see cref="MouseBindingManager"/> class.</summary>
    /// <param name="mouseHookManager">An instance of the mouse hook manager to subscribe to.</param>
    public MouseBindingManager(MouseHookManager mouseHookManager)
    {
        _mouseHookManager = mouseHookManager ?? throw new ArgumentNullException(nameof(mouseHookManager));
        _mouseHookManager.MouseButtonDownHandler += OnMouseButtonDown;
        _mouseHookManager.MouseButtonUpHandler += OnMouseButtonUp;
        _mouseHookManager.MouseScrollHandler += OnMouseScroll;
    }

    #region Button Bindings

    /// <summary>
    /// Registers a binding for a single mouse button press.
    /// </summary>
    /// <param name="button">The trigger button.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterBinding(MouseButtons button, Action action)
    {
        return RegisterBinding(button, 0, action);
    }

    /// <summary>
    /// Registers a binding for a mouse button press with other buttons held.
    /// </summary>
    /// <param name="triggerButton">The button that triggers the action.</param>
    /// <param name="heldButtons">Buttons that must be held (as MouseButtons flags).</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterBinding(MouseButtons triggerButton, MouseButtons heldButtons, Action action)
    {
        return RegisterBinding(triggerButton, (int)heldButtons, action);
    }

    /// <summary>
    /// Registers a binding for a mouse button press with other buttons held.
    /// </summary>
    /// <param name="triggerButton">The button that triggers the action.</param>
    /// <param name="heldButtonsMask">Bitmask of buttons that must be held.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterBinding(MouseButtons triggerButton, int heldButtonsMask, Action action)
    {
        var binding = new MouseBinding(triggerButton, heldButtonsMask);

        if (_registeredButtonBindings.ContainsKey(binding))
        {
            Console.WriteLine($"Mouse binding '{binding}' is already registered.");
            return false;
        }

        _registeredButtonBindings.Add(binding, action);
        return true;
    }

    /// <summary>
    /// Registers a binding with generic action parameters.
    /// </summary>
    public bool RegisterBinding<T1>(MouseButtons triggerButton, int heldButtonsMask, Action<T1> action, T1 arg1)
    {
        return RegisterBinding(triggerButton, heldButtonsMask, () => action(arg1));
    }

    /// <summary>
    /// Registers a binding with two generic action parameters.
    /// </summary>
    public bool RegisterBinding<T1, T2>(MouseButtons triggerButton, int heldButtonsMask, Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        return RegisterBinding(triggerButton, heldButtonsMask, () => action(arg1, arg2));
    }

    /// <summary>
    /// Unregisters a button binding.
    /// </summary>
    /// <param name="triggerButton">The trigger button of the binding.</param>
    /// <param name="heldButtonsMask">The held buttons mask of the binding.</param>
    /// <returns>True if unregistered successfully; false if not found.</returns>
    public bool UnregisterBinding(MouseButtons triggerButton, int heldButtonsMask = 0)
    {
        var binding = new MouseBinding(triggerButton, heldButtonsMask);
        bool removed = _registeredButtonBindings.Remove(binding);

        if (removed)
        {
            Console.WriteLine($"Unregistered mouse binding: {binding}");
            if (_activeBinding.HasValue && _activeBinding.Value.Equals(binding))
            {
                _activeBinding = null;
            }
        }
        else
        {
            Console.WriteLine($"Mouse binding '{binding}' not found for unregistration.");
        }

        return removed;
    }

    #endregion

    #region Scroll Bindings

    /// <summary>
    /// Registers a scroll binding with no held buttons.
    /// </summary>
    /// <param name="direction">The scroll direction (Vertical or Horizontal).</param>
    /// <param name="action">Action receiving the wheel delta.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterScrollBinding(ScrollDirection direction, Action<short> action)
    {
        return RegisterScrollBinding(direction, 0, action);
    }

    /// <summary>
    /// Registers a scroll binding with held button requirement.
    /// </summary>
    /// <param name="direction">The scroll direction.</param>
    /// <param name="heldButtons">Buttons that must be held while scrolling.</param>
    /// <param name="action">Action receiving the wheel delta.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterScrollBinding(ScrollDirection direction, MouseButtons heldButtons, Action<short> action)
    {
        return RegisterScrollBinding(direction, (int)heldButtons, action);
    }

    /// <summary>
    /// Registers a scroll binding with held button requirement (bitmask).
    /// </summary>
    /// <param name="direction">The scroll direction.</param>
    /// <param name="heldButtonsMask">Bitmask of buttons that must be held.</param>
    /// <param name="action">Action receiving the wheel delta.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool RegisterScrollBinding(ScrollDirection direction, int heldButtonsMask, Action<short> action)
    {
        var binding = new MouseBinding(direction, heldButtonsMask);

        if (_registeredScrollBindings.ContainsKey(binding))
        {
            Console.WriteLine($"Scroll binding '{binding}' is already registered.");
            return false;
        }

        _registeredScrollBindings.Add(binding, action);
        return true;
    }

    /// <summary>
    /// Unregisters a scroll binding.
    /// </summary>
    /// <param name="direction">The scroll direction.</param>
    /// <param name="heldButtonsMask">The held buttons mask.</param>
    /// <returns>True if unregistered successfully; false if not found.</returns>
    public bool UnregisterScrollBinding(ScrollDirection direction, int heldButtonsMask = 0)
    {
        var binding = new MouseBinding(direction, heldButtonsMask);
        bool removed = _registeredScrollBindings.Remove(binding);

        if (removed)
        {
            Console.WriteLine($"Unregistered scroll binding: {binding}");
        }
        else
        {
            Console.WriteLine($"Scroll binding '{binding}' not found for unregistration.");
        }

        return removed;
    }

    #endregion

    #region Event Handlers

    private void OnMouseButtonDown(object? sender, MouseEvent e)
    {
        if (e.Handled)
            return;

        // Get the currently held buttons BEFORE this button was pressed
        // (the hook already updated the state, so we need to exclude the trigger)
        int heldBeforeTrigger = MouseButtonModifiers.CurrentButtons & ~(int)e.Button;

        var binding = new MouseBinding(e.Button, heldBeforeTrigger);

        if (_registeredButtonBindings.TryGetValue(binding, out Action? action))
        {
            if (_activeBinding == null || !_activeBinding.Value.Equals(binding))
            {
                Console.WriteLine($"Mouse binding '{binding}' triggered. Executing action...");

                // Execute on Task.Run to avoid blocking the hook callback
                Task.Run(action);

                e.Handled = true;
                _activeBinding = binding;
            }
            else
            {
                // Binding already active, ignore repeat
                Console.WriteLine($"Mouse binding '{binding}' is already active. Ignoring repeat.");
            }
        }
    }

    private void OnMouseButtonUp(object? sender, MouseEvent e)
    {
        // Clear active binding if the trigger button was released
        if (_activeBinding.HasValue && _activeBinding.Value.TriggerButton == e.Button)
        {
            _activeBinding = null;
        }
    }

    private void OnMouseScroll(object? sender, MouseEvent e)
    {
        if (e.Handled || e.ScrollDirection == null)
            return;

        // Check current held buttons
        int heldButtons = MouseButtonModifiers.CurrentButtons;

        var binding = new MouseBinding(e.ScrollDirection.Value, heldButtons);

        if (_registeredScrollBindings.TryGetValue(binding, out Action<short>? action))
        {
            Console.WriteLine($"Scroll binding '{binding}' triggered. Delta={e.WheelDelta}");

            // Execute on Task.Run to avoid blocking the hook callback
            short delta = e.WheelDelta;
            Task.Run(() => action(delta));

            e.Handled = true;
        }
    }

    #endregion

    /// <summary>Disposes the manager and unsubscribes from events.</summary>
    public void Dispose()
    {
        _mouseHookManager.MouseButtonDownHandler -= OnMouseButtonDown;
        _mouseHookManager.MouseButtonUpHandler -= OnMouseButtonUp;
        _mouseHookManager.MouseScrollHandler -= OnMouseScroll;

        _registeredButtonBindings.Clear();
        _registeredScrollBindings.Clear();
        _activeBinding = null;

        Console.WriteLine("MouseBindingManager disposed.");
    }
}
