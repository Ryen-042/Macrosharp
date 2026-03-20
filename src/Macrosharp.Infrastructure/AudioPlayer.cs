using System.Media;
using System.Text;

namespace Macrosharp.Infrastructure;

public static class AudioPlayer
{
    private const int RepeatedFailureThreshold = 3;
    private static readonly object FailureGate = new();
    private static readonly Dictionary<string, int> FailureCounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional callback invoked once when repeated audio failures reach the escalation threshold for a sound key.
    /// </summary>
    public static Action<string>? RepeatedFailureNotifier { get; set; }

    public static void PlayAudio(string fileName, bool async = false, int volumePercent = 100)
    {
        if (async)
        {
            _ = Task.Run(() => PlayAudio(fileName, async: false, volumePercent));
            return;
        }

        using var stream = OpenWaveStream(fileName, volumePercent);
        using var player = new SoundPlayer(stream);
        player.PlaySync();
    }

    private static MemoryStream OpenWaveStream(string fileName, int volumePercent)
    {
        var clamped = Math.Clamp(volumePercent, 0, 100);
        var bytes = File.ReadAllBytes(fileName);

        if (clamped == 100)
        {
            return new MemoryStream(bytes, writable: false);
        }

        return new MemoryStream(ApplyVolumeToWav(bytes, clamped), writable: false);
    }

    private static byte[] ApplyVolumeToWav(byte[] wav, int volumePercent)
    {
        if (wav.Length < 44)
        {
            return wav;
        }

        if (!Encoding.ASCII.GetString(wav, 0, 4).Equals("RIFF", StringComparison.Ordinal) || !Encoding.ASCII.GetString(wav, 8, 4).Equals("WAVE", StringComparison.Ordinal))
        {
            return wav;
        }

        var bitsPerSample = 16;
        var dataOffset = -1;
        var dataSize = 0;
        var offset = 12;

        while (offset + 8 <= wav.Length)
        {
            var chunkId = Encoding.ASCII.GetString(wav, offset, 4);
            var chunkSize = BitConverter.ToInt32(wav, offset + 4);
            var chunkData = offset + 8;
            if (chunkData + chunkSize > wav.Length)
            {
                break;
            }

            if (chunkId == "fmt " && chunkSize >= 16)
            {
                bitsPerSample = BitConverter.ToInt16(wav, chunkData + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = chunkData;
                dataSize = chunkSize;
                break;
            }

            offset = chunkData + chunkSize + (chunkSize % 2);
        }

        if (dataOffset < 0 || dataSize <= 0)
        {
            return wav;
        }

        var output = new byte[wav.Length];
        Buffer.BlockCopy(wav, 0, output, 0, wav.Length);

        var volume = volumePercent / 100.0;
        if (bitsPerSample == 16)
        {
            for (var i = dataOffset; i + 1 < dataOffset + dataSize; i += 2)
            {
                var sample = BitConverter.ToInt16(output, i);
                var scaled = (int)Math.Round(sample * volume);
                scaled = Math.Clamp(scaled, short.MinValue, short.MaxValue);
                var bytes = BitConverter.GetBytes((short)scaled);
                output[i] = bytes[0];
                output[i + 1] = bytes[1];
            }
        }
        else if (bitsPerSample == 8)
        {
            for (var i = dataOffset; i < dataOffset + dataSize; i++)
            {
                var centered = output[i] - 128;
                var scaled = (int)Math.Round(centered * volume);
                scaled = Math.Clamp(scaled, -128, 127);
                output[i] = (byte)(scaled + 128);
            }
        }

        return output;
    }

    private static void PlayPreset(string soundFileName, bool shouldPlayAsync, int volumePercent, string operation)
    {
        string fullPath = PathLocator.GetSfxPath(soundFileName);

        try
        {
            PlayAudio(fullPath, async: shouldPlayAsync, volumePercent: volumePercent);
            ResetFailureCount(operation);
        }
        catch (FileNotFoundException ex)
        {
            HandlePlaybackFailure(operation, fullPath, ex, "Audio file not found");
        }
        catch (DirectoryNotFoundException ex)
        {
            HandlePlaybackFailure(operation, fullPath, ex, "Audio directory not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            HandlePlaybackFailure(operation, fullPath, ex, "Audio file access denied");
        }
        catch (InvalidOperationException ex)
        {
            HandlePlaybackFailure(operation, fullPath, ex, "Audio playback operation failed");
        }
        catch (Exception ex)
        {
            HandlePlaybackFailure(operation, fullPath, ex, "Unexpected audio playback failure");
        }
    }

    private static void HandlePlaybackFailure(string operation, string fullPath, Exception ex, string summary)
    {
        Console.WriteLine($"[WARN] [AudioPlayer] {summary} during '{operation}'. Path='{fullPath}'. Error='{ex.Message}'.");

        string key = BuildFailureKey(operation, fullPath);
        bool shouldNotify = false;
        int failureCount;

        lock (FailureGate)
        {
            FailureCounts.TryGetValue(key, out failureCount);
            failureCount++;
            FailureCounts[key] = failureCount;

            if (failureCount == RepeatedFailureThreshold)
            {
                shouldNotify = true;
            }
        }

        if (!shouldNotify)
        {
            return;
        }

        string notification = $"Macrosharp detected repeated audio failures for '{operation}'.\n\nPath: {fullPath}\nLast error: {ex.Message}";

        if (RepeatedFailureNotifier is not null)
        {
            try
            {
                RepeatedFailureNotifier(notification);
            }
            catch (Exception notifierEx)
            {
                Console.WriteLine($"[WARN] [AudioPlayer] Failed to deliver repeated-failure notification. Error='{notifierEx.Message}'.");
            }
        }
        else
        {
            Console.WriteLine($"[WARN] [AudioPlayer] {notification}");
        }
    }

    private static void ResetFailureCount(string operation)
    {
        lock (FailureGate)
        {
            var matchingKeys = FailureCounts.Keys.Where(k => k.StartsWith(operation + "|", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in matchingKeys)
            {
                FailureCounts.Remove(key);
            }
        }
    }

    private static string BuildFailureKey(string operation, string fullPath)
    {
        return $"{operation}|{fullPath}";
    }

    public static void PlayStartAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("connection-sound.wav", shouldPlayAsync, 100, nameof(PlayStartAsync));
    }

    public static void PlaySuccessAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("coins-497.wav", shouldPlayAsync, 100, nameof(PlaySuccessAsync));
    }

    public static void PlayFailure(bool shouldPlayAsync = true)
    {
        PlayPreset("wrong.swf.wav", shouldPlayAsync, 100, nameof(PlayFailure));
    }

    /// <summary>Plays the "off / disable / suppress" feedback sound (no-trespassing-368.wav).</summary>
    public static void PlayOffAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("no-trespassing-368.wav", shouldPlayAsync, 100, nameof(PlayOffAsync));
    }

    /// <summary>Plays the "on / enable / resume" feedback sound (pedantic-490.wav).</summary>
    public static void PlayOnAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("pedantic-490.wav", shouldPlayAsync, 100, nameof(PlayOnAsync));
    }

    /// <summary>Plays the knob click sound (knob-458.wav), used for text-expansion confirmations.</summary>
    public static void PlayKnobAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("knob-458.wav", shouldPlayAsync, 100, nameof(PlayKnobAsync));
    }

    /// <summary>Plays the undo sound (undo.wav).</summary>
    public static void PlayUndoAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("undo.wav", shouldPlayAsync, 100, nameof(PlayUndoAsync));
    }

    /// <summary>Plays the crack-the-whip sound (crack_the_whip.wav), used for application termination.</summary>
    public static void PlayCrackTheWhipAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("crack_the_whip.wav", shouldPlayAsync, 100, nameof(PlayCrackTheWhipAsync));
    }

    /// <summary>Plays the bonk sound (bonk sound.wav), used for display-switch hotkeys.</summary>
    public static void PlayBonkAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("bonk sound.wav", shouldPlayAsync, 100, nameof(PlayBonkAsync));
    }

    public static void PlayAchievementAsync(bool shouldPlayAsync = true)
    {
        PlayPreset("achievement.wav", shouldPlayAsync, 100, nameof(PlayAchievementAsync));
    }

    public static void PlayNotificationAsync(bool shouldPlayAsync = true, int volumePercent = 100)
    {
        PlayPreset("notification.wav", shouldPlayAsync, volumePercent, nameof(PlayNotificationAsync));
    }
}
