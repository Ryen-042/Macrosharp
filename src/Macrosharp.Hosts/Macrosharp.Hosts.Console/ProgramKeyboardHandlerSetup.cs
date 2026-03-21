using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Devices.Mouse;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.Hosts.ConsoleHost;

internal static class ProgramKeyboardHandlerSetup
{
    public static void SetupTerminalKeyLoggingHandler(KeyboardHookManager keyboardHookManager, Func<bool> isTerminalMessagesEnabled)
    {
        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (!isTerminalMessagesEnabled() || e.IsInjected)
            {
                return;
            }

            if (Modifiers.ModifierKeys.Contains(e.KeyCode))
            {
                return;
            }

            Console.WriteLine(FormatTerminalKeyMessage(e));
        };
    }

    public static void SetupBurstClickEscapeStopHandler(KeyboardHookManager keyboardHookManager, Func<bool> isBurstClickActive, Action stopBurstClick)
    {
        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (e.Handled)
            {
                return;
            }

            if (e.KeyCode != VirtualKey.ESCAPE)
            {
                return;
            }

            if (Modifiers.CurrentModifiers != 0)
            {
                return;
            }

            if (!isBurstClickActive())
            {
                return;
            }

            stopBurstClick();
            e.Handled = true;
        };
    }

    public static void SetupScrollLockMouseHandler(KeyboardHookManager keyboardHookManager, Func<bool> isPaused)
    {
        bool leftMouseHeld = false;
        bool rightMouseHeld = false;
        bool middleMouseHeld = false;

        keyboardHookManager.KeyDownHandler += (_, e) =>
        {
            if (e.Handled || isPaused())
            {
                return;
            }

            if (!Modifiers.IsScrollLockOn)
            {
                return;
            }

            if (Modifiers.HasModifier(Modifiers.CTRL) || Modifiers.HasModifier(Modifiers.WIN))
            {
                return;
            }

            if (Modifiers.ModifierKeys.Contains(e.KeyCode))
            {
                return;
            }

            bool isAlt = Modifiers.HasModifier(Modifiers.ALT);
            bool isShift = Modifiers.HasModifier(Modifiers.SHIFT);
            bool isBacktick = Modifiers.HasModifier(Modifiers.BACKTICK);

            switch (e.KeyCode)
            {
                case VirtualKey.KEY_W when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? 8 : 3, direction: 1));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_S when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? -8 : -3, direction: 1));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_A when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? -8 : -3, direction: 0));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_D when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseScroll(steps: isAlt ? 8 : 3, direction: 0));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_Q when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.LeftButton));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_E when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.RightButton));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_2 when !isBacktick:
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.MiddleButton));
                    e.Handled = true;
                    return;
                case VirtualKey.KEY_Q when isBacktick:
                {
                    leftMouseHeld = !leftMouseHeld;
                    var op = leftMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.LeftButton, op: op));
                    e.Handled = true;
                    return;
                }
                case VirtualKey.KEY_E when isBacktick:
                {
                    rightMouseHeld = !rightMouseHeld;
                    var op = rightMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.RightButton, op: op));
                    e.Handled = true;
                    return;
                }
                case VirtualKey.KEY_2 when isBacktick:
                {
                    middleMouseHeld = !middleMouseHeld;
                    var op = middleMouseHeld ? MouseEventOperation.MouseDown : MouseEventOperation.MouseUp;
                    Task.Run(() => MouseSimulator.SendMouseClick(button: MouseButton.MiddleButton, op: op));
                    e.Handled = true;
                    return;
                }
                case VirtualKey.OEM_1:
                    Task.Run(() => MouseSimulator.MoveCursor(dx: isAlt ? 80 : isShift ? 3 : 20, dy: 0));
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_7:
                    Task.Run(() => MouseSimulator.MoveCursor(dx: 0, dy: isAlt ? 80 : isShift ? 3 : 20));
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_2:
                    Task.Run(() => MouseSimulator.MoveCursor(dx: isAlt ? -80 : isShift ? -3 : -20, dy: 0));
                    e.Handled = true;
                    return;
                case VirtualKey.OEM_PERIOD:
                    Task.Run(() => MouseSimulator.MoveCursor(dx: 0, dy: isAlt ? -80 : isShift ? -3 : -20));
                    e.Handled = true;
                    return;
            }
        };
    }

    private static string FormatTerminalKeyMessage(KeyboardEvent e)
    {
        var scanCode = PInvoke.MapVirtualKey((uint)e.KeyCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
        var isCapsLockOn = Modifiers.IsCapsLockOn;
        var displayName = KeysMapper.GetDisplayName(e.KeyCode, e.IsShiftDown, isCapsLockOn);
        var asciiCode = KeysMapper.GetAsciiCode(e.KeyCode, e.IsShiftDown, isCapsLockOn);
        var pressedModifiers = Modifiers.GetModifiersStringFromMask(Modifiers.CurrentModifiers);
        if (string.IsNullOrWhiteSpace(pressedModifiers))
        {
            pressedModifiers = "None";
        }

        return $"[Key] {displayName}, VK={(ushort)e.KeyCode,3}, SC={scanCode,3}, ASCII={asciiCode,-3} | Modifiers={pressedModifiers} ({Modifiers.CurrentModifiers}) | Ext={e.IsExtendedKey}, Inj={e.IsInjected}, Alt={e.IsAltDown} | Caps={Modifiers.IsCapsLockOn}, Num={Modifiers.IsNumLockOn}, Scroll={Modifiers.IsScrollLockOn}";
    }
}
