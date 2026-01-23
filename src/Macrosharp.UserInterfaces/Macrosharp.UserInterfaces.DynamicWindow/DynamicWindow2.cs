//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Macrosharp.Win32.Native;
//using Windows.Win32;
//using Windows.Win32.Foundation;
//using Windows.Win32.Graphics.Gdi;
//using Windows.Win32.UI.Controls;
//using Windows.Win32.UI.WindowsAndMessaging;
//using Windows.Win32.UI.HiDpi;
//using Windows.Win32.UI.Input.KeyboardAndMouse;

//namespace Macrosharp.UserInterfaces.DynamicWindow;


//public class SimpleWindow2
//{
//    private const string ClassName = "SimpleWindowClass";
//    private string _title;
//    private int _computedWidth = 500;
//    private int _computedHeight = 300;
//    private int _labelWidth = 150;
//    private int _inputFieldWidth = 250;
//    private int _itemsHeight = 20;
//    private int _xSep = 10;
//    private int _ySep = 10;
//    private int _xOffset = 10;
//    private int _yOffset = 10;
//    private HWND _hwnd;
//    private HWND _hButtonOK;
//    private List<HWND> _hEditFields = new List<HWND>();
//    public List<string> UserInputs { get; private set; } = new List<string>();

//    // Key capture fields
//    private HWND _hKeyDisplay;
//    private HWND _hCaptureButton;
//    private bool _isCapturing = false;
//    private int? _capturedKeyVK;
//    private int? _capturedKeyScanCode;
//    private string? _capturedKeyName;
//    private bool _showKeyCaptureField = false;
//    private const int CaptureButtonId = 2; // Command id for capture button

//    private WNDPROC _wndProcDelegate;

//    private static int LOWORD(LPARAM value) => (short)(value.Value & 0xFFFF);
//    private static int LOWORD(WPARAM value) => (short)(value.Value & 0xFFFF);
//    private static int HIWORD(LPARAM value) => (short)((value.Value >> 16) & 0xFFFF);
//    private static int HIWORD(WPARAM value) => (short)((value.Value >> 16) & 0xFFFF);

//    public SimpleWindow2(string title)
//    {
//        _title = title;
//        _wndProcDelegate = WindowProcedure; // Assign the delegate to the method
//    }

//    private LRESULT WindowProcedure(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
//    {
//        if (message == PInvoke.WM_DESTROY)
//        {
//            PInvoke.PostQuitMessage(0);
//            PInvoke.UnregisterClass(ClassName, null); // hInstance is null for current process
//            return (LRESULT)0;
//        }
//        else if (message == PInvoke.WM_COMMAND)
//        {
//            int controlId = LOWORD(wParam);

//            if (controlId == 1) // OK button
//            {
//                foreach (var hEdit in _hEditFields)
//                {
//                    char[] buffer = new char[256]; // Adjust buffer size as needed
//                    PInvoke.GetWindowText(hEdit, buffer, 256);
//                    UserInputs.Add(new string(buffer).TrimEnd('\0'));
//                }
//                DestroyWindow();
//                return (LRESULT)0;
//            }
//            else if (controlId == CaptureButtonId) // Capture button
//            {
//                _isCapturing = true;
//                PInvoke.SetWindowText(_hCaptureButton, "Press any key...");
//                PInvoke.SetFocus(_hwnd); // Set focus to main window
//                return (LRESULT)0;
//            }
//        }
//        else if (message == PInvoke.WM_KEYDOWN)
//        {
//            if (_isCapturing)
//            {
//                uint scanCode = (uint)(PInvoke.MapVirtualKey((uint)wParam.Value, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC) << 16);
//                if ((lParam.Value & (1 << 24)) != 0)
//                {
//                    scanCode |= (1 << 24);
//                }

//                if (wParam.Value == (int)VirtualKey.TAB)
//                {
//                    PInvoke.SetFocus(PInvoke.GetNextDlgTabItem(hwnd, HWND.Null, false));
//                    return (LRESULT)0;
//                }

//                unsafe
//                {
//                    fixed (char* buffer = new char[32])
//                    {
//                        int result = PInvoke.GetKeyNameText((int)scanCode, buffer, 32);
//                        if (result > 0)
//                        {
//                            _capturedKeyVK = (int)wParam.Value;
//                            _capturedKeyScanCode = (int)(scanCode >> 16);
//                            _capturedKeyName = new string(buffer, 0, result);
//                            PInvoke.SetWindowText(_hKeyDisplay, _capturedKeyName);
//                            PInvoke.SetWindowText(_hCaptureButton, "Press to Capture Key");
//                            _isCapturing = false;
//                        }
//                    }
//                }
//            }
//            else if (wParam.Value == (int)VirtualKey.RETURN)
//            {
//                PInvoke.SendMessage(_hButtonOK, PInvoke.BM_CLICK, 0, 0);
//                return (LRESULT)0;
//            }
//            else if (wParam.Value == (int)VirtualKey.ESCAPE)
//            {
//                DestroyWindow();
//                return (LRESULT)0;
//            }
//            return (LRESULT)0;
//        }
//        else if (message == PInvoke.WM_SYSKEYDOWN)
//        {
//            uint scanCode = (uint)(PInvoke.MapVirtualKey((uint)wParam.Value, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC) << 16);
//            if ((lParam.Value & (1 << 24)) != 0)
//            {
//                scanCode |= (1 << 24);
//            }

//            if (wParam.Value == (int)VirtualKey.MENU)
//            {
//                _capturedKeyVK = (int)wParam.Value;
//                _capturedKeyScanCode = (int)(scanCode >> 16);
//                _capturedKeyName = "Alt";
//                PInvoke.SetWindowText(_hKeyDisplay, _capturedKeyName);
//                PInvoke.SetWindowText(_hCaptureButton, "Press to Capture Key");
//                _isCapturing = false;
//                return (LRESULT)1; // Prevent system processing
//            }
//            return (LRESULT)0;
//        }
//        else if (message == PInvoke.WM_GETMINMAXINFO)
//        {
//            var minMaxInfo = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(lParam);
//            minMaxInfo.ptMinTrackSize.X = _computedWidth;
//            minMaxInfo.ptMinTrackSize.Y = _computedHeight;
//            minMaxInfo.ptMaxTrackSize.Y = _computedHeight; //Comment this line to allow vertical resizing
//            System.Runtime.InteropServices.Marshal.StructureToPtr(minMaxInfo, lParam, true);
//            return 0;
//        }
//        else if (message == PInvoke.WM_SIZE)
//        {
//            int width = LOWORD(lParam);
//            if (_showKeyCaptureField)
//            {
//                PInvoke.SetWindowPos(_hCaptureButton, HWND.Null, 0, 0, width - (_labelWidth + _xSep + 2 * _xOffset), _itemsHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
//            }
//            foreach (var hEdit in _hEditFields)
//            {
//                PInvoke.SetWindowPos(hEdit, HWND.Null, 0, 0, width - (_labelWidth + _xSep + 2 * _xOffset), _itemsHeight, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
//            }
//            PInvoke.SetWindowPos(_hButtonOK, HWND.Null, 0, 0, width - 2 * _xOffset, _itemsHeight + 5, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
//            return 0;
//        }

//        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
//    }


//    public void DestroyWindow()
//    {
//        PInvoke.DestroyWindow(_hwnd);
//        PInvoke.UnregisterClass(ClassName, null);
//    }

//    public List<string> CreateDynamicInputWindow(List<string> inputLabels, List<string>? placeholders = null, bool enableKeyCapture = false)
//    {
//        _showKeyCaptureField = enableKeyCapture;

//        if (placeholders != null && placeholders.Count != inputLabels.Count)
//        {
//            Console.WriteLine("Warning! The number of placeholders should be equal to the number of input labels. Continuing without placeholders...");
//            placeholders = null;
//        }

//        WNDCLASSW wndClass = new WNDCLASSW();
//        wndClass.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;
//        wndClass.lpfnWndProc = _wndProcDelegate;
//        wndClass.cbClsExtra = 0;
//        wndClass.cbWndExtra = 0;
//        wndClass.hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
//        wndClass.hIcon = PInvoke.LoadIcon((HINSTANCE)null, PInvoke.IDI_APPLICATION);
//        wndClass.hCursor = PInvoke.LoadCursor((HINSTANCE)null, PInvoke.IDC_ARROW);
//        wndClass.hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.DKGRAY_BRUSH)); // Darker background color
//        wndClass.lpszMenuName = null;
//        wndClass.lpszClassName = ClassName;

//        ushort classAtom = PInvoke.RegisterClassW(wndClass);
//        if (classAtom == 0)
//        {
//            System.ComponentModel.Win32Exception lastError = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastPInvokeError());
//            if (lastError.NativeErrorCode != 1410) // 1410 - Class already exists
//            {
//                Console.WriteLine($"Failed to register window class. Error: {lastError.Message}, Code: {lastError.NativeErrorCode}");
//                return UserInputs; // Or throw exception
//            }
//            else
//            {
//                Console.WriteLine("Window class already registered. Continuing...");
//            }
//        }


//        _computedWidth = _labelWidth + _inputFieldWidth + _xSep + 2 * _xOffset;
//        int capturedKeyHeight = enableKeyCapture ? (_itemsHeight + _ySep) : 0;
//        _computedHeight = 60 + capturedKeyHeight + (_itemsHeight + _ySep) * inputLabels.Count + _itemsHeight + 2 * _yOffset;


//        _hwnd = PInvoke.CreateWindowExW(
//            WindowExStyles.WS_EX_TOPMOST,
//            ClassName,
//            _title,
//            WindowStyles.WS_OVERLAPPEDWINDOW,
//            PInvoke.CW_USEDEFAULT,
//            PInvoke.CW_USEDEFAULT,
//            _computedWidth,
//            _computedHeight,
//            HWND.Null,
//            HMENU.Null,
//            wndClass.hInstance,
//            null);

//        if (placeholders == null)
//        {
//            placeholders = Enumerable.Repeat(string.Empty, inputLabels.Count).ToList();
//        }

//        int yPos = _yOffset;

//        if (enableKeyCapture)
//        {
//            _hKeyDisplay = PInvoke.CreateWindowExW(
//                0,
//                "STATIC",
//                "",
//                WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | WindowStyles.SS_CENTER | WindowStyles.SS_CENTERIMAGE,
//                _xOffset,
//                yPos,
//                _labelWidth,
//                _itemsHeight,
//                _hwnd,
//                HMENU.Null,
//                wndClass.hInstance,
//                null);

//            _hCaptureButton = PInvoke.CreateWindowExW(
//                0,
//                "Button",
//                "Press to Capture Key",
//                WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | WindowStyles.BS_PUSHBUTTON | WindowStyles.WS_TABSTOP,
//                _xOffset + _labelWidth + _xSep,
//                yPos,
//                _inputFieldWidth,
//                _itemsHeight,
//                _hwnd,
//                (HMENU)CaptureButtonId, // Command ID as HMENU (IntPtr cast)
//                wndClass.hInstance,
//                null);

//            SetWindowSubclass(_hCaptureButton, SubclassProc, CaptureButtonId, _hwnd);
//            yPos += _itemsHeight + _ySep;
//        }


//        _hEditFields.Clear();
//        for (int i = 0; i < inputLabels.Count; i++)
//        {
//            PInvoke.CreateWindowExW(
//                0,
//                "STATIC",
//                inputLabels[i],
//                WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | StaticControlStyles.SS_CENTER | StaticControlStyles.SS_CENTERIMAGE,
//                _xOffset,
//                yPos,
//                _labelWidth,
//                _itemsHeight,
//                _hwnd,
//                HMENU.Null,
//                wndClass.hInstance,
//                null);

//            HWND hEdit = PInvoke.CreateWindowExW(
//                0,
//                "Edit",
//                placeholders[i],
//                WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | WindowStyles.WS_BORDER | EditControlStyles.ES_AUTOHSCROLL | EditControlStyles.ES_LEFT | WindowStyles.WS_TABSTOP,
//                _xOffset + _labelWidth + _xSep,
//                yPos,
//                _inputFieldWidth,
//                _itemsHeight,
//                _hwnd,
//                HMENU.Null,
//                wndClass.hInstance,
//                null);
//            _hEditFields.Add(hEdit);
//            SetWindowSubclass(hEdit, SubclassProc, 1, _hwnd); // Using command ID 1 for edit controls as well - revisit if needed

//            yPos += _itemsHeight + _ySep;
//        }


//        _hButtonOK = PInvoke.CreateWindowExW(
//            0,
//            "Button",
//            "OK",
//            WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | ButtonStyles.BS_PUSHBUTTON | WindowStyles.WS_TABSTOP,
//            _xOffset,
//            yPos,
//            _labelWidth + _inputFieldWidth + _xSep,
//            _itemsHeight,
//            _hwnd,
//            (HMENU)1, // Command ID 1 for OK button
//            wndClass.hInstance,
//            null);
//        SetWindowSubclass(_hButtonOK, SubclassProc, 1, _hwnd);


//        PInvoke.ShowWindow(_hwnd, ShowWindowCommand.SW_SHOWNORMAL);
//        PInvoke.UpdateWindow(_hwnd);

//        MSG msg;
//        while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
//        {
//            PInvoke.TranslateMessage(msg);
//            PInvoke.DispatchMessage(msg);
//        }

//        return UserInputs;
//    }


//    private delegate LRESULT SubclassDelegate(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nint dwRefData);

//    [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
//    private static LRESULT SubclassProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nint dwRefData)
//    {
//        if (uMsg == PInvoke.WM_KEYDOWN)
//        {
//            if (wParam.Value == (int)VirtualKey.VK_TAB)
//            {
//                bool shiftPressed = (PInvoke.GetAsyncKeyState(VirtualKey.VK_SHIFT) & 0x8000) != 0;
//                PInvoke.SetFocus(PInvoke.GetNextDlgTabItem((HWND)dwRefData, hwnd, shiftPressed));
//                return 0;
//            }
//            else if (wParam.Value == (int)VirtualKey.VK_RETURN)
//            {
//                HWND buttonHwnd = PInvoke.GetDlgItem((HWND)dwRefData, 1); // OK button ID is 1
//                PInvoke.SendMessage(buttonHwnd, PInvoke.BM_CLICK, 0, 0);
//                return 0;
//            }
//        }
//        return PInvoke.DefSubclassProc(hwnd, uMsg, wParam, lParam);
//    }

//    // Import SetWindowSubclass and DefSubclassProc from ComCtl32.dll
//    [System.Runtime.InteropServices.DllImport("ComCtl32.dll")]
//    private static extern unsafe BOOL SetWindowSubclass(HWND hWnd, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.FunctionPtr)] SubclassDelegate pfnSubclass, nuint uIdSubclass, nint dwRefData);

//    [System.Runtime.InteropServices.DllImport("ComCtl32.dll")]
//    private static extern LRESULT DefSubclassProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
//}