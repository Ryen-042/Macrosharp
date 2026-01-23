using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Macrosharp.Devices.Keyboard;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.DynamicWindow;

public class SimpleWindow
{
    private readonly string className = "SimpleWindowClass";
    private readonly string title;
    private int computedWidth = 500;
    private int computedHeight = 300;
    private readonly int labelWidth;
    private readonly int inputFieldWidth;
    private readonly int itemsHeight;
    private readonly int xSep;
    private readonly int ySep;
    private readonly int xOffset;
    private readonly int yOffset;
    private HWND hwnd;
    private HWND hButtonOK;
    private List<HWND> hEditFields = new();
    public List<string> userInputs = new();

    // Key capture fields
    private HWND hKeyDisplay;
    private HWND hCaptureButton;
    private HWND hFinishCaptureButton;
    private bool isCapturing;
    public uint capturedKeyVK;
    public uint capturedKeyScanCode;
    public string? capturedKeyName;
    public string? capturedKeySequence;
    private bool showKeyCaptureField;
    private bool enableKeyCapture = false;
    private readonly HashSet<ModifierKind> pendingModifiers = new();
    private readonly List<KeyPress> capturedPresses = new();
    private readonly HashSet<VirtualKey> nonModifierKeysUsed = new();
    private bool captureFinished;
    private const int OK_BUTTON_ID = 1;
    private const int CAPTURE_BUTTON_ID = 2;
    private const int FINISH_CAPTURE_BUTTON_ID = 3;

    private enum ModifierKind
    {
        Ctrl,
        Shift,
        Alt,
        Win,
    }

    private readonly record struct KeyPress(VirtualKey Key, IReadOnlyList<ModifierKind> Modifiers);

    public SimpleWindow(string title, int labelWidth = 150, int inputFieldWidth = 250, int itemsHeight = 20, int xSep = 10, int ySep = 10, int xOffset = 10, int yOffset = 10)
    {
        this.title = title;
        this.labelWidth = labelWidth;
        this.inputFieldWidth = inputFieldWidth;
        this.itemsHeight = itemsHeight;
        this.xSep = xSep;
        this.ySep = ySep;
        this.xOffset = xOffset;
        this.yOffset = yOffset;

        // Make the application DPI aware
        PInvoke.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
    }

    private unsafe LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        switch (message)
        {
            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                PInvoke.UnregisterClass(className, default);
                return (LRESULT)0;

            case PInvoke.WM_COMMAND:
                ushort controlId = (ushort)LOWORD(wParam);
                if (controlId == OK_BUTTON_ID) // OK button
                {
                    if (showKeyCaptureField && capturedPresses.Count == 0 && !captureFinished)
                    {
                        var warningResult = PInvoke.MessageBox(
                            hwnd,
                            "No keys were captured.\n\nTo capture: click 'Press to Capture Key', then press a key (or press a modifier then a key), and click Finish.",
                            "No Keys Captured",
                            MESSAGEBOX_STYLE.MB_OKCANCEL | MESSAGEBOX_STYLE.MB_ICONWARNING
                        );
                        if (warningResult == MESSAGEBOX_RESULT.IDCANCEL)
                        {
                            return (LRESULT)0;
                        }
                    }

                    char[] bufferArray = ArrayPool<char>.Shared.Rent(256);
                    try
                    {
                        int bufferLength = Math.Min(256, bufferArray.Length);
                        foreach (var hEdit in hEditFields)
                        {
                            Array.Clear(bufferArray, 0, bufferLength);
                            fixed (char* pBuffer = bufferArray)
                            {
                                PInvoke.GetWindowText(hEdit, pBuffer, bufferLength);
                                userInputs.Add(new string(pBuffer));
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(bufferArray);
                    }
                    DestroyWindow();
                    return (LRESULT)0;
                }
                else if (controlId == CAPTURE_BUTTON_ID)
                {
                    StartKeyCapture();
                    return (LRESULT)0;
                }
                else if (controlId == FINISH_CAPTURE_BUTTON_ID)
                {
                    FinishKeyCapture();
                    return (LRESULT)0;
                }
                break;

            case PInvoke.WM_KEYDOWN:
                if (isCapturing)
                {
                    if ((lParam.Value & (1 << 30)) != 0)
                    {
                        return (LRESULT)0;
                    }

                    var key = (VirtualKey)wParam.Value;
                    if (key == VirtualKey.ESCAPE)
                    {
                        CancelKeyCapture();
                        return (LRESULT)0;
                    }

                    if (IsModifierKey(key))
                    {
                        pendingModifiers.Add(ToModifierKind(key));
                        UpdateCaptureDisplay(includePending: true);
                        return (LRESULT)0;
                    }

                    var combinedModifiers = GetActiveModifiers();
                    foreach (var modifier in pendingModifiers)
                    {
                        combinedModifiers.Add(modifier);
                    }

                    if (combinedModifiers.Count == 0)
                    {
                        if (!nonModifierKeysUsed.Add(key))
                        {
                            UpdateCaptureDisplay(includePending: false);
                            return (LRESULT)0;
                        }
                    }

                    AddCompletedPress(key, combinedModifiers);
                    pendingModifiers.Clear();
                    UpdateCaptureDisplay(includePending: false);

                    if (capturedPresses.Count >= 3)
                    {
                        FinishKeyCapture();
                    }
                    else
                    {
                        ShowFinishCaptureButton();
                    }
                }
                else if (wParam.Value == (int)VirtualKey.RETURN)
                {
                    var buttonHwnd = PInvoke.GetDlgItem(hwnd, OK_BUTTON_ID);
                    PInvoke.SendMessage(buttonHwnd, PInvoke.BM_CLICK, 0, 0);
                    return (LRESULT)0;
                }
                else if (wParam.Value == (int)VirtualKey.ESCAPE)
                {
                    DestroyWindow();
                    return (LRESULT)0;
                }
                break;

            case PInvoke.WM_KEYUP:
                if (isCapturing)
                {
                    var key = (VirtualKey)wParam.Value;
                    if (IsModifierKey(key))
                    {
                        pendingModifiers.Clear();
                        foreach (var modifier in GetActiveModifiers())
                        {
                            pendingModifiers.Add(modifier);
                        }
                        UpdateCaptureDisplay(includePending: pendingModifiers.Count > 0);
                    }
                }
                break;

            case PInvoke.WM_GETMINMAXINFO:
                unsafe
                {
                    MINMAXINFO* minMaxInfo = (MINMAXINFO*)lParam.Value;
                    minMaxInfo->ptMinTrackSize.X = computedWidth;
                    minMaxInfo->ptMinTrackSize.Y = computedHeight;
                    minMaxInfo->ptMaxTrackSize.Y = computedHeight;
                }
                return (LRESULT)0;

            case PInvoke.WM_SIZE:
                int width = LOWORD(lParam);
                if (showKeyCaptureField)
                {
                    PInvoke.SetWindowPos(hCaptureButton, HWND.Null, 0, 0, width - (labelWidth + xSep + 2 * xOffset), itemsHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

                    PInvoke.SetWindowPos(hFinishCaptureButton, HWND.Null, 0, 0, width - 2 * xOffset, itemsHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
                }

                foreach (var hEdit in hEditFields)
                {
                    PInvoke.SetWindowPos(hEdit, HWND.Null, 0, 0, width - (labelWidth + xSep + 2 * xOffset), itemsHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
                }

                PInvoke.SetWindowPos(hButtonOK, HWND.Null, 0, 0, width - 2 * xOffset, itemsHeight + 5, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
                return (LRESULT)0;
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }

    public void CreateDynamicInputWindow(IReadOnlyList<string> inputLabels, IReadOnlyList<string>? placeholders = null, bool enableKeyCapture = false)
    {
        this.enableKeyCapture = enableKeyCapture;
        showKeyCaptureField = enableKeyCapture;

        if (placeholders != null && placeholders.Count != inputLabels.Count)
        {
            Console.WriteLine("Warning! The number of placeholders should be equal to the number of input labels. Continuing without placeholders...");
            placeholders = null;
        }

        unsafe
        {
            fixed (char* ptrClassName = this.className)
            {
                // Register window class
                var wndClass = new WNDCLASSW
                {
                    style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                    lpfnWndProc = new WNDPROC(WndProc),
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION),
                    hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                    hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH).Value),
                    lpszClassName = new PCWSTR(ptrClassName),
                };
                try
                {
                    PInvoke.RegisterClass(wndClass);

                    // Adjust window dimensions
                    computedWidth = labelWidth + inputFieldWidth + xSep + 2 * xOffset;
                    int capturedKeyHeight = showKeyCaptureField ? (itemsHeight + ySep) * 2 : 0;
                    computedHeight = 60 + capturedKeyHeight + (itemsHeight + ySep) * inputLabels.Count + itemsHeight + 2 * yOffset;

                    // Create main window
                    hwnd = PInvoke.CreateWindowEx(
                        WINDOW_EX_STYLE.WS_EX_TOPMOST,
                        className,
                        title,
                        WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
                        PInvoke.CW_USEDEFAULT,
                        PInvoke.CW_USEDEFAULT,
                        computedWidth,
                        computedHeight,
                        default,
                        default,
                        new SafeFileHandle(wndClass.hInstance, ownsHandle: false),
                        null
                    );
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to register window class as it already exists. Continuing...");
                }
            }
        }

        placeholders ??= Enumerable.Repeat(string.Empty, inputLabels.Count).ToList();
        int yPos = yOffset;

        if (showKeyCaptureField)
        {
            CreateKeyCaptureControls(ref yPos);
        }

        // Create Label and Edit controls
        for (int i = 0; i < inputLabels.Count; i++)
        {
            CreateLabelAndEditControl(inputLabels[i], placeholders[i], ref yPos);
        }

        // Create OK button
        CreateOKButton(yPos);

        // Show window
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(hwnd);

        // Message loop
        MSG msg = new();
        while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
    }

    private unsafe void CreateKeyCaptureControls(ref int yPos)
    {
        // Key display field
        hKeyDisplay = PInvoke.CreateWindowEx(0, "STATIC", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_BORDER, xOffset, yPos, labelWidth, itemsHeight, hwnd, default, PInvoke.GetModuleHandle((string?)null), null);

        // Capture button
        hCaptureButton = PInvoke.CreateWindowEx(
            0,
            "BUTTON",
            "Press to Capture Key",
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP,
            xOffset + labelWidth + xSep,
            yPos,
            inputFieldWidth,
            itemsHeight,
            hwnd,
            new SafeFileHandle((IntPtr)CAPTURE_BUTTON_ID, ownsHandle: false),
            PInvoke.GetModuleHandle((string?)null),
            null
        );

        yPos += itemsHeight + ySep;

        // Finish capture button (hidden until first press)
        hFinishCaptureButton = PInvoke.CreateWindowEx(
            0,
            "BUTTON",
            "Finish capture",
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_TABSTOP,
            xOffset,
            yPos,
            labelWidth + inputFieldWidth + xSep,
            itemsHeight,
            hwnd,
            new SafeFileHandle((IntPtr)FINISH_CAPTURE_BUTTON_ID, ownsHandle: false),
            PInvoke.GetModuleHandle((string?)null),
            null
        );

        PInvoke.ShowWindow(hFinishCaptureButton, SHOW_WINDOW_CMD.SW_HIDE);

        yPos += itemsHeight + ySep;
    }

    private unsafe void CreateLabelAndEditControl(string label, string placeholder, ref int yPos)
    {
        // Label
        PInvoke.CreateWindowEx(0, "STATIC", label, WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, xOffset, yPos, labelWidth, itemsHeight, hwnd, default, PInvoke.GetModuleHandle((string?)null), null);

        // Edit control
        var hEdit = PInvoke.CreateWindowEx(
            0,
            "EDIT",
            placeholder,
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_BORDER | WINDOW_STYLE.WS_TABSTOP,
            xOffset + labelWidth + xSep,
            yPos,
            inputFieldWidth,
            itemsHeight,
            hwnd,
            default,
            PInvoke.GetModuleHandle((string?)null),
            null
        );

        hEditFields.Add(hEdit);
        yPos += itemsHeight + ySep;
    }

    private unsafe void CreateOKButton(int yPos)
    {
        hButtonOK = PInvoke.CreateWindowEx(
            0,
            "BUTTON",
            "OK",
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP,
            xOffset,
            yPos,
            labelWidth + inputFieldWidth + xSep,
            itemsHeight,
            hwnd,
            new SafeFileHandle(new HMENU((IntPtr)1), ownsHandle: false),
            PInvoke.GetModuleHandle((string?)null),
            null
        );
    }

    private unsafe void DestroyWindow()
    {
        PInvoke.DestroyWindow(hwnd);
        PInvoke.UnregisterClass(className, default);
    }

    private void StartKeyCapture()
    {
        if (!enableKeyCapture)
        {
            return;
        }

        capturedPresses.Clear();
        nonModifierKeysUsed.Clear();
        pendingModifiers.Clear();
        capturedKeySequence = null;
        captureFinished = false;
        isCapturing = true;

        PInvoke.SetWindowText(hCaptureButton, "Capturing...");
        PInvoke.SetWindowText(hKeyDisplay, "");
        PInvoke.ShowWindow(hFinishCaptureButton, SHOW_WINDOW_CMD.SW_HIDE);
        PInvoke.SetFocus(hwnd);
    }

    private void FinishKeyCapture()
    {
        if (capturedPresses.Count == 0)
        {
            return;
        }

        captureFinished = true;
        isCapturing = false;
        pendingModifiers.Clear();
        capturedKeySequence = BuildSequenceDisplay(includePending: false);
        PInvoke.SetWindowText(hKeyDisplay, capturedKeySequence ?? string.Empty);
        PInvoke.SetWindowText(hCaptureButton, "Press to Capture Key");
        PInvoke.ShowWindow(hFinishCaptureButton, SHOW_WINDOW_CMD.SW_HIDE);
    }

    private void CancelKeyCapture()
    {
        isCapturing = false;
        pendingModifiers.Clear();
        PInvoke.SetWindowText(hCaptureButton, "Press to Capture Key");
        UpdateCaptureDisplay(includePending: false);
        if (capturedPresses.Count > 0)
        {
            ShowFinishCaptureButton();
        }
    }

    private void ShowFinishCaptureButton()
    {
        PInvoke.ShowWindow(hFinishCaptureButton, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
    }

    private void AddCompletedPress(VirtualKey key, HashSet<ModifierKind> modifiers)
    {
        var orderedModifiers = OrderModifiers(modifiers);
        capturedPresses.Add(new KeyPress(key, orderedModifiers));
        capturedKeyVK = (uint)key;
        capturedKeyScanCode = PInvoke.MapVirtualKey((uint)key, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
        capturedKeyName = KeysMapper.GetDisplayName(key, orderedModifiers.Contains(ModifierKind.Shift), IsCapsLockOn());
        capturedKeySequence = BuildSequenceDisplay(includePending: false);
    }

    private void UpdateCaptureDisplay(bool includePending)
    {
        string display = BuildSequenceDisplay(includePending);
        PInvoke.SetWindowText(hKeyDisplay, display);
    }

    private string BuildSequenceDisplay(bool includePending)
    {
        if (capturedPresses.Count == 0 && (!includePending || pendingModifiers.Count == 0))
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var press in capturedPresses)
        {
            parts.Add(FormatPress(press));
        }

        if (includePending && pendingModifiers.Count > 0)
        {
            var pending = string.Join("+", OrderModifiers(pendingModifiers).Select(GetModifierLabel));
            parts.Add($"{pending}+…");
        }

        return string.Join(", ", parts);
    }

    private static string FormatPress(KeyPress press)
    {
        var modifierText = press.Modifiers.Count == 0 ? string.Empty : string.Join("+", press.Modifiers.Select(GetModifierLabel)) + "+";

        bool shiftDown = press.Modifiers.Contains(ModifierKind.Shift);
        bool capsLockOn = IsCapsLockOn();
        string keyName = KeysMapper.GetDisplayName(press.Key, shiftDown, capsLockOn);
        return modifierText + keyName;
    }

    private static List<ModifierKind> OrderModifiers(HashSet<ModifierKind> modifiers)
    {
        var ordered = new List<ModifierKind>(4);
        if (modifiers.Contains(ModifierKind.Ctrl))
        {
            ordered.Add(ModifierKind.Ctrl);
        }

        if (modifiers.Contains(ModifierKind.Shift))
        {
            ordered.Add(ModifierKind.Shift);
        }

        if (modifiers.Contains(ModifierKind.Alt))
        {
            ordered.Add(ModifierKind.Alt);
        }

        if (modifiers.Contains(ModifierKind.Win))
        {
            ordered.Add(ModifierKind.Win);
        }
        return ordered;
    }

    private static HashSet<ModifierKind> GetActiveModifiers()
    {
        var modifiers = new HashSet<ModifierKind>();
        if (IsKeyDown(VirtualKey.CONTROL) || IsKeyDown(VirtualKey.LCONTROL) || IsKeyDown(VirtualKey.RCONTROL))
        {
            modifiers.Add(ModifierKind.Ctrl);
        }

        if (IsKeyDown(VirtualKey.SHIFT) || IsKeyDown(VirtualKey.LSHIFT) || IsKeyDown(VirtualKey.RSHIFT))
        {
            modifiers.Add(ModifierKind.Shift);
        }

        if (IsKeyDown(VirtualKey.MENU) || IsKeyDown(VirtualKey.LMENU) || IsKeyDown(VirtualKey.RMENU))
        {
            modifiers.Add(ModifierKind.Alt);
        }

        if (IsKeyDown(VirtualKey.LWIN) || IsKeyDown(VirtualKey.RWIN))
        {
            modifiers.Add(ModifierKind.Win);
        }

        return modifiers;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        return (PInvoke.GetKeyState((int)key) & 0x8000) != 0;
    }

    private static bool IsModifierKey(VirtualKey key)
    {
        return key == VirtualKey.CONTROL
            || key == VirtualKey.LCONTROL
            || key == VirtualKey.RCONTROL
            || key == VirtualKey.SHIFT
            || key == VirtualKey.LSHIFT
            || key == VirtualKey.RSHIFT
            || key == VirtualKey.MENU
            || key == VirtualKey.LMENU
            || key == VirtualKey.RMENU
            || key == VirtualKey.LWIN
            || key == VirtualKey.RWIN;
    }

    private static ModifierKind ToModifierKind(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.CONTROL or VirtualKey.LCONTROL or VirtualKey.RCONTROL => ModifierKind.Ctrl,
            VirtualKey.SHIFT or VirtualKey.LSHIFT or VirtualKey.RSHIFT => ModifierKind.Shift,
            VirtualKey.MENU or VirtualKey.LMENU or VirtualKey.RMENU => ModifierKind.Alt,
            VirtualKey.LWIN or VirtualKey.RWIN => ModifierKind.Win,
            _ => ModifierKind.Ctrl,
        };
    }

    private static string GetModifierLabel(ModifierKind kind)
    {
        return kind switch
        {
            ModifierKind.Ctrl => "Ctrl",
            ModifierKind.Shift => "Shift",
            ModifierKind.Alt => "Alt",
            ModifierKind.Win => "Win",
            _ => "",
        };
    }

    private static bool IsCapsLockOn()
    {
        return (PInvoke.GetKeyState((int)VirtualKey.CAPITAL) & 1) != 0;
    }

    private static int LOWORD(LPARAM value) => (short)(value.Value & 0xFFFF);

    private static int LOWORD(WPARAM value) => (short)(value.Value & 0xFFFF);

    private static int HIWORD(LPARAM value) => (short)((value.Value >> 16) & 0xFFFF);

    private static int HIWORD(WPARAM value) => (short)((value.Value >> 16) & 0xFFFF);
}
