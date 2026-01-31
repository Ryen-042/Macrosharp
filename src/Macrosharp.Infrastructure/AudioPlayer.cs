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
}

// TODO: add error handling/logging for audio playback failures.
