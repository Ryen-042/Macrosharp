using System.Media;

namespace Macrosharp.Infrastructure;

public static class AudioPlayer
{
    public static void PlayAudio(string fileName, bool async = false)
    {
        using var player = new SoundPlayer(fileName);

        if (async)
            player.Play();
        else
            player.PlaySync();
    }

    public static void PlayStartAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("connection-sound.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    public static void PlaySuccessAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("coins-497.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    public static void PlayFailure(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("wrong.swf.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the "off / disable / suppress" feedback sound (no-trespassing-368.wav).</summary>
    public static void PlayOffAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("no-trespassing-368.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the "on / enable / resume" feedback sound (pedantic-490.wav).</summary>
    public static void PlayOnAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("pedantic-490.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the knob click sound (knob-458.wav), used for text-expansion confirmations.</summary>
    public static void PlayKnobAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("knob-458.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the undo sound (undo.wav).</summary>
    public static void PlayUndoAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("undo.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the crack-the-whip sound (crack_the_whip.wav), used for application termination.</summary>
    public static void PlayCrackTheWhipAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("crack_the_whip.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    /// <summary>Plays the bonk sound (bonk sound.wav), used for display-switch hotkeys.</summary>
    public static void PlayBonkAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("bonk sound.wav"), async: shouldPlayAsync);
        }
        catch { }
    }

    public static void PlayAchievementAsync(bool shouldPlayAsync = true)
    {
        try
        {
            PlayAudio(PathLocator.GetSfxPath("achievement.wav"), async: shouldPlayAsync);
        }
        catch { }
    }
}

// TODO: add error handling/logging for audio playback failures.
