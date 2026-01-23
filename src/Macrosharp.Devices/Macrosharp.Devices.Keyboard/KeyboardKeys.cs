using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.Devices.Keyboard;

/// <summary>Represents the virtual key codes used in the Windows API. These key codes are used to identify keyboard keys and other input events</summary>
/// <remarks>
/// <para>Virtual key codes differ from scan codes in that they are used to identify keys, rather than the physical location of the key.
/// For example, the 'A' key will always have a virtual key code of 0x41, regardless of the keyboard layout or language.</para>
/// <para>Generally, keys send the same virtual key codes regardless of the pressed modifier keys (Shift, Ctrl, Alt, etc.), however, there are some exceptions.
/// For example, presseing Shift with NumPad1 is mapped to the End key (same when the NumLock is off and pressing NumPad1 without Shift).</para>
/// <para>Virtual codes for special keys "Shift", "Ctrl", "Alt" and "Windows" exist in two versions: "left" and "right".
/// The distinction between left- and right-hand side versions is immaterial to the user, except for the Alt key (LMENU/RMENU).
/// If the keyboard uses "AltGr", this modifier combination is assigned to the "right" Alt (RMENU), whereas the "left" Alt keeps its standard "Alt" function.</para>
/// <para>The virtual and scan codes for "Pause" (scan code 69) are "hard-wired" one to another, regardless of the keyboard layout.
/// Another peculiarity of this key is that, when pressed together with Ctrl, it generates a completely different scan code (70 ext), as if it were
/// a separate physical key. This "quasi-key" is normally mapped to "Break" (CANCEL), interpreted by some programs as an "Abort" or "Terminate" signal.</para>
/// </remarks>
public enum VirtualKey : ushort
{
    /// <summary>Mouse left button key.</summary>
    /// <remarks>Note, pressing a key with LBUTTON assigned to it will NOT make Windows act as if a physical mouse button was pressed.</remarks>
    LBUTTON = 1,

    /// <summary>Mouse right button key.</summary>
    /// <remarks>Note, pressing a key with RBUTTON assigned to it will NOT make Windows act as if a physical mouse button was pressed.</remarks>
    RBUTTON = 2,

    /// <summary>BREAK or Control-Break Processing key.</summary>
    /// <remarks>Used to terminate a DOS application or batch file.</remarks>
    CANCEL = 3,

    /// <summary>Mouse middle button key.</summary>
    /// <remarks>Note, pressing a key with MBUTTON assigned to it will NOT make Windows act as if a physical mouse button was pressed.</remarks>
    MBUTTON = 4,

    /// <summary>Mouse X1 button key.</summary>
    /// <remarks>Note, pressing a key with XBUTTON1 assigned to it will NOT make Windows act as if a physical mouse button was pressed.</remarks>
    XBUTTON1 = 5,

    /// <summary>Mouse X2 button key.</summary>
    /// <remarks>Note, pressing a key with XBUTTON2 assigned to it will NOT make Windows act as if a physical mouse button was pressed.</remarks>
    XBUTTON2 = 6,

    /// <summary>Backspace key.</summary>
    BACK = 8,

    /// <summary>Tab key.</summary>
    TAB = 9,

    /// <summary>Clear/Center key (NumPad5 with NumLock off).</summary>
    CLEAR = 12,

    /// <summary>Enter key.</summary>
    RETURN = 13,

    /// <summary>Shift key.</summary>
    SHIFT = 16,

    /// <summary>Ctrl key.</summary>
    CONTROL = 17,

    /// <summary>Alt key.</summary>
    MENU = 18,

    /// <summary>Pause key.</summary>
    /// <remarks>Used to pause the output of a DOS application or batch file.</remarks>
    PAUSE = 19,

    /// <summary>Caps Lock key.</summary>
    CAPITAL = 20,

    /// <summary>Kana key.</summary>
    KANA = 21,

    /// <summary>IME Hanguel mode key.</summary>
    /// <remarks>Maintained for compatibility; use <see cref="HANGUL"/> instead.</remarks>
    HANGUEL = 21,

    /// <summary>IME Hangul mode key.</summary>
    /// <remarks>For Korean toggles between Hangul and English input modes.</remarks>
    HANGUL = 21,

    /// <summary>IME On key.</summary>
    /// <remarks>Used to toggle the input method editor (IME) on.</remarks>
    IME_ON = 22,

    /// <summary>Junja key.</summary>
    JUNJA = 23,

    /// <summary>Final key.</summary>
    FINAL = 24,

    /// <summary>IME Hanja mode key.</summary>
    HANJA = 25,

    /// <summary>IME Kanji mode key.</summary>
    KANJI = 25,

    /// <summary>IME Off key.</summary>
    /// <remarks>Used to toggle the input method editor (IME) off.</remarks>
    IME_OFF = 26,

    /// <summary>Esc key.</summary>
    ESCAPE = 27,

    /// <summary>Convert key.</summary>
    CONVERT = 28,

    /// <summary>Nonconvert key.</summary>
    NONCONVERT = 29,

    /// <summary>Accept key.</summary>
    ACCEPT = 30,

    /// <summary>IME Mode Change Request key.</summary>
    /// <remarks>Used to request a change in the input method editor (IME) mode.</remarks>
    MODECHANGE = 31,

    /// <summary>Spacebar key.</summary>
    SPACE = 32,

    /// <summary>Page Up key.</summary>
    PRIOR = 33,

    /// <summary>Page Down key.</summary>
    NEXT = 34,

    /// <summary>End key.</summary>
    END = 35,

    /// <summary>Home key.</summary>
    HOME = 36,

    /// <summary>Left Arrow key.</summary>
    LEFT = 37,

    /// <summary>Up Arrow key.</summary>
    UP = 38,

    /// <summary>Right Arrow key.</summary>
    RIGHT = 39,

    /// <summary>Down Arrow key.</summary>
    DOWN = 40,

    /// <summary>Select key.</summary>
    SELECT = 41,

    /// <summary>Print key.</summary>
    PRINT = 42,

    /// <summary>Execute key.</summary>
    EXECUTE = 43,

    /// <summary>Print Screen key.</summary>
    SNAPSHOT = 44,

    /// <summary>Insert key.</summary>
    INSERT = 45,

    /// <summary>Delete key.</summary>
    DELETE = 46,

    /// <summary>Help key.</summary>
    HELP = 47,

    /// <summary>0 key.</summary>
    KEY_0 = 48,

    /// <summary>1 key.</summary>
    KEY_1 = 49,

    /// <summary>2 key.</summary>
    KEY_2 = 50,

    /// <summary>3 key.</summary>
    KEY_3 = 51,

    /// <summary>4 key.</summary>
    KEY_4 = 52,

    /// <summary>5 key.</summary>
    KEY_5 = 53,

    /// <summary>6 key.</summary>
    KEY_6 = 54,

    /// <summary>7 key.</summary>
    KEY_7 = 55,

    /// <summary>8 key.</summary>
    KEY_8 = 56,

    /// <summary>9 key.</summary>
    KEY_9 = 57,

    /// <summary>A key.</summary>
    KEY_A = 65,

    /// <summary>B key.</summary>
    KEY_B = 66,

    /// <summary>C key.</summary>
    KEY_C = 67,

    /// <summary>D key.</summary>
    KEY_D = 68,

    /// <summary>E key.</summary>
    KEY_E = 69,

    /// <summary>F key.</summary>
    KEY_F = 70,

    /// <summary>G key.</summary>
    KEY_G = 71,

    /// <summary>H key.</summary>
    KEY_H = 72,

    /// <summary>I key.</summary>
    KEY_I = 73,

    /// <summary>J key.</summary>
    KEY_J = 74,

    /// <summary>K key.</summary>
    KEY_K = 75,

    /// <summary>L key.</summary>
    KEY_L = 76,

    /// <summary>M key.</summary>
    KEY_M = 77,

    /// <summary>N key.</summary>
    KEY_N = 78,

    /// <summary>O key.</summary>
    KEY_O = 79,

    /// <summary>P key.</summary>
    KEY_P = 80,

    /// <summary>Q key.</summary>
    KEY_Q = 81,

    /// <summary>R key.</summary>
    KEY_R = 82,

    /// <summary>S key.</summary>
    KEY_S = 83,

    /// <summary>T key.</summary>
    KEY_T = 84,

    /// <summary>U key.</summary>
    KEY_U = 85,

    /// <summary>V key.</summary>
    KEY_V = 86,

    /// <summary>W key.</summary>
    KEY_W = 87,

    /// <summary>X key.</summary>
    KEY_X = 88,

    /// <summary>Y key.</summary>
    KEY_Y = 89,

    /// <summary>Z key.</summary>
    KEY_Z = 90,

    /// <summary>Left Win key.</summary>
    LWIN = 91,

    /// <summary>Right Win key.</summary>
    RWIN = 92,

    /// <summary>Context Menu key.</summary>
    APPS = 93,

    /// <summary>Computer Sleep key.</summary>
    SLEEP = 95,

    /// <summary>NumPad0 key.</summary>
    NUMPAD0 = 96,

    /// <summary>NumPad1 key.</summary>
    NUMPAD1 = 97,

    /// <summary>NumPad2 key.</summary>
    NUMPAD2 = 98,

    /// <summary>NumPad3 key.</summary>
    NUMPAD3 = 99,

    /// <summary>NumPad4 key.</summary>
    NUMPAD4 = 100,

    /// <summary>NumPad5 key.</summary>
    NUMPAD5 = 101,

    /// <summary>NumPad6 key.</summary>
    NUMPAD6 = 102,

    /// <summary>NumPad7 key.</summary>
    NUMPAD7 = 103,

    /// <summary>NumPad8 key.</summary>
    NUMPAD8 = 104,

    /// <summary>NumPad9 key.</summary>
    NUMPAD9 = 105,

    /// <summary>NumPad * key.</summary>
    MULTIPLY = 106,

    /// <summary>NumPad + key.</summary>
    ADD = 107,

    /// <summary>Separator key.</summary>
    SEPARATOR = 108,

    /// <summary>NumPad - key.</summary>
    SUBTRACT = 109,

    /// <summary>NumPad . key.</summary>
    DECIMAL = 110,

    /// <summary>NumPad / key.</summary>
    DIVIDE = 111,

    /// <summary>F1 key.</summary>
    F1 = 112,

    /// <summary>F2 key.</summary>
    F2 = 113,

    /// <summary>F3 key.</summary>
    F3 = 114,

    /// <summary>F4 key.</summary>
    F4 = 115,

    /// <summary>F5 key.</summary>
    F5 = 116,

    /// <summary>F6 key.</summary>
    F6 = 117,

    /// <summary>F7 key.</summary>
    F7 = 118,

    /// <summary>F8 key.</summary>
    F8 = 119,

    /// <summary>F9 key.</summary>
    F9 = 120,

    /// <summary>F10 key.</summary>
    F10 = 121,

    /// <summary>F11 key.</summary>
    F11 = 122,

    /// <summary>F12 key.</summary>
    F12 = 123,

    /// <summary>F13 key.</summary>
    F13 = 124,

    /// <summary>F14 key.</summary>
    F14 = 125,

    /// <summary>F15 key.</summary>
    F15 = 126,

    /// <summary>F16 key.</summary>
    F16 = 127,

    /// <summary>F17 key.</summary>
    F17 = 128,

    /// <summary>F18 key.</summary>
    F18 = 129,

    /// <summary>F19 key.</summary>
    F19 = 130,

    /// <summary>F20 key.</summary>
    F20 = 131,

    /// <summary>F21 key.</summary>
    F21 = 132,

    /// <summary>F22 key.</summary>
    F22 = 133,

    /// <summary>F23 key.</summary>
    F23 = 134,

    /// <summary>F24 key.</summary>
    F24 = 135,

    /// <summary>Navigation View key.</summary>
    VK_NAVIGATION_VIEW = 136,

    /// <summary>Navigation Menu key.</summary>
    VK_NAVIGATION_MENU = 137,

    /// <summary>Navigation Up key.</summary>
    VK_NAVIGATION_UP = 138,

    /// <summary>Navigation Down key.</summary>
    VK_NAVIGATION_DOWN = 139,

    /// <summary>Navigation Left key.</summary>
    VK_NAVIGATION_LEFT = 140,

    /// <summary>Navigation Right key.</summary>
    VK_NAVIGATION_RIGHT = 141,

    /// <summary>Navigation Accept key.</summary>
    VK_NAVIGATION_ACCEPT = 142,

    /// <summary>Navigation Cancel key.</summary>
    VK_NAVIGATION_CANCEL = 143,

    /// <summary>NumLock key.</summary>
    NUMLOCK = 144,

    /// <summary>ScrLock key.</summary>
    SCROLL = 145,

    /// <summary>Jisho key.</summary>
    OEM_FJ_JISHO = 146,

    /// <summary>NEC Equal key.</summary>
    VK_OEM_NEC_EQUAL = 146,

    /// <summary>Mashu key.</summary>
    OEM_FJ_MASSHOU = 147,

    /// <summary>Touroku key.</summary>
    OEM_FJ_TOUROKU = 148,

    /// <summary>Loya key.</summary>
    OEM_FJ_LOYA = 149,

    /// <summary>Roya key.</summary>
    OEM_FJ_ROYA = 150,

    /// <summary>Left Shift key.</summary>
    LSHIFT = 160,

    /// <summary>Right Shift key.</summary>
    RSHIFT = 161,

    /// <summary>Left Ctrl key.</summary>
    LCONTROL = 162,

    /// <summary>Right Ctrl key.</summary>
    RCONTROL = 163,

    /// <summary>Left Alt key.</summary>
    LMENU = 164,

    /// <summary>Right Alt key.</summary>
    RMENU = 165,

    /// <summary>Browser Back key.</summary>
    BROWSER_BACK = 166,

    /// <summary>Browser Forward key.</summary>
    BROWSER_FORWARD = 167,

    /// <summary>Browser Refresh key.</summary>
    BROWSER_REFRESH = 168,

    /// <summary>Browser Stop key.</summary>
    BROWSER_STOP = 169,

    /// <summary>Browser Search key.</summary>
    BROWSER_SEARCH = 170,

    /// <summary>Browser Favorites key.</summary>
    BROWSER_FAVORITES = 171,

    /// <summary>Browser Home key.</summary>
    BROWSER_HOME = 172,

    /// <summary>Volume Mute key.</summary>
    VOLUME_MUTE = 173,

    /// <summary>Volume Down key.</summary>
    VOLUME_DOWN = 174,

    /// <summary>Volume Up key.</summary>
    VOLUME_UP = 175,

    /// <summary>Next Track key.</summary>
    MEDIA_NEXT_TRACK = 176,

    /// <summary>Previous Track key.</summary>
    MEDIA_PREV_TRACK = 177,

    /// <summary>Stop Media key.</summary>
    MEDIA_STOP = 178,

    /// <summary>Play/Pause Media key.</summary>
    MEDIA_PLAY_PAUSE = 179,

    /// <summary>Start Mail key.</summary>
    LAUNCH_MAIL = 180,

    /// <summary>Select Media key.</summary>
    LAUNCH_MEDIA_SELECT = 181,

    /// <summary>Start App1 key.</summary>
    LAUNCH_APP1 = 182,

    /// <summary>Start App2 key.</summary>
    LAUNCH_APP2 = 183,

    /// <summary>OEM_1 (: ;) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ;: key.</remarks>
    OEM_1 = 186,

    /// <summary>OEM_PLUS (= +) key.</summary>
    /// <remarks>For any country/region, the =+ key</remarks>
    OEM_PLUS = 187,

    /// <summary>OEM_COMMA (, <) key.</summary>
    /// <remarks>For any country/region, the ,< key</remarks>
    OEM_COMMA = 188,

    /// <summary>OEM_MINUS (- _) key.</summary>
    /// <remarks>For any country/region, the -_ key</remarks>
    OEM_MINUS = 189,

    /// <summary>OEM_PERIOD (. >) key.</summary>
    /// <remarks>For any country/region, the .> key</remarks>
    OEM_PERIOD = 190,

    /// <summary>OEM_2 (/ ?) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the /? key.</remarks>
    OEM_2 = 191,

    /// <summary>OEM_3, Backtick Key (` ~).</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the `~ key.</remarks>
    OEM_3 = 192,

    /// <summary>Abnt C1 key.</summary>
    ABNT_C1 = 193,

    /// <summary>Abnt C2 key.</summary>
    ABNT_C2 = 194,

    /// <summary>Gamepad A key.</summary>
    VK_GAMEPAD_A = 195,

    /// <summary>Gamepad B key.</summary>
    VK_GAMEPAD_B = 196,

    /// <summary>Gamepad X key.</summary>
    VK_GAMEPAD_X = 197,

    /// <summary>Gamepad Y key.</summary>
    VK_GAMEPAD_Y = 198,

    /// <summary>Gamepad Right Shoulder key.</summary>
    VK_GAMEPAD_RIGHT_SHOULDER = 199,

    /// <summary>Gamepad Left Shoulder key.</summary>
    VK_GAMEPAD_LEFT_SHOULDER = 200,

    /// <summary>Gamepad Left Trigger key.</summary>
    VK_GAMEPAD_LEFT_TRIGGER = 201,

    /// <summary>Gamepad Right Trigger key.</summary>
    VK_GAMEPAD_RIGHT_TRIGGER = 202,

    /// <summary>Gamepad Dpad Up key.</summary>
    VK_GAMEPAD_DPAD_UP = 203,

    /// <summary>Gamepad Dpad Down key.</summary>
    VK_GAMEPAD_DPAD_DOWN = 204,

    /// <summary>Gamepad Dpad Left key.</summary>
    VK_GAMEPAD_DPAD_LEFT = 205,

    /// <summary>Gamepad Dpad Right key.</summary>
    VK_GAMEPAD_DPAD_RIGHT = 206,

    /// <summary>Gamepad Menu key.</summary>
    VK_GAMEPAD_MENU = 207,

    /// <summary>Gamepad View key.</summary>
    VK_GAMEPAD_VIEW = 208,

    /// <summary>Gamepad Left Thumbstick button key.</summary>
    VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 209,

    /// <summary>Gamepad Right Thumbstick button key.</summary>
    VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 210,

    /// <summary>Gamepad Left Thumbstick Up key.</summary>
    VK_GAMEPAD_LEFT_THUMBSTICK_UP = 211,

    /// <summary>Gamepad Left Thumbstick Down key.</summary>
    VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 212,

    /// <summary>Gamepad Left Thumbstick Right key.</summary>
    VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 213,

    /// <summary>Gamepad Left Thumbstick Left key.</summary>
    VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 214,

    /// <summary>Gamepad Right Thumbstick Up key.</summary>
    VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 215,

    /// <summary>Gamepad Right Thumbstick Down key.</summary>
    VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 216,

    /// <summary>Gamepad Right Thumbstick Right key.</summary>
    VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 217,

    /// <summary>Gamepad Right Thumbstick Left key.</summary>
    VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 218,

    /// <summary>OEM_4 ([ {) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the [{ key.</remarks>
    OEM_4 = 219,

    /// <summary>OEM_5 (\ |) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the \| key.</remarks>
    OEM_5 = 220,

    /// <summary>OEM_6 (]}) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ]} key.</remarks>
    OEM_6 = 221,

    /// <summary>OEM_7 (' ") key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '" key.</remarks>
    OEM_7 = 222,

    /// <summary>OEM_8 (§ !) key.</summary>
    /// <remarks>Used for miscellaneous characters; it can vary by keyboard.</remarks>
    OEM_8 = 223,

    /// <summary>Ax key.</summary>
    OEM_AX = 225,

    /// <summary>OEM_102 (\\| <>) key.</summary>
    /// <remarks>The <> keys on the US standard keyboard, or the \\| key on the non-US 102-key keyboard (usually located between the "Left Shift" and "Z" keys on European keyboards).</remarks>
    OEM_102 = 226,

    /// <summary>IcoHlp key.</summary>
    ICO_HELP = 227,

    /// <summary>Ico00 key.</summary>
    /// <remarks>Produces '00' (two zeros) when presse.</remarks>
    ICO_00 = 228,

    /// <summary>Process key.</summary>
    PROCESSKEY = 229,

    /// <summary>IcoClr key.</summary>
    ICO_CLEAR = 230,

    /// <summary>Packet key.</summary>
    /// <remarks>Used to pass Unicode characters as if they were keystrokes. The PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods.</remarks>
    PACKET = 231,

    /// <summary>Reset key.</summary>
    OEM_RESET = 233,

    /// <summary>Jump key.</summary>
    OEM_JUMP = 234,

    /// <summary>OemPa1 key.</summary>
    OEM_PA1 = 235,

    /// <summary>OemPa2 key.</summary>
    OEM_PA2 = 236,

    /// <summary>OemPa3 key.</summary>
    OEM_PA3 = 237,

    /// <summary>WsCtrl key.</summary>
    OEM_WSCTRL = 238,

    /// <summary>Cu Sel key.</summary>
    OEM_CUSEL = 239,

    /// <summary>Alphanumeric key.</summary>
    VK_DBE_ALPHANUMERIC = 240,

    /// <summary>Oem Attn key.</summary>
    OEM_ATTN = 240,

    /// <summary>Katakana key.</summary>
    VK_DBE_KATAKANA = 241,

    /// <summary>Finish key.</summary>
    OEM_FINISH = 241,

    /// <summary>Hiragana key.</summary>
    VK_DBE_HIRAGANA = 242,

    /// <summary>Copy key.</summary>
    OEM_COPY = 242,

    /// <summary>Single-Byte Character key.</summary>
    VK_DBE_SBCSCHAR = 243,

    /// <summary>Auto key.</summary>
    OEM_AUTO = 243,

    /// <summary>Double-Byte Character key.</summary>
    VK_DBE_DBCSCHAR = 244,

    /// <summary>Enlw key.</summary>
    OEM_ENLW = 244,

    /// <summary>Roman key.</summary>
    VK_DBE_ROMAN = 245,

    /// <summary>Back Tab key.</summary>
    OEM_BACKTAB = 245,

    /// <summary>Attn key.</summary>
    ATTN = 246,

    /// <summary>No Roman key.</summary>
    VK_DBE_NOROMAN = 246,

    /// <summary>Cr Sel key.</summary>
    CRSEL = 247,

    /// <summary>Word Register Mode key.</summary>
    VK_DBE_ENTERWORDREGISTERMODE = 247,

    /// <summary>Ime Config Mode key.</summary>
    VK_DBE_ENTERIMECONFIGMODE = 248,

    /// <summary>Ex Sel key.</summary>
    EXSEL = 248,

    /// <summary>Flush String key.</summary>
    VK_DBE_FLUSHSTRING = 249,

    /// <summary>Erase Eof key.</summary>
    EREOF = 249,

    /// <summary>Code Input key.</summary>
    VK_DBE_CODEINPUT = 250,

    /// <summary>Play key.</summary>
    PLAY = 250,

    /// <summary>No Code Input key.</summary>
    VK_DBE_NOCODEINPUT = 251,

    /// <summary>Zoom key.</summary>
    ZOOM = 251,

    /// <summary>Determine String key.</summary>
    VK_DBE_DETERMINESTRING = 252,

    /// <summary>NoName key.</summary>
    /// <remarks>Reserved for future use.</remarks>
    NONAME = 252,

    /// <summary>Dialog Conversion Mode key.</summary>
    VK_DBE_ENTERDLGCONVERSIONMODE = 253,

    /// <summary>Pa1 key.</summary>
    PA1 = 253,

    /// <summary>OemClr key.</summary>
    OEM_CLEAR = 254,

    /// <summary>Key is not recognized key.</summary>
    VK_UNKNOWN = 255,

    /// <summary>Not Virtual Key</summary>
    VK__none_ = 255,
}

/// <summary>Represents the scan codes for various keys on a keyboard.</summary>
/// <remarks>
/// <para>Scan codes are different from virtual key codes in that they are used to identify the physical location of a key on the keyboard.
/// For example, the 'A' key on a QWERTY keyboard will have a different scan code than the 'A' key on a Dvorak keyboard, while the virtual key code will be the same.</para>
/// <para>Unlike Virtual codes for special keys "Ctrl", "Alt" and "Windows", there is no distinction between left- and right-hand side versions of the scan codes, except for Shift (<see cref="LSHIFT"/> and <see cref="RSHIFT"/>).</para>
/// </remarks>
public enum ScanCode : ushort
{
    /// <summary>Escape key.</summary>
    ESCAPE = 1,

    /// <summary>Number 1 key.</summary>
    KEY_1 = 2,

    /// <summary>Number 2 key.</summary>
    KEY_2 = 3,

    /// <summary>Number 3 key.</summary>
    KEY_3 = 4,

    /// <summary>Number 4 key.</summary>
    KEY_4 = 5,

    /// <summary>Number 5 key.</summary>
    KEY_5 = 6,

    /// <summary>Number 6 key.</summary>
    KEY_6 = 7,

    /// <summary>Number 7 key.</summary>
    KEY_7 = 8,

    /// <summary>Number 8 key.</summary>
    KEY_8 = 9,

    /// <summary>Number 9 key.</summary>
    KEY_9 = 10,

    /// <summary>Number 0 key.</summary>
    KEY_0 = 11,

    /// <summary>OEM minus key.</summary>
    OEM_MINUS = 12,

    /// <summary>OEM plus key.</summary>
    OEM_PLUS = 13,

    /// <summary>Backspace key.</summary>
    BACK = 14,

    /// <summary>Tab key.</summary>
    TAB = 15,

    /// <summary>Q key.</summary>
    KEY_Q = 16,

    /// <summary>W key.</summary>
    KEY_W = 17,

    /// <summary>E key.</summary>
    KEY_E = 18,

    /// <summary>R key.</summary>
    KEY_R = 19,

    /// <summary>T key.</summary>
    KEY_T = 20,

    /// <summary>Y key.</summary>
    KEY_Y = 21,

    /// <summary>U key.</summary>
    KEY_U = 22,

    /// <summary>I key.</summary>
    KEY_I = 23,

    /// <summary>O key.</summary>
    KEY_O = 24,

    /// <summary>P key.</summary>
    KEY_P = 25,

    /// <summary>OEM 4 key.</summary>
    OEM_4 = 26,

    /// <summary>OEM 6 key.</summary>
    OEM_6 = 27,

    /// <summary>Return (Enter) key.</summary>
    RETURN = 28,

    /// <summary>Control key.</summary>
    CONTROL = 29,

    /// <summary>A key.</summary>
    KEY_A = 30,

    /// <summary>S key.</summary>
    KEY_S = 31,

    /// <summary>D key.</summary>
    KEY_D = 32,

    /// <summary>F key.</summary>
    KEY_F = 33,

    /// <summary>G key.</summary>
    KEY_G = 34,

    /// <summary>H key.</summary>
    KEY_H = 35,

    /// <summary>J key.</summary>
    KEY_J = 36,

    /// <summary>K key.</summary>
    KEY_K = 37,

    /// <summary>L key.</summary>
    KEY_L = 38,

    /// <summary>OEM 1 key.</summary>
    OEM_1 = 39,

    /// <summary>OEM 7 key.</summary>
    OEM_7 = 40,

    /// <summary>OEM_3, backtick key (` ~).</summary>
    OEM_3 = 41,

    /// <summary>Left Shift key.</summary>
    LSHIFT = 42,

    /// <summary>OEM 5 key.</summary>
    OEM_5 = 43,

    /// <summary>Z key.</summary>
    KEY_Z = 44,

    /// <summary>X key.</summary>
    KEY_X = 45,

    /// <summary>C key.</summary>
    KEY_C = 46,

    /// <summary>V key.</summary>
    KEY_V = 47,

    /// <summary>B key.</summary>
    KEY_B = 48,

    /// <summary>Volume Up key.</summary>
    VOLUME_UP = 48,

    /// <summary>N key.</summary>
    KEY_N = 49,

    /// <summary>M key.</summary>
    KEY_M = 50,

    /// <summary>OEM comma key.</summary>
    OEM_COMMA = 51,

    /// <summary>OEM period key.</summary>
    OEM_PERIOD = 52,

    /// <summary>OEM 2 key.</summary>
    OEM_2 = 53,

    /// <summary>Right Shift key.</summary>
    RSHIFT = 54,

    /// <summary>Print Screen (Snapshot) key.</summary>
    SNAPSHOT = 55,

    /// <summary>Menu key.</summary>
    MENU = 56,

    /// <summary>Spacebar key.</summary>
    SPACE = 57,

    /// <summary>Caps Lock key.</summary>
    CAPITAL = 58,

    /// <summary>F1 key.</summary>
    F1 = 59,

    /// <summary>F2 key.</summary>
    F2 = 60,

    /// <summary>F3 key.</summary>
    F3 = 61,

    /// <summary>F4 key.</summary>
    F4 = 62,

    /// <summary>F5 key.</summary>
    F5 = 63,

    /// <summary>F6 key.</summary>
    F6 = 64,

    /// <summary>F7 key.</summary>
    F7 = 65,

    /// <summary>F8 key.</summary>
    F8 = 66,

    /// <summary>F9 key.</summary>
    F9 = 67,

    /// <summary>F10 key.</summary>
    F10 = 68,

    /// <summary>NumLock key.</summary>
    NUMLOCK = 69,

    /// <summary>Pause key.</summary>
    /// <remarks>Used to pause the output of a DOS application or batch file.</remarks>
    PAUSE = 69,

    /// <summary>ScrLock key.</summary>
    SCROLL = 70,

    /// <summary>Cancel key.</summary>
    /// <remarks>Used to terminate a DOS application or batch file.</remarks>
    CANCEL = 70,

    /// <summary>Home key.</summary>
    HOME = 71,

    /// <summary>Up Arrow key.</summary>
    UP = 72,

    /// <summary>Page Up key.</summary>
    PRIOR = 73,

    /// <summary>Subtract key.</summary>
    SUBTRACT = 74,

    /// <summary>Left Arrow key.</summary>
    LEFT = 75,

    /// <summary>Clear/Center key (NumPad5 with NumLock off).</summary>
    CLEAR = 76,

    /// <summary>Right Arrow key.</summary>
    RIGHT = 77,

    /// <summary>Add key.</summary>
    ADD = 78,

    /// <summary>End key.</summary>
    END = 79,

    /// <summary>Down Arrow key.</summary>
    DOWN = 80,

    /// <summary>Page Down key.</summary>
    NEXT = 81,

    /// <summary>Insert key.</summary>
    INSERT = 82,

    /// <summary>Delete key.</summary>
    DELETE = 83,

    /// <summary>OEM 102 key (backslash on European keyboards).</summary>
    OEM_102 = 86, // '\' in European keyboards (between LShift and Z).
}

/// <summary>Provides a method to resolve the display names of keys based on their virtual key codes.</summary>
public static class KeysMapper
{
    /// <summary>Maps OEM keys to their display names.</summary>
    static readonly Dictionary<VirtualKey, char> OemKeyNames = new()
    {
        { VirtualKey.OEM_1, ';' },
        { VirtualKey.OEM_PLUS, '=' },
        { VirtualKey.OEM_COMMA, ',' },
        { VirtualKey.OEM_MINUS, '-' },
        { VirtualKey.OEM_PERIOD, '.' },
        { VirtualKey.OEM_2, '/' },
        { VirtualKey.OEM_3, '`' },
        { VirtualKey.OEM_4, '[' },
        { VirtualKey.OEM_5, '\\' },
        { VirtualKey.OEM_6, ']' },
        { VirtualKey.OEM_7, '\'' },
        { VirtualKey.OEM_8, (char)0xDF },
        { VirtualKey.OEM_102, '\\' },
    };

    /// <summary>Maps OEM and number row keys to their display names when Shift is pressed.</summary>
    static readonly Dictionary<VirtualKey, char> OemAndNumRowKeyNamesShifted = new()
    {
        { VirtualKey.OEM_1, ':' },
        { VirtualKey.OEM_PLUS, '+' },
        { VirtualKey.OEM_COMMA, '<' },
        { VirtualKey.OEM_MINUS, '_' },
        { VirtualKey.OEM_PERIOD, '>' },
        { VirtualKey.OEM_2, '?' },
        { VirtualKey.OEM_3, '~' },
        { VirtualKey.OEM_4, '{' },
        { VirtualKey.OEM_5, '|' },
        { VirtualKey.OEM_6, '}' },
        { VirtualKey.OEM_7, '\"' },
        { VirtualKey.OEM_8, (char)0xDF },
        { VirtualKey.OEM_102, '|' },
        { VirtualKey.KEY_0, ')' },
        { VirtualKey.KEY_1, '!' },
        { VirtualKey.KEY_2, '@' },
        { VirtualKey.KEY_3, '#' },
        { VirtualKey.KEY_4, '$' },
        { VirtualKey.KEY_5, '%' },
        { VirtualKey.KEY_6, '^' },
        { VirtualKey.KEY_7, '&' },
        { VirtualKey.KEY_8, '*' },
        { VirtualKey.KEY_9, '(' },
    };

    /// <summary>Returns the display name for the given virtual key and shift state.</summary>
    /// <param name="key">The virtual key code.</param>
    /// <param name="isShiftDown">True if the Shift key is currently down.</param>
    /// <returns>A string representing the display name of the key.</returns>
    public static string GetDisplayName(VirtualKey key, bool isShiftDown = false, bool isCapsLockOn = false)
    {
        var dict = isShiftDown ? OemAndNumRowKeyNamesShifted : OemKeyNames;
        if (dict.TryGetValue(key, out var display))
            return display.ToString();

        if (key >= VirtualKey.KEY_0 && key <= VirtualKey.KEY_9)
            return key.ToString()[4..]; // Remove "KEY_" prefix, (e.g., "KEY_0" -> "0")

        if (key >= VirtualKey.KEY_A && key <= VirtualKey.KEY_Z)
        {
            // Letters are uppercase if either Caps Lock is active or Shift is pressed, not both
            bool isUpperCase = isCapsLockOn ^ isShiftDown;
            return isUpperCase ? key.ToString()[4..] : key.ToString()[4..].ToLowerInvariant();
        }

        // For other keys, return the VirtualKey enum's name
        return key.ToString();
    }

    /// <summary>Returns the ASCII code for the given virtual key and shift state.</summary>
    /// <remarks>Note: This is a simplified mapping and might not cover all international layouts or special keys accurately. For complex scenarios, consider ToAsciiEx or similar Win32 functions with keyboard layouts.</remarks>
    /// <param name="key">The virtual key code.</param>
    /// <param name="isShiftDown">True if the Shift key is currently down.</param>
    /// <returns>The ASCII code of the key, or 0 if not a standard printable ASCII character.</returns>
    public static uint GetAsciiCode(VirtualKey key, bool isShiftDown = false, bool isCapsLockOn = false)
    {
        if (key >= VirtualKey.KEY_A && key <= VirtualKey.KEY_Z)
        {
            bool isUpperCase = isCapsLockOn ^ isShiftDown;
            return isUpperCase
                ? (uint)key // Shifted letters are the same as their virtual key codes
                : (uint)(key + 32); // Lowercase letters are 32 positions after uppercase in ASCII
        }

        if (key >= VirtualKey.KEY_0 && key <= VirtualKey.KEY_9)
        {
            // Un-shifted number row keys has the same ASCII as their virtual key codes
            return isShiftDown ? OemAndNumRowKeyNamesShifted[key] : (uint)key;
        }

        if (key >= VirtualKey.NUMPAD0 && key <= VirtualKey.NUMPAD9)
        {
            return (uint)key; // Numpad keys have the same ASCII as their virtual key codes
        }

        // OEM and symbol keys
        string display = GetDisplayName(key, isShiftDown, isCapsLockOn);
        if (!string.IsNullOrEmpty(display) && display.Length == 1)
        {
            return display[0];
        }

        // Modifiers, function, and utility keys have ASCII value of 0
        return PInvoke.MapVirtualKey((uint)key, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_CHAR);
    }

    /// <summary>
    /// Converts a string key name to its corresponding VirtualKey enum value.
    /// </summary>
    /// <param name="keyName">The string name of the key (e.g., "Z", "F1", "Escape").</param>
    /// <returns>The VirtualKey enum value.</returns>
    /// <exception cref="ArgumentException">Thrown if the key name is unknown.</exception>
    public static VirtualKey GetVirtualKeyFromString(string keyName)
    {
        // First, try to parse directly from the VirtualKey enum
        if (Enum.TryParse(keyName, true, out VirtualKey virtualKey))
        {
            return virtualKey;
        }
        // Handle common aliases or special cases not directly in the enum name
        // csharpier-ignore-start
        switch (keyName.ToLowerInvariant())
        {
            case "ctrl": return VirtualKey.CONTROL;
            case "lctrl": return VirtualKey.LCONTROL;
            case "rctrl": return VirtualKey.RCONTROL;
            case "shift": return VirtualKey.SHIFT;
            case "lshift": return VirtualKey.LSHIFT;
            case "rshift": return VirtualKey.RSHIFT;
            case "alt": return VirtualKey.MENU;
            case "lalt": return VirtualKey.LMENU;
            case "ralt": return VirtualKey.RMENU;
            case "win": return VirtualKey.LWIN; // Using LWIN as general Win key
            case "lwin": return VirtualKey.LWIN;
            case "rwin": return VirtualKey.RWIN;
            case "backtick": return VirtualKey.OEM_3;
            case "capslock": return VirtualKey.CAPITAL;
            case "numlock": return VirtualKey.NUMLOCK;
            case "scrolllock": return VirtualKey.SCROLL;
            case "space": return VirtualKey.SPACE;
            case "enter": return VirtualKey.RETURN;
            case "tab": return VirtualKey.TAB;
            case "backspace": return VirtualKey.BACK;
            case "delete": return VirtualKey.DELETE;
            case "insert": return VirtualKey.INSERT;
            case "home": return VirtualKey.HOME;
            case "end": return VirtualKey.END;
            case "pageup": return VirtualKey.PRIOR;
            case "pagedown": return VirtualKey.NEXT;
            case "left": return VirtualKey.LEFT;
            case "right": return VirtualKey.RIGHT;
            case "up": return VirtualKey.UP;
            case "down": return VirtualKey.DOWN;
            case "printscreen": return VirtualKey.SNAPSHOT;
            case "pause": return VirtualKey.PAUSE;
            // Add more aliases as needed
        }
        // csharpier-ignore-end

        // For single letter/number keys, try appending "KEY_"
        if (keyName.Length == 1 && char.IsLetterOrDigit(keyName[0]))
        {
            if (Enum.TryParse($"KEY_{keyName.ToUpperInvariant()}", true, out virtualKey))
            {
                return virtualKey;
            }
        }
        // For F-keys, try appending "F"
        if (keyName.StartsWith("F", StringComparison.OrdinalIgnoreCase) && keyName.Length > 1 && int.TryParse(keyName.Substring(1), out int fNum))
        {
            if (fNum >= 1 && fNum <= 24) // Assuming F1-F24
            {
                if (Enum.TryParse($"F{fNum}", true, out virtualKey))
                {
                    return virtualKey;
                }
            }
        }
        // For Numpad keys, try appending "NUMPAD"
        if (keyName.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) && keyName.Length > 6 && int.TryParse(keyName.Substring(6), out int numPadNum))
        {
            if (numPadNum >= 0 && numPadNum <= 9)
            {
                if (Enum.TryParse($"NUMPAD{numPadNum}", true, out virtualKey))
                {
                    return virtualKey;
                }
            }
        }

        throw new ArgumentException($"Unknown key name: '{keyName}'. Please ensure it matches a VirtualKey enum name or a defined alias.");
    }
}
