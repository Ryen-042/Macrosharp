using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Macrosharp.Win32.Abstractions.SystemControl;

/// <summary>
/// Provides process suspend and resume operations for the active window's process.
/// Uses NtSuspendProcess/NtResumeProcess from ntdll.dll.
/// </summary>
public static class ProcessControl
{
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(nint processHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(nint processHandle);

    /// <summary>
    /// Suspends all threads of the process that owns the foreground window.
    /// Returns true if the process was suspended successfully.
    /// </summary>
    public static bool SuspendActiveWindowProcess()
    {
        return ExecuteOnActiveProcess(NtSuspendProcess);
    }

    /// <summary>
    /// Resumes all threads of the process that owns the foreground window.
    /// Returns true if the process was resumed successfully.
    /// </summary>
    public static bool ResumeActiveWindowProcess()
    {
        return ExecuteOnActiveProcess(NtResumeProcess);
    }

    private static bool ExecuteOnActiveProcess(Func<nint, int> ntAction)
    {
        HWND hwnd = PInvoke.GetForegroundWindow();
        if (hwnd == HWND.Null)
            return false;

        uint processId = 0;
        unsafe
        {
            PInvoke.GetWindowThreadProcessId(hwnd, &processId);
        }
        if (processId == 0)
            return false;

        // Do not suspend ourselves
        if (processId == (uint)Environment.ProcessId)
            return false;

        nint hProcess = nint.Zero;
        try
        {
            hProcess = PInvoke.OpenProcess(
                Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_SUSPEND_RESUME,
                false,
                processId);

            if (hProcess == nint.Zero)
                return false;

            int status = ntAction(hProcess);
            return status == 0; // NTSTATUS 0 = STATUS_SUCCESS
        }
        catch
        {
            return false;
        }
        finally
        {
            if (hProcess != nint.Zero)
                PInvoke.CloseHandle((HANDLE)hProcess);
        }
    }
}
