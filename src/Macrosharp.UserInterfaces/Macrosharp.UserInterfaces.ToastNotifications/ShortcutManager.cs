using System.Runtime.InteropServices;

namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Creates and manages the Start Menu shortcut required for toast notifications
/// in unpackaged desktop applications. The shortcut is stamped with the application's
/// AUMID so that Windows can correctly route toast activation events.
/// </summary>
internal static class ShortcutManager
{
    // PKEY_AppUserModel_ID: {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, 5
    private static readonly PropertyKey PKEY_AppUserModel_ID = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 5
    };

    private static readonly Guid IID_IPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

    /// <summary>
    /// Ensures a Start Menu shortcut exists for the application with the correct AUMID property.
    /// Creates or updates the shortcut as needed. This method is idempotent.
    /// </summary>
    public static void EnsureStartMenuShortcut(string appName, string exePath, string? iconPath, string aumid)
    {
        string shortcutPath = GetShortcutPath(appName);

        if (File.Exists(shortcutPath) && HasCorrectAumid(shortcutPath, aumid))
        {
            Console.WriteLine($"Start Menu shortcut already exists with correct AUMID: {shortcutPath}");
            return;
        }

        CreateShortcut(shortcutPath, appName, exePath, iconPath);
        StampAumid(shortcutPath, aumid);
        Console.WriteLine($"Start Menu shortcut created: {shortcutPath}");
    }

    private static string GetShortcutPath(string appName)
    {
        string programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        return Path.Combine(programsFolder, $"{appName}.lnk");
    }

    /// <summary>
    /// Creates the basic .lnk file using WScript.Shell (late-bound IDispatch).
    /// This avoids fragile IShellLinkW COM vtable interop entirely.
    /// </summary>
    private static void CreateShortcut(string shortcutPath, string appName, string exePath, string? iconPath)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell COM class not found.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            try
            {
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
                shortcut.Description = appName;

                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                {
                    shortcut.IconLocation = $"{iconPath},0";
                }
                else
                {
                    shortcut.IconLocation = $"{exePath},0";
                }

                shortcut.Save();
            }
            finally
            {
                Marshal.ReleaseComObject(shortcut);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(shell);
        }
    }

    /// <summary>
    /// Stamps the AUMID property on an existing .lnk file using
    /// SHGetPropertyStoreFromParsingName → IPropertyStore.
    /// </summary>
    private static void StampAumid(string shortcutPath, string aumid)
    {
        var iid = IID_IPropertyStore;
        int hr = SHGetPropertyStoreFromParsingName(
            shortcutPath,
            IntPtr.Zero,
            GPS_READWRITE,
            ref iid,
            out IPropertyStore propertyStore);

        Marshal.ThrowExceptionForHR(hr);

        try
        {
            SetAumidProperty(propertyStore, aumid);
            propertyStore.Commit();
        }
        finally
        {
            Marshal.ReleaseComObject(propertyStore);
        }
    }

    private static void SetAumidProperty(IPropertyStore propertyStore, string aumid)
    {
        var propVariant = new PropVariant();
        propVariant.vt = VT_LPWSTR;
        propVariant.data1 = Marshal.StringToCoTaskMemUni(aumid);

        try
        {
            var key = PKEY_AppUserModel_ID;
            propertyStore.SetValue(ref key, ref propVariant);
        }
        finally
        {
            PropVariantClear(ref propVariant);
        }
    }

    private static bool HasCorrectAumid(string shortcutPath, string expectedAumid)
    {
        try
        {
            var iid = IID_IPropertyStore;
            int hr = SHGetPropertyStoreFromParsingName(
                shortcutPath,
                IntPtr.Zero,
                GPS_DEFAULT,
                ref iid,
                out IPropertyStore propertyStore);

            if (hr < 0)
            {
                return false;
            }

            try
            {
                var propVariant = new PropVariant();
                var key = PKEY_AppUserModel_ID;
                propertyStore.GetValue(ref key, ref propVariant);

                try
                {
                    if (propVariant.vt == VT_LPWSTR && propVariant.data1 != IntPtr.Zero)
                    {
                        string? value = Marshal.PtrToStringUni(propVariant.data1);
                        return string.Equals(value, expectedAumid, StringComparison.Ordinal);
                    }

                    return false;
                }
                finally
                {
                    PropVariantClear(ref propVariant);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(propertyStore);
            }
        }
        catch
        {
            return false;
        }
    }

    // ── P/Invoke and COM interop definitions ────────────────────────────────

    private const ushort VT_LPWSTR = 31;
    private const int GPS_DEFAULT = 0;
    private const int GPS_READWRITE = 2;

    [DllImport("shell32.dll", PreserveSig = true)]
    private static extern int SHGetPropertyStoreFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        int flags,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }

    /// <summary>
    /// PROPVARIANT — 24 bytes on x64 (8-byte header + 16-byte data union).
    /// The data union must be 16 bytes to accommodate DECIMAL and other
    /// large variant types. Using two IntPtr fields covers 16 bytes on x64.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr data1;
        public IntPtr data2;
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, ref PropVariant pv);
        void SetValue(ref PropertyKey key, ref PropVariant propvar);
        void Commit();
    }
}
