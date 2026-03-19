using System.Runtime.InteropServices;
using Macrosharp.Devices.Core;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.UserInterfaces.DynamicWindow;

public static class FilterableTableWindow
{
    private static readonly object Sync = new();
    private static FilterableTableHost? _host;

    public static void ShowOrActivate(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, string filterPlaceholder = "Filter...")
    {
        if (columns is null)
            throw new ArgumentNullException(nameof(columns));
        if (rows is null)
            throw new ArgumentNullException(nameof(rows));

        lock (Sync)
        {
            if (_host is { IsRunning: true })
            {
                _host.UpdateData(title, columns, rows, filterPlaceholder);
                return;
            }

            _host = new FilterableTableHost(() =>
            {
                lock (Sync)
                {
                    _host = null;
                }
            });

            _host.Start(title, columns, rows, filterPlaceholder);
        }
    }

    private sealed class FilterableTableHost
    {
        private const string WindowClassNamePrefix = "Macrosharp.FilterableTableWindow";
        private const string ListViewClassName = "SysListView32";

        private const uint WmAppRefresh = PInvoke.WM_APP + 10;
        private const uint WmAppActivate = PInvoke.WM_APP + 11;

        private const int IdFilterEdit = 2001;
        private const int IdExportButton = 2002;
        private const int IdHintLabel = 2003;

        private const int EnSetFocus = 0x0100;
        private const int EnKillFocus = 0x0200;
        private const int EnChange = 0x0300;
        private const int BnClicked = 0;

        private const uint WmSetRedraw = 0x000B;

        private const uint LvmFirst = 0x1000;
        private const uint LvmDeleteAllItems = LvmFirst + 9;
        private const uint LvmDeleteColumn = LvmFirst + 28;
        private const uint LvmInsertItemW = LvmFirst + 77;
        private const uint LvmInsertColumnW = LvmFirst + 97;
        private const uint LvmSetItemTextW = LvmFirst + 116;
        private const uint LvmSetExtendedListViewStyle = LvmFirst + 54;
        private const uint LvmGetExtendedListViewStyle = LvmFirst + 55;
        private const int LvnFirst = -100;
        private const int LvnColumnClick = LvnFirst - 8;

        private const uint LvsReport = 0x0001;
        private const uint LvsSingleSel = 0x0004;
        private const uint LvsShowSelAlways = 0x0008;

        private const nint LvsExFullRowSelect = 0x00000020;
        private const nint LvsExGridLines = 0x00000001;
        private const nint LvsExDoubleBuffer = 0x00010000;

        private const uint LvifText = 0x0001;

        private const uint LvcfFmt = 0x0001;
        private const uint LvcfWidth = 0x0002;
        private const uint LvcfText = 0x0004;
        private const int LvcfmtLeft = 0x0000;

        private const uint CbSizeBytes = 260;

        private readonly Action _onClosed;
        private readonly ManualResetEventSlim _started = new(false);

        private readonly object _dataLock = new();
        private string _title = "Macrosharp";
        private string _placeholder = "Filter...";
        private List<string> _columns = new();
        private List<List<string>> _allRows = new();
        private List<List<string>> _visibleRows = new();
        private string _filterText = string.Empty;
        private int _sortColumn;
        private bool _sortAscending = true;
        private readonly int _groupColumn = 2;

        private HWND _hwnd;
        private HWND _editFilter;
        private HWND _buttonExport;
        private HWND _labelHint;
        private HWND _listView;

        private bool _isFilterPlaceholder = true;

        private WNDPROC? _wndProc;
        private readonly string _windowClassName = $"{WindowClassNamePrefix}.{Guid.NewGuid():N}";

        public bool IsRunning { get; private set; }

        public FilterableTableHost(Action onClosed)
        {
            _onClosed = onClosed;
        }

        public void Start(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, string filterPlaceholder)
        {
            UpdateDataCore(title, columns, rows, filterPlaceholder);

            var thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Macrosharp.FilterableTableWindow",
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            _started.Wait(TimeSpan.FromSeconds(3));
        }

        public void UpdateData(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, string filterPlaceholder)
        {
            UpdateDataCore(title, columns, rows, filterPlaceholder);

            if (_hwnd != HWND.Null)
            {
                PInvoke.PostMessage(_hwnd, WmAppRefresh, 0, 0);
                PInvoke.PostMessage(_hwnd, WmAppActivate, 0, 0);
            }
        }

        private void UpdateDataCore(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows, string filterPlaceholder)
        {
            lock (_dataLock)
            {
                _title = string.IsNullOrWhiteSpace(title) ? "Macrosharp" : title;
                _placeholder = string.IsNullOrWhiteSpace(filterPlaceholder) ? "Filter..." : filterPlaceholder;
                _columns = columns.Select(c => c ?? string.Empty).ToList();
                _allRows = rows.Select(r => r.Select(cell => cell ?? string.Empty).ToList()).ToList();
                _visibleRows = new List<List<string>>(_allRows);
            }
        }

        private unsafe void Run()
        {
            IsRunning = true;
            _wndProc = WindowProc;

            fixed (char* className = _windowClassName)
            {
                var wc = new WNDCLASSW
                {
                    lpfnWndProc = _wndProc,
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    lpszClassName = className,
                    hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                    hbrBackground = new HBRUSH((nint)6),
                };

                PInvoke.RegisterClass(wc);
            }

            _hwnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_APPWINDOW,
                _windowClassName,
                _title,
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE,
                PInvoke.CW_USEDEFAULT,
                PInvoke.CW_USEDEFAULT,
                1080,
                680,
                HWND.Null,
                default,
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            if (_hwnd == HWND.Null)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Failed to create hotkeys window. Win32Error={error}");
                _started.Set();
                IsRunning = false;
                PInvoke.UnregisterClass(_windowClassName, default);
                _onClosed();
                return;
            }

            _started.Set();

            MSG msg;
            while (PInvoke.GetMessage(out msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }

            PInvoke.UnregisterClass(_windowClassName, default);
            IsRunning = false;
            _onClosed();
        }

        private unsafe LRESULT WindowProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_CREATE:
                    CreateControls(hwnd);
                    RefreshView();
                    return (LRESULT)0;

                case PInvoke.WM_SIZE:
                    LayoutControls(hwnd);
                    return (LRESULT)0;

                case PInvoke.WM_COMMAND:
                    HandleCommand(hwnd, wParam);
                    return (LRESULT)0;

                case PInvoke.WM_NOTIFY:
                    return HandleNotify(lParam);

                case WmAppRefresh:
                    RefreshView();
                    return (LRESULT)0;

                case WmAppActivate:
                    PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
                    PInvoke.SetForegroundWindow(hwnd);
                    return (LRESULT)0;

                case PInvoke.WM_DESTROY:
                    PInvoke.PostQuitMessage(0);
                    return (LRESULT)0;
            }

            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private unsafe void CreateControls(HWND hwnd)
        {
            _editFilter = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_CLIENTEDGE,
                "EDIT",
                _placeholder,
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP | WINDOW_STYLE.WS_BORDER | (WINDOW_STYLE)0x0080,
                12,
                12,
                560,
                28,
                hwnd,
                new SafeFileHandle((IntPtr)IdFilterEdit, ownsHandle: false),
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            _buttonExport = PInvoke.CreateWindowEx(
                0,
                "BUTTON",
                "Export Visible Rows",
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP | (WINDOW_STYLE)0,
                586,
                12,
                180,
                28,
                hwnd,
                new SafeFileHandle((IntPtr)IdExportButton, ownsHandle: false),
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            _labelHint = PInvoke.CreateWindowEx(
                0,
                "STATIC",
                "0 item(s)",
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE,
                12,
                48,
                740,
                20,
                hwnd,
                new SafeFileHandle((IntPtr)IdHintLabel, ownsHandle: false),
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            _listView = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_CLIENTEDGE,
                ListViewClassName,
                string.Empty,
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_TABSTOP | WINDOW_STYLE.WS_BORDER | WINDOW_STYLE.WS_VSCROLL | WINDOW_STYLE.WS_HSCROLL | (WINDOW_STYLE)LvsReport | (WINDOW_STYLE)LvsShowSelAlways | (WINDOW_STYLE)LvsSingleSel,
                12,
                74,
                1040,
                560,
                hwnd,
                default,
                PInvoke.GetModuleHandle((string?)null),
                null
            );

            nint ex = PInvoke.SendMessage(_listView, LvmGetExtendedListViewStyle, 0, 0).Value;
            ex |= LvsExFullRowSelect | LvsExGridLines | LvsExDoubleBuffer;
            PInvoke.SendMessage(_listView, LvmSetExtendedListViewStyle, 0, ex);

            _isFilterPlaceholder = true;
            _filterText = string.Empty;
        }

        private void LayoutControls(HWND hwnd)
        {
            PInvoke.GetClientRect(hwnd, out var rc);
            int width = rc.right - rc.left;
            int height = rc.bottom - rc.top;

            int margin = 12;
            int top = 12;
            int filterHeight = 28;
            int exportWidth = 180;
            int hintHeight = 20;

            int filterWidth = Math.Max(250, width - (margin * 3) - exportWidth);
            SetBounds(_editFilter, margin, top, filterWidth, filterHeight);
            SetBounds(_buttonExport, margin * 2 + filterWidth, top, exportWidth, filterHeight);

            int hintTop = top + filterHeight + 8;
            SetBounds(_labelHint, margin, hintTop, width - margin * 2, hintHeight);

            int listTop = hintTop + hintHeight + 6;
            int listHeight = Math.Max(120, height - listTop - margin);
            SetBounds(_listView, margin, listTop, width - margin * 2, listHeight);
        }

        private void HandleCommand(HWND hwnd, WPARAM wParam)
        {
            int id = (ushort)(wParam.Value & 0xFFFF);
            int code = (ushort)((wParam.Value >> 16) & 0xFFFF);

            if (id == IdFilterEdit)
            {
                switch (code)
                {
                    case EnSetFocus:
                        if (_isFilterPlaceholder)
                        {
                            _isFilterPlaceholder = false;
                            SetControlText(_editFilter, string.Empty);
                        }
                        break;

                    case EnKillFocus:
                        if (string.IsNullOrWhiteSpace(GetControlText(_editFilter)))
                        {
                            _isFilterPlaceholder = true;
                            _filterText = string.Empty;
                            SetControlText(_editFilter, _placeholder);
                            RefreshView();
                        }
                        break;

                    case EnChange:
                        if (_isFilterPlaceholder)
                            return;

                        _filterText = GetControlText(_editFilter);
                        RefreshView();
                        break;
                }
            }

            if (id == IdExportButton && code == BnClicked)
            {
                ExportVisibleRows(hwnd);
            }
        }

        private unsafe LRESULT HandleNotify(LPARAM lParam)
        {
            NMHDRNative* nmhdr = (NMHDRNative*)lParam.Value;
            if ((HWND)nmhdr->hwndFrom != _listView)
                return (LRESULT)0;

            if (nmhdr->code == LvnColumnClick)
            {
                NMLISTVIEWNative* click = (NMLISTVIEWNative*)lParam.Value;
                int column = click->iSubItem;
                if (_sortColumn == column)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = column;
                    _sortAscending = true;
                }

                RefreshView();
            }

            return (LRESULT)0;
        }

        private void RefreshView()
        {
            lock (_dataLock)
            {
                ApplyFilterAndSort();
                SetControlText(_hwnd, _title);
                SetControlText(_labelHint, $"{_visibleRows.Count} of {_allRows.Count} item(s)");
                RebuildColumns();
                RebuildRows();
            }
        }

        private void ApplyFilterAndSort()
        {
            IEnumerable<List<string>> query = _allRows;
            string filter = _filterText.Trim();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(row => row.Any(cell => cell.Contains(filter, StringComparison.OrdinalIgnoreCase)));
            }

            query = _sortAscending
                ? query.OrderBy(row => GetCell(row, _sortColumn), StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(row => GetCell(row, _sortColumn), StringComparer.OrdinalIgnoreCase);

            if (_groupColumn >= 0 && _groupColumn < _columns.Count)
            {
                query = query
                    .OrderBy(row => GetCell(row, _groupColumn), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => GetCell(row, _sortColumn), StringComparer.OrdinalIgnoreCase);
            }

            _visibleRows = query.ToList();
        }

        private unsafe void RebuildColumns()
        {
            PInvoke.SendMessage(_listView, WmSetRedraw, 0, 0);
            try
            {
                while (PInvoke.SendMessage(_listView, LvmDeleteColumn, 0, 0).Value != 0)
                {
                }

                for (int i = 0; i < _columns.Count; i++)
                {
                    int width = i == 0 ? 230 : i == 1 ? 500 : 260;
                    string title = _columns[i];
                    fixed (char* pText = title)
                    {
                        LVCOLUMNWNative col = new()
                        {
                            mask = LvcfFmt | LvcfWidth | LvcfText,
                            fmt = LvcfmtLeft,
                            cx = width,
                            pszText = pText,
                            cchTextMax = title.Length,
                        };

                        PInvoke.SendMessage(_listView, LvmInsertColumnW, (WPARAM)(nuint)i, (LPARAM)(nint)(&col));
                    }
                }
            }
            finally
            {
                PInvoke.SendMessage(_listView, WmSetRedraw, 1, 0);
            }
        }

        private unsafe void RebuildRows()
        {
            PInvoke.SendMessage(_listView, WmSetRedraw, 0, 0);
            try
            {
                PInvoke.SendMessage(_listView, LvmDeleteAllItems, 0, 0);
                int itemIndex = 0;
                string? currentGroup = null;

                for (int i = 0; i < _visibleRows.Count; i++)
                {
                    var row = _visibleRows[i];
                    string rowGroup = NormalizeGroup(GetCell(row, _groupColumn));

                    if (!string.Equals(currentGroup, rowGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        currentGroup = rowGroup;
                        InsertListViewRow(itemIndex++, $"[{rowGroup}]", string.Empty, string.Empty);
                    }

                    string first = GetCell(row, 0);
                    string second = _columns.Count > 1 ? GetCell(row, 1) : string.Empty;
                    string third = _columns.Count > 2 ? GetCell(row, 2) : string.Empty;
                    InsertListViewRow(itemIndex++, first, second, third);
                }
            }
            finally
            {
                PInvoke.SendMessage(_listView, WmSetRedraw, 1, 0);
                PInvoke.InvalidateRect(_listView, null, true);
            }
        }

        private static string NormalizeGroup(string value) => string.IsNullOrWhiteSpace(value) ? "No source" : value;

        private unsafe void InsertListViewRow(int itemIndex, string first, string second, string third)
        {
            fixed (char* pFirst = first)
            {
                LVITEMWNative item = new()
                {
                    mask = LvifText,
                    iItem = itemIndex,
                    iSubItem = 0,
                    pszText = pFirst,
                };

                PInvoke.SendMessage(_listView, LvmInsertItemW, 0, (LPARAM)(nint)(&item));
            }

            if (_columns.Count > 1)
            {
                fixed (char* pSecond = second)
                {
                    LVITEMWNative sub1 = new() { iSubItem = 1, pszText = pSecond };
                    PInvoke.SendMessage(_listView, LvmSetItemTextW, (WPARAM)(nuint)itemIndex, (LPARAM)(nint)(&sub1));
                }
            }

            if (_columns.Count > 2)
            {
                fixed (char* pThird = third)
                {
                    LVITEMWNative sub2 = new() { iSubItem = 2, pszText = pThird };
                    PInvoke.SendMessage(_listView, LvmSetItemTextW, (WPARAM)(nuint)itemIndex, (LPARAM)(nint)(&sub2));
                }
            }
        }

        private static string GetCell(IReadOnlyList<string> row, int index)
        {
            if (index < 0 || index >= row.Count)
                return string.Empty;

            return row[index] ?? string.Empty;
        }

        private void ExportVisibleRows(HWND hwnd)
        {
            string payload;
            int rowCount;
            lock (_dataLock)
            {
                rowCount = _visibleRows.Count;
                var lines = new List<string> { string.Join('\t', _columns) };
                lines.AddRange(_visibleRows.Select(row => string.Join('\t', row.Select(v => (v ?? string.Empty).Replace("\r", " ").Replace("\n", " ")))));
                payload = string.Join(Environment.NewLine, lines);
            }

            if (!TrySetClipboardText(hwnd, payload))
            {
                PInvoke.MessageBox(hwnd, "Failed to export to clipboard.", "Macrosharp", MESSAGEBOX_STYLE.MB_ICONERROR | MESSAGEBOX_STYLE.MB_OK);
                return;
            }

            PInvoke.MessageBox(hwnd, $"Copied {rowCount} row(s) to clipboard.", "Macrosharp", MESSAGEBOX_STYLE.MB_ICONINFORMATION | MESSAGEBOX_STYLE.MB_OK);
        }

        private static bool TrySetClipboardText(HWND owner, string text)
        {
            try
            {
                KeyboardSimulator.SetClipboardText(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static unsafe string GetControlText(HWND control)
        {
            uint length = (uint)PInvoke.GetWindowTextLength(control);
            if (length == 0)
                return string.Empty;

            Span<char> buffer = stackalloc char[(int)Math.Min(length + 1, CbSizeBytes)];
            fixed (char* p = buffer)
            {
                PInvoke.GetWindowText(control, p, buffer.Length);
            }

            return new string(buffer[..(int)Math.Min(length, (uint)buffer.Length - 1)]);
        }

        private static void SetControlText(HWND control, string text)
        {
            if (control != HWND.Null)
            {
                PInvoke.SetWindowText(control, text);
            }
        }

        private static void SetBounds(HWND control, int x, int y, int width, int height)
        {
            if (control == HWND.Null)
                return;

            PInvoke.SetWindowPos(control, HWND.Null, x, y, width, height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDRNative
        {
            public nint hwndFrom;
            public nuint idFrom;
            public int code;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMLISTVIEWNative
        {
            public NMHDRNative hdr;
            public int iItem;
            public int iSubItem;
            public uint uNewState;
            public uint uOldState;
            public uint uChanged;
            public int ptActionX;
            public int ptActionY;
            public nint lParam;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct LVCOLUMNWNative
        {
            public uint mask;
            public int fmt;
            public int cx;
            public char* pszText;
            public int cchTextMax;
            public int iSubItem;
            public int iImage;
            public int iOrder;
            public int cxMin;
            public int cxDefault;
            public int cxIdeal;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct LVITEMWNative
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public char* pszText;
            public int cchTextMax;
            public int iImage;
            public nint lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public uint* puColumns;
            public int* piColFmt;
            public int iGroup;
        }

    }
}
