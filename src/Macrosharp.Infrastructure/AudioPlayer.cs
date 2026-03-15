using System.Media;
using System.Text;

namespace Macrosharp.Infrastructure;

public static class AudioPlayer
{
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

    public static void PlayStartAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("connection-sound.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    public static void PlaySuccessAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("coins-497.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    public static void PlayFailure(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("wrong.swf.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the "off / disable / suppress" feedback sound (no-trespassing-368.wav).</summary>
    public static void PlayOffAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("no-trespassing-368.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the "on / enable / resume" feedback sound (pedantic-490.wav).</summary>
    public static void PlayOnAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("pedantic-490.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the knob click sound (knob-458.wav), used for text-expansion confirmations.</summary>
    public static void PlayKnobAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("knob-458.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the undo sound (undo.wav).</summary>
    public static void PlayUndoAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("undo.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the crack-the-whip sound (crack_the_whip.wav), used for application termination.</summary>
    public static void PlayCrackTheWhipAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("crack_the_whip.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    /// <summary>Plays the bonk sound (bonk sound.wav), used for display-switch hotkeys.</summary>
    public static void PlayBonkAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("bonk sound.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    public static void PlayAchievementAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("achievement.wav"), async: shouldPlayAsync, volumePercent: 100);
        }
        catch { }
    }

    public static void PlayNotificationAsync(bool shouldPlayAsync = true, int volumePercent = 100)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("notification.wav"), async: shouldPlayAsync, volumePercent: volumePercent);
        }
        catch { }
    }
}

// TODO: add error handling/logging for audio playback failures.
