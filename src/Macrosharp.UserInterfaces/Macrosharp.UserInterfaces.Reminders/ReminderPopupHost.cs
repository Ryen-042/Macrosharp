using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderPopupHost
{
    public void Show(ReminderDefinition reminder, ReminderPopupOptions popupOptions, Action<ReminderPopupResult> onResult)
    {
        var thread = new Thread(() =>
        {
            var popup = new ReminderPopupWindow(reminder, popupOptions, onResult);
            popup.Run();
        });

        thread.Name = $"ReminderPopup-{reminder.Id}";
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private sealed class ReminderPopupWindow
    {
        private const string ClassName = "Macrosharp.ReminderPopup";
        private const int SnoozeButtonId = 1001;
        private const int DismissButtonId = 1002;
        private const int PopupWidth = 430;
        private const int PopupHeight = 230;

        private readonly ReminderDefinition _reminder;
        private readonly ReminderPopupOptions _popupOptions;
        private readonly Action<ReminderPopupResult> _onResult;
        private readonly WNDPROC _wndProc;
        private readonly Dictionary<nint, COLORREF> _textColors = new();
        private readonly HBRUSH _backgroundBrush;
        private nint _titleFont;
        private nint _regularFont;
        private nint _boldFont;
        private nint _italicFont;
        private nint _boldItalicFont;

        private HWND _hwnd;
        private bool _resultSubmitted;
        private PopupPlacement _placement;

        public ReminderPopupWindow(ReminderDefinition reminder, ReminderPopupOptions popupOptions, Action<ReminderPopupResult> onResult)
        {
            _reminder = reminder;
            _popupOptions = popupOptions;
            _onResult = onResult;
            _wndProc = WndProc;
            _backgroundBrush = PInvoke.CreateSolidBrush(new COLORREF(0x241e1e));
        }

        public void Run()
        {
            try
            {
                RegisterClass();
                CreateWindow();
                InitializeFonts();
                CreateControls();

                PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.UpdateWindow(_hwnd);

                var duration = Math.Clamp(_popupOptions.DurationSeconds, 3, 120);
                Task.Delay(TimeSpan.FromSeconds(duration))
                    .ContinueWith(_ =>
                    {
                        if (_resultSubmitted || _hwnd == HWND.Null)
                        {
                            return;
                        }

                        PInvoke.PostMessage(_hwnd, PInvoke.WM_CLOSE, 0, 0);
                    });

                MSG msg = new();
                while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
                {
                    PInvoke.TranslateMessage(msg);
                    PInvoke.DispatchMessage(msg);
                }
            }
            finally
            {
                PopupPlacementCoordinator.Release(_placement);
                PInvoke.UnregisterClass(ClassName, default);
                PInvoke.DeleteObject(_backgroundBrush);
                if (_titleFont != 0)
                    DeleteObject(_titleFont);
                if (_regularFont != 0)
                    DeleteObject(_regularFont);
                if (_boldFont != 0)
                    DeleteObject(_boldFont);
                if (_italicFont != 0)
                    DeleteObject(_italicFont);
                if (_boldItalicFont != 0)
                    DeleteObject(_boldItalicFont);
            }
        }

        private void InitializeFonts()
        {
            _titleFont = CreateFontW(-20, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
            _regularFont = CreateFontW(-16, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
            _boldFont = CreateFontW(-16, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
            _italicFont = CreateFontW(-16, 0, 0, 0, 400, 1, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
            _boldItalicFont = CreateFontW(-16, 0, 0, 0, 700, 1, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
        }

        private unsafe void RegisterClass()
        {
            fixed (char* className = ClassName)
            {
                var wndClass = new WNDCLASSW
                {
                    style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                    lpfnWndProc = _wndProc,
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION),
                    hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                    hbrBackground = _backgroundBrush,
                    lpszClassName = new PCWSTR(className),
                };

                try
                {
                    PInvoke.RegisterClass(wndClass);
                }
                catch
                {
                    // Class may already exist in multi-popup scenarios.
                }
            }
        }

        private unsafe void CreateWindow()
        {
            _placement = PopupPlacementCoordinator.Allocate(_popupOptions.Position, _popupOptions.MonitorIndex, PopupWidth, PopupHeight);

            _hwnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_LAYERED,
                ClassName,
                _reminder.Title,
                WINDOW_STYLE.WS_POPUP,
                _placement.X,
                _placement.Y,
                PopupWidth,
                PopupHeight,
                default,
                default,
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            if (_hwnd == HWND.Null)
            {
                throw new InvalidOperationException("Failed to create reminder popup window.");
            }

            var alpha = (byte)Math.Clamp(_popupOptions.OpacityPercent * 255 / 100, 76, 255);
            PInvoke.SetLayeredWindowAttributes(_hwnd, new Windows.Win32.Foundation.COLORREF(0), alpha, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
        }

        private unsafe void CreateControls()
        {
            var title = PInvoke.CreateWindowEx(0, "STATIC", _reminder.Title, WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, 18, 14, 392, 24, _hwnd, default, PInvoke.GetModuleHandle((string?)null), null);
            _textColors[(nint)title.Value] = new COLORREF(0xFFFFFF);
            if (_titleFont != 0)
            {
                PInvoke.SendMessage(title, PInvoke.WM_SETFONT, (WPARAM)(nuint)_titleFont, (LPARAM)1);
            }

            CreateRichMessageControls(_reminder.Message);

            var snooze = PInvoke.CreateWindowEx(
                0,
                "BUTTON",
                $"Snooze {ResolveSnoozeMinutes(_popupOptions)}m",
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP,
                18,
                184,
                120,
                32,
                _hwnd,
                new SafeFileHandle((IntPtr)SnoozeButtonId, ownsHandle: false),
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            var dismiss = PInvoke.CreateWindowEx(
                0,
                "BUTTON",
                "Dismiss",
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP,
                150,
                184,
                120,
                32,
                _hwnd,
                new SafeFileHandle((IntPtr)DismissButtonId, ownsHandle: false),
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            _ = title;
            _ = snooze;
            _ = dismiss;
        }

        private unsafe void CreateRichMessageControls(string message)
        {
            const int startX = 18;
            const int maxRight = 410;
            const int maxBottom = 168;
            const int lineHeight = 20;

            var x = startX;
            var y = 46;
            var runs = RichTextTagParser.Parse(message);
            var hdc = GetDC((nint)_hwnd.Value);

            try
            {
                foreach (var run in runs)
                {
                    var parts = Regex.Split(run.Text, "(\\s+)");
                    foreach (var part in parts)
                    {
                        if (string.IsNullOrEmpty(part))
                        {
                            continue;
                        }

                        if (part.Contains('\n'))
                        {
                            var newLineCount = part.Count(c => c == '\n');
                            x = startX;
                            y += lineHeight * Math.Max(1, newLineCount);
                            if (y > maxBottom)
                            {
                                return;
                            }

                            continue;
                        }

                        var display = part;
                        var font = ResolveRunFont(run);
                        var measuredWidth = MeasureTextWidth(hdc, display, font);
                        var estimatedWidth = Math.Max(4, measuredWidth);
                        if (!string.IsNullOrWhiteSpace(part) && x + estimatedWidth > maxRight)
                        {
                            x = startX;
                            y += lineHeight;
                        }

                        if (y > maxBottom)
                        {
                            return;
                        }

                        var label = PInvoke.CreateWindowEx(0, "STATIC", display, WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, x, y, estimatedWidth + 8, lineHeight, _hwnd, default, PInvoke.GetModuleHandle((string?)null), null);

                        _textColors[(nint)label.Value] = run.Color;
                        if (font != 0)
                        {
                            PInvoke.SendMessage(label, PInvoke.WM_SETFONT, (WPARAM)(nuint)font, (LPARAM)1);
                        }

                        x += estimatedWidth + 1;
                    }
                }
            }
            finally
            {
                if (hdc != 0)
                {
                    ReleaseDC((nint)_hwnd.Value, hdc);
                }
            }
        }

        private static int MeasureTextWidth(nint hdc, string text, nint font)
        {
            if (hdc == 0 || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var old = SelectObject(hdc, font);
            try
            {
                if (GetTextExtentPoint32W(hdc, text, text.Length, out var size))
                {
                    return size.cx;
                }

                return 0;
            }
            finally
            {
                if (old != 0)
                {
                    SelectObject(hdc, old);
                }
            }
        }

        private nint ResolveRunFont(RichRun run)
        {
            if (run.Bold && run.Italic)
            {
                return _boldItalicFont;
            }

            if (run.Bold)
            {
                return _boldFont;
            }

            if (run.Italic)
            {
                return _italicFont;
            }

            return _regularFont;
        }

        private unsafe LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case PInvoke.WM_COMMAND:
                {
                    var controlId = (ushort)(wParam.Value & 0xFFFF);
                    if (controlId == SnoozeButtonId)
                    {
                        Submit(ReminderPopupAction.Snooze, ResolveSnoozeMinutes(_popupOptions));
                        return (LRESULT)0;
                    }

                    if (controlId == DismissButtonId)
                    {
                        Submit(ReminderPopupAction.Dismiss, 0);
                        return (LRESULT)0;
                    }

                    break;
                }

                case PInvoke.WM_CTLCOLORSTATIC:
                {
                    var hdc = new HDC((nint)wParam.Value);
                    var child = new HWND((nint)lParam.Value);
                    if (_textColors.TryGetValue((nint)child.Value, out var color))
                    {
                        PInvoke.SetTextColor(hdc, color);
                    }
                    else
                    {
                        PInvoke.SetTextColor(hdc, new COLORREF(0xDCDCDC));
                    }

                    PInvoke.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);
                    return (LRESULT)(nint)_backgroundBrush.Value;
                }

                case PInvoke.WM_CLOSE:
                    Submit(ReminderPopupAction.Timeout, 0);
                    return (LRESULT)0;

                case PInvoke.WM_DESTROY:
                    PInvoke.PostQuitMessage(0);
                    return (LRESULT)0;
            }

            return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint CreateFontW(
            int cHeight,
            int cWidth,
            int cEscapement,
            int cOrientation,
            int cWeight,
            uint bItalic,
            uint bUnderline,
            uint bStrikeOut,
            uint iCharSet,
            uint iOutPrecision,
            uint iClipPrecision,
            uint iQuality,
            uint iPitchAndFamily,
            string pszFaceName
        );

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(nint hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetDC(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(nint hWnd, nint hDc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern nint SelectObject(nint hdc, nint hGdiObj);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetTextExtentPoint32W(nint hdc, string lpString, int c, out GdiSize size);

        [StructLayout(LayoutKind.Sequential)]
        private struct GdiSize
        {
            public int cx;
            public int cy;
        }

        private void Submit(ReminderPopupAction action, int snoozeMinutes)
        {
            if (_resultSubmitted)
            {
                return;
            }

            _resultSubmitted = true;
            _onResult(new ReminderPopupResult { Action = action, SnoozeMinutes = snoozeMinutes });

            if (_hwnd != HWND.Null)
            {
                var handle = _hwnd;
                _hwnd = HWND.Null;
                PInvoke.DestroyWindow(handle);
            }
        }

        private static int ResolveSnoozeMinutes(ReminderPopupOptions popupOptions)
        {
            if (popupOptions.SnoozeMinutes.Count == 0)
            {
                return 5;
            }

            return Math.Clamp(popupOptions.SnoozeMinutes[0], 1, 180);
        }
    }

    private enum HorizontalAnchor
    {
        Left,
        Center,
        Right,
    }

    private enum VerticalAnchor
    {
        Top,
        Middle,
        Bottom,
    }

    private readonly record struct MonitorWorkArea(int Left, int Top, int Right, int Bottom, bool IsPrimary)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    private readonly record struct SlotKey(int Monitor, ReminderPopupPosition Position, int Column, int Row);

    private readonly record struct PopupPlacement(int X, int Y, SlotKey Slot)
    {
        public static PopupPlacement Empty => new(0, 0, default);
    }

    private static class PopupPlacementCoordinator
    {
        private const int Margin = 18;
        private const int Spacing = 10;
        private const uint MONITORINFOF_PRIMARY = 0x00000001;

        private static readonly object Gate = new();
        private static readonly HashSet<SlotKey> Occupied = new();

        private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetMonitorInfoW(nint hMonitor, ref MONITORINFOEX lpmi);

        public static PopupPlacement Allocate(ReminderPopupPosition position, int? monitorIndex, int width, int height)
        {
            lock (Gate)
            {
                var monitors = EnumerateMonitors();
                var selectedMonitorIndex = ResolveMonitorIndex(monitors, monitorIndex);
                var monitor = monitors[selectedMonitorIndex];
                var (horizontalAnchor, verticalAnchor) = ResolveAnchors(position);

                var stepX = width + Spacing;
                var stepY = height + Spacing;

                var maxCols = Math.Max(1, (monitor.Width - (2 * Margin) + Spacing) / stepX);
                var maxRows = Math.Max(1, (monitor.Height - (2 * Margin) + Spacing) / stepY);

                var columns = BuildColumns(maxCols, horizontalAnchor);
                var rows = BuildRows(maxRows, verticalAnchor);

                foreach (var column in columns)
                {
                    foreach (var row in rows)
                    {
                        var x = ResolveX(monitor, horizontalAnchor, width, stepX, column);
                        var y = ResolveY(monitor, verticalAnchor, height, stepY, row);

                        if (!IsWithinWorkArea(monitor, x, y, width, height))
                        {
                            continue;
                        }

                        var slot = new SlotKey(selectedMonitorIndex, position, column, row);
                        if (Occupied.Contains(slot))
                        {
                            continue;
                        }

                        Occupied.Add(slot);
                        return new PopupPlacement(x, y, slot);
                    }
                }

                var fallbackX = monitor.Right - Margin - width;
                var fallbackY = monitor.Bottom - Margin - height;
                var fallback = new SlotKey(selectedMonitorIndex, position, int.MinValue, int.MinValue);
                Occupied.Add(fallback);
                return new PopupPlacement(fallbackX, fallbackY, fallback);
            }
        }

        public static void Release(PopupPlacement placement)
        {
            if (placement.Slot == default)
            {
                return;
            }

            lock (Gate)
            {
                Occupied.Remove(placement.Slot);
            }
        }

        private static int ResolveMonitorIndex(IReadOnlyList<MonitorWorkArea> monitors, int? requested)
        {
            if (requested.HasValue)
            {
                return Math.Clamp(requested.Value, 0, monitors.Count - 1);
            }

            var primaryIndex = monitors
                .Select((m, i) => new { Monitor = m, Index = i })
                .FirstOrDefault(x => x.Monitor.IsPrimary)?.Index;

            return primaryIndex ?? 0;
        }

        private static List<MonitorWorkArea> EnumerateMonitors()
        {
            var result = new List<MonitorWorkArea>();

            EnumDisplayMonitors(
                0,
                0,
                (hMonitor, _, _, _) =>
                {
                    var info = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>(), szDevice = string.Empty };
                    if (GetMonitorInfoW(hMonitor, ref info))
                    {
                        result.Add(
                            new MonitorWorkArea(
                                info.rcWork.left,
                                info.rcWork.top,
                                info.rcWork.right,
                                info.rcWork.bottom,
                                (info.dwFlags & MONITORINFOF_PRIMARY) != 0
                            )
                        );
                    }

                    return true;
                },
                0
            );

            if (result.Count == 0)
            {
                var width = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
                var height = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
                result.Add(new MonitorWorkArea(0, 0, width, height, true));
            }

            return result;
        }

        private static IEnumerable<int> BuildColumns(int maxColumns, HorizontalAnchor anchor)
        {
            return anchor == HorizontalAnchor.Center ? BuildCenteredSequence(maxColumns) : Enumerable.Range(0, maxColumns);
        }

        private static IEnumerable<int> BuildRows(int maxRows, VerticalAnchor anchor)
        {
            return anchor == VerticalAnchor.Middle ? BuildCenteredSequence(maxRows) : Enumerable.Range(0, maxRows);
        }

        private static IEnumerable<int> BuildCenteredSequence(int count)
        {
            yield return 0;
            for (var i = 1; i < count; i++)
            {
                var offset = (i + 1) / 2;
                yield return i % 2 == 1 ? offset : -offset;
            }
        }

        private static int ResolveX(MonitorWorkArea area, HorizontalAnchor anchor, int width, int stepX, int column)
        {
            return anchor switch
            {
                HorizontalAnchor.Left => area.Left + Margin + (column * stepX),
                HorizontalAnchor.Right => area.Right - Margin - width - (column * stepX),
                _ => area.Left + ((area.Width - width) / 2) + (column * stepX),
            };
        }

        private static int ResolveY(MonitorWorkArea area, VerticalAnchor anchor, int height, int stepY, int row)
        {
            return anchor switch
            {
                VerticalAnchor.Top => area.Top + Margin + (row * stepY),
                VerticalAnchor.Bottom => area.Bottom - Margin - height - (row * stepY),
                _ => area.Top + ((area.Height - height) / 2) + (row * stepY),
            };
        }

        private static bool IsWithinWorkArea(MonitorWorkArea area, int x, int y, int width, int height)
        {
            return x >= area.Left + Margin
                && y >= area.Top + Margin
                && (x + width) <= area.Right - Margin
                && (y + height) <= area.Bottom - Margin;
        }

        private static (HorizontalAnchor horizontal, VerticalAnchor vertical) ResolveAnchors(ReminderPopupPosition position)
        {
            return position switch
            {
                ReminderPopupPosition.TopLeft => (HorizontalAnchor.Left, VerticalAnchor.Top),
                ReminderPopupPosition.TopCenter => (HorizontalAnchor.Center, VerticalAnchor.Top),
                ReminderPopupPosition.TopRight => (HorizontalAnchor.Right, VerticalAnchor.Top),
                ReminderPopupPosition.MiddleLeft => (HorizontalAnchor.Left, VerticalAnchor.Middle),
                ReminderPopupPosition.Center => (HorizontalAnchor.Center, VerticalAnchor.Middle),
                ReminderPopupPosition.MiddleRight => (HorizontalAnchor.Right, VerticalAnchor.Middle),
                ReminderPopupPosition.BottomLeft => (HorizontalAnchor.Left, VerticalAnchor.Bottom),
                ReminderPopupPosition.BottomCenter => (HorizontalAnchor.Center, VerticalAnchor.Bottom),
                _ => (HorizontalAnchor.Right, VerticalAnchor.Bottom),
            };
        }
    }

    private readonly record struct RichRun(string Text, bool Bold, bool Italic, COLORREF Color);

    private static class RichTextTagParser
    {
        private static readonly Regex ColorRegex = new("^color=#([0-9a-fA-F]{6})$", RegexOptions.Compiled);

        public static List<RichRun> Parse(string? text)
        {
            var source = text ?? string.Empty;
            var runs = new List<RichRun>();

            var bold = false;
            var italic = false;
            var color = new COLORREF(0xDCDCDC);
            var i = 0;

            while (i < source.Length)
            {
                if (source[i] == '[')
                {
                    var close = source.IndexOf(']', i);
                    if (close > i)
                    {
                        var tag = source[(i + 1)..close].Trim();
                        if (tag.Equals("b", StringComparison.OrdinalIgnoreCase))
                        {
                            bold = true;
                            i = close + 1;
                            continue;
                        }

                        if (tag.Equals("/b", StringComparison.OrdinalIgnoreCase))
                        {
                            bold = false;
                            i = close + 1;
                            continue;
                        }

                        if (tag.Equals("i", StringComparison.OrdinalIgnoreCase))
                        {
                            italic = true;
                            i = close + 1;
                            continue;
                        }

                        if (tag.Equals("/i", StringComparison.OrdinalIgnoreCase))
                        {
                            italic = false;
                            i = close + 1;
                            continue;
                        }

                        if (tag.Equals("/color", StringComparison.OrdinalIgnoreCase))
                        {
                            color = new COLORREF(0xDCDCDC);
                            i = close + 1;
                            continue;
                        }

                        var match = ColorRegex.Match(tag);
                        if (match.Success)
                        {
                            var hex = match.Groups[1].Value;
                            var r = Convert.ToByte(hex[..2], 16);
                            var g = Convert.ToByte(hex.Substring(2, 2), 16);
                            var b = Convert.ToByte(hex.Substring(4, 2), 16);
                            color = new COLORREF((uint)(r | (g << 8) | (b << 16)));
                            i = close + 1;
                            continue;
                        }
                    }
                }

                var nextTag = source.IndexOf('[', i);
                var chunk = nextTag >= 0 ? source[i..nextTag] : source[i..];
                if (chunk.Length > 0)
                {
                    runs.Add(new RichRun(chunk.Replace("\r", string.Empty), bold, italic, color));
                }

                if (nextTag < 0)
                {
                    break;
                }

                i = nextTag;
            }

            if (runs.Count == 0)
            {
                runs.Add(new RichRun(string.Empty, false, false, new COLORREF(0xDCDCDC)));
            }

            return runs;
        }
    }
}
