using System.Management;

namespace Macrosharp.Win32.Abstractions.SystemControl;

/// <summary>
/// Controls screen brightness on laptops and supported displays via WMI.
/// Uses the WmiMonitorBrightnessMethods class in the root\wmi namespace.
/// </summary>
public static class BrightnessControl
{
    private const int StepSize = 10;
    private const int MinBrightness = 0;
    private const int MaxBrightness = 100;

    /// <summary>Increases screen brightness by one step (10%). Returns the new brightness level, or -1 on failure.</summary>
    public static int IncreaseBrightness()
    {
        return AdjustBrightness(StepSize);
    }

    /// <summary>Decreases screen brightness by one step (10%). Returns the new brightness level, or -1 on failure.</summary>
    public static int DecreaseBrightness()
    {
        return AdjustBrightness(-StepSize);
    }

    private static int AdjustBrightness(int delta)
    {
        try
        {
            int current = GetCurrentBrightness();
            if (current < 0)
                return -1;

            int target = Math.Clamp(current + delta, MinBrightness, MaxBrightness);
            SetBrightness(target);
            return target;
        }
        catch
        {
            return -1;
        }
    }

    private static int GetCurrentBrightness()
    {
        using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
        foreach (ManagementObject obj in searcher.Get())
        {
            return Convert.ToInt32(obj["CurrentBrightness"]);
        }
        return -1;
    }

    private static void SetBrightness(int level)
    {
        using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
        foreach (ManagementObject obj in searcher.Get())
        {
            obj.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)level });
        }
    }
}
