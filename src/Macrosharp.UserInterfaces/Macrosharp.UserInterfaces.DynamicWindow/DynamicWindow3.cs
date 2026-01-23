//using System;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using Windows.Win32;
//using Windows.Win32.Foundation;
//using Windows.Win32.UI.WindowsAndMessaging;
//using Windows.Win32.Graphics.Gdi;
//using Windows.Win32.UI.Controls;

//namespace Macrosharp.UserInterfaces.DynamicWindow;

//public class SimpleWindow3
//{
//    private const string ClassName = "SimpleWindowClass";
//    private const int CAPTURE_BUTTON_ID = 2;

//    private readonly string _title;
//    private readonly int _labelWidth;
//    private readonly int _inputWidth;
//    private readonly int _itemHeight;
//    private readonly int _xSep;
//    private readonly int _ySep;
//    private readonly int _xOffset;
//    private readonly int _yOffset;

//    private HWND _hwnd;
//    private HWND _hButtonOk;
//    private readonly List<HWND> _hEditFields = new();
//    private HWND _hKeyDisplay;
//    private HWND _hCaptureButton;
//    private bool _isCapturing;
//    private string _capturedKeyName = string.Empty;
//    private bool _showKeyCapture;

//    public List<string> UserInputs { get; } = new();

//    public SimpleWindow3(string title, int labelWidth = 150, int inputWidth = 250,
//        int itemHeight = 20, int xSep = 10, int ySep = 10, int xOffset = 10, int yOffset = 10)
//    {
//        _title = title;
//        _labelWidth = labelWidth;
//        _inputWidth = inputWidth;
//        _itemHeight = itemHeight;
//        _xSep = xSep;
//        _ySep = ySep;
//        _xOffset = xOffset;
//        _yOffset = yOffset;
//    }

//    public unsafe void CreateDynamicWindow(string[] inputLabels, string[]? placeholders = null, bool enableKeyCapture = false)
//    {
//        _showKeyCapture = enableKeyCapture;
//        placeholders ??= new string[inputLabels.Length];

//        var wndClass = new WNDCLASSW
//        {
//            style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
//            lpfnWndProc = WndProc,
//            hInstance = PInvoke.GetModuleHandle(
//            hCursor = PInvoke.LoadCursor(default, PInvoke.IDC_ARROW),
//            hbrBackground = PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH),
//            lpszClassName = ClassName
//        };

//        ushort atom = PInvoke.RegisterClass(wndClass);
//        if (atom == 0)
//            throw new InvalidOperationException("Window class registration failed");

//        int width = _labelWidth + _inputWidth + _xSep + 2 * _xOffset;
//        int keyCaptureHeight = _showKeyCapture ? _itemHeight + _ySep : 0;
//        int height = 60 + keyCaptureHeight + inputLabels.Length * (_itemHeight + _ySep) + 2 * _yOffset;

//        _hwnd = PInvoke.CreateWindowEx(
//            WINDOW_EX_STYLE.WS_EX_TOPMOST,
//            ClassName,
//            _title,
//            WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
//            0, 0,
//            width,
//            height,
//            default,
//            default,
//            wndClass.hInstance,
//            null
//        );

//        if (_hwnd.IsNull)
//            throw new InvalidOperationException("Window creation failed");

//        int yPos = _yOffset;

//        if (_showKeyCapture)
//        {
//            CreateKeyCaptureControls(ref yPos);
//            yPos += _itemHeight + _ySep;
//        }

//        for (int i = 0; i < inputLabels.Length; i++)
//        {
//            CreateLabel(inputLabels[i], yPos);
//            CreateEditField(placeholders[i], yPos);
//            yPos += _itemHeight + _ySep;
//        }

//        CreateOkButton(yPos);

//        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
//        PInvoke.UpdateWindow(_hwnd);

//        RunMessageLoop();
//    }

//    private unsafe void CreateKeyCaptureControls(ref int yPos)
//    {
//        _hKeyDisplay = PInvoke.CreateWindowEx(
//            0,
//            "STATIC",
//            "",
//            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (uint)STATIC_STYLES.SS_CENTER,
//            _xOffset,
//            yPos,
//            _labelWidth,
//            _itemHeight,
//            _hwnd,
//            default,
//            default,
//            null
//        );

//        _hCaptureButton = PInvoke.CreateWindowEx(
//            0,
//            "BUTTON",
//            "Press to Capture Key",
//            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (uint)BUTTON_STYLE.BS_PUSHBUTTON,
//            _xOffset + _labelWidth + _xSep,
//            yPos,
//            _inputWidth,
//            _itemHeight,
//            _hwnd,
//            (HMENU)(nint)CAPTURE_BUTTON_ID,
//            default,
//            null
//        );
//    }

//    private unsafe void CreateLabel(string text, int yPos)
//    {
//        PInvoke.CreateWindowEx(
//            0,
//            "STATIC",
//            text,
//            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (uint)STATIC_STYLES.SS_CENTER,
//            _xOffset,
//            yPos,
//            _labelWidth,
//            _itemHeight,
//            _hwnd,
//            default,
//            default,
//            null
//        );
//    }

//    private unsafe void CreateEditField(string placeholder, int yPos)
//    {
//        var hEdit = PInvoke.CreateWindowEx(
//            0,
//            "EDIT",
//            placeholder,
//            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_BORDER |
//            (uint)EDIT_STYLE.ES_AUTOHSCROLL,
//            _xOffset + _labelWidth + _xSep,
//            yPos,
//            _inputWidth,
//            _itemHeight,
//            _hwnd,
//            default,
//            default,
//            null
//        );

//        _hEditFields.Add(hEdit);
//    }

//    private unsafe void CreateOkButton(int yPos)
//    {
//        _hButtonOk = PInvoke.CreateWindowEx(
//            0,
//            "BUTTON",
//            "OK",
//            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (uint)BUTTON_STYLE.BS_PUSHBUTTON,
//            _xOffset,
//            yPos,
//            _labelWidth + _inputWidth + _xSep,
//            _itemHeight,
//            _hwnd,
//            (HMENU)(nint)1,
//            default,
//            null
//        );
//    }

//    private unsafe LRESULT WndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
//    {
//        switch (uMsg)
//        {
//            case PInvoke.WM_DESTROY:
//                PInvoke.PostQuitMessage(0);
//                return 0;

//            case PInvoke.WM_COMMAND:
//                int controlId = (int)wParam.Value;
//                if (controlId == 1) // OK button
//                {
//                    foreach (var hEdit in _hEditFields)
//                    {
//                        var length = PInvoke.GetWindowTextLength(hEdit) + 1;
//                        var buffer = new char[length];
//                        fixed (char* pBuffer = buffer)
//                        {
//                            PInvoke.GetWindowText(hEdit, pBuffer, length);
//                            UserInputs.Add(new string(pBuffer));
//                        }
//                    }
//                    PInvoke.DestroyWindow(hwnd);
//                    return 0;
//                }
//                else if (controlId == CAPTURE_BUTTON_ID)
//                {
//                    _isCapturing = true;
//                    PInvoke.SetWindowText(_hCaptureButton, "Press any key...");
//                    PInvoke.SetFocus(hwnd);
//                    return 0;
//                }
//                break;

//            case PInvoke.WM_KEYDOWN:
//                if (_isCapturing)
//                {
//                    HandleKeyCapture(wParam);
//                    return 0;
//                }
//                break;

//            case PInvoke.WM_SYSKEYDOWN:
//                if (_isCapturing && wParam.Value == PInvoke.VK_MENU)
//                {
//                    _capturedKeyName = "Alt";
//                    UpdateKeyDisplay();
//                    return 1;
//                }
//                break;

//            case PInvoke.WM_GETMINMAXINFO:
//                var mmi = (MINMAXINFO*)lParam.Value;
//                mmi->ptMinTrackSize.x = _labelWidth + _inputWidth + _xSep + 2 * _xOffset;
//                mmi->ptMinTrackSize.y = 60 + (_showKeyCapture ? _itemHeight + _ySep : 0) +
//                    (_itemHeight + _ySep) * _hEditFields.Count + 2 * _yOffset;
//                return 0;

//            case PInvoke.WM_SIZE:
//                int newWidth = (ushort)lParam.Value;
//                foreach (var hEdit in _hEditFields)
//                {
//                    PInvoke.SetWindowPos(
//                        hEdit,
//                        default,
//                        0, 0,
//                        newWidth - (_labelWidth + _xSep + 2 * _xOffset),
//                        _itemHeight,
//                        SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
//                    );
//                }
//                break;
//        }

//        return PInvoke.DefWindowProc(hwnd, uMsg, wParam, lParam);
//    }

//    private void HandleKeyCapture(WPARAM wParam)
//    {
//        uint scanCode = PInvoke.MapVirtualKey((uint)wParam.Value, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
//        char[] buffer = new char[256];

//        if (PInvoke.GetKeyNameText((int)(scanCode << 16), buffer, buffer.Length) > 0)
//        {
//            _capturedKeyName = new string(buffer).TrimEnd('\0');
//            UpdateKeyDisplay();
//        }
//    }

//    private void UpdateKeyDisplay()
//    {
//        PInvoke.SetWindowText(_hKeyDisplay, _capturedKeyName);
//        PInvoke.SetWindowText(_hCaptureButton, "Press to Capture Key");
//        _isCapturing = false;
//    }

//    private void RunMessageLoop()
//    {
//        while (PInvoke.GetMessage(out var msg, default, 0, 0) != false)
//        {
//            PInvoke.TranslateMessage(msg);
//            PInvoke.DispatchMessage(msg);
//        }
//    }
//}