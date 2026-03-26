using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Win32.Native;

public static class MessageBoxes
{
    /// <summary>
    /// Shows a message box without an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT Show(string text, string caption, MESSAGEBOX_STYLE style = MESSAGEBOX_STYLE.MB_OK, bool focusOnCreate = true, bool alwaysOnTop = true) => Show(HWND.Null, text, caption, style, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a message box with optional focus and topmost behavior.
    /// </summary>
    public static MESSAGEBOX_RESULT Show(HWND owner, string text, string caption, MESSAGEBOX_STYLE style = MESSAGEBOX_STYLE.MB_OK, bool focusOnCreate = true, bool alwaysOnTop = true)
    {
        MESSAGEBOX_STYLE effectiveStyle = style;

        if (focusOnCreate)
        {
            effectiveStyle |= MESSAGEBOX_STYLE.MB_SETFOREGROUND;
        }

        if (alwaysOnTop)
        {
            effectiveStyle |= MESSAGEBOX_STYLE.MB_TOPMOST;
        }

        return PInvoke.MessageBox(owner, text, caption, effectiveStyle);
    }

    /// <summary>
    /// Shows an informational OK message box without an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowInfo(string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(HWND.Null, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows an informational OK message box with an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowInfo(HWND owner, string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(owner, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a warning OK message box without an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowWarning(string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(HWND.Null, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a warning OK message box with an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowWarning(HWND owner, string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(owner, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a warning OK/Cancel message box without an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowWarningOkCancel(string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(HWND.Null, text, caption, MESSAGEBOX_STYLE.MB_OKCANCEL | MESSAGEBOX_STYLE.MB_ICONWARNING, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a warning OK/Cancel message box with an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowWarningOkCancel(HWND owner, string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(owner, text, caption, MESSAGEBOX_STYLE.MB_OKCANCEL | MESSAGEBOX_STYLE.MB_ICONWARNING, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows an error OK message box without an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowError(string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(HWND.Null, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows an error OK message box with an explicit owner.
    /// </summary>
    public static MESSAGEBOX_RESULT ShowError(HWND owner, string text, string caption = "Macrosharp", bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(owner, text, caption, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR, focusOnCreate, alwaysOnTop);

    /// <summary>
    /// Shows a Yes/No confirmation box without an explicit owner and returns true when Yes is selected.
    /// </summary>
    public static bool ShowConfirmYesNo(string text, string caption = "Macrosharp", MESSAGEBOX_STYLE style = MESSAGEBOX_STYLE.MB_ICONQUESTION, bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(HWND.Null, text, caption, style | MESSAGEBOX_STYLE.MB_YESNO, focusOnCreate, alwaysOnTop) == MESSAGEBOX_RESULT.IDYES;

    /// <summary>
    /// Shows a Yes/No confirmation box with an explicit owner and returns true when Yes is selected.
    /// </summary>
    public static bool ShowConfirmYesNo(HWND owner, string text, string caption = "Macrosharp", MESSAGEBOX_STYLE style = MESSAGEBOX_STYLE.MB_ICONQUESTION, bool focusOnCreate = true, bool alwaysOnTop = true) =>
        Show(owner, text, caption, style | MESSAGEBOX_STYLE.MB_YESNO, focusOnCreate, alwaysOnTop) == MESSAGEBOX_RESULT.IDYES;
}
