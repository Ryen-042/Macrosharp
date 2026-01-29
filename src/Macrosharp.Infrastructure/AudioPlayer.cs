using System.Media;

namespace Macrosharp.Infrastructure;

public static class AudioPlayer
{
    public static void PlayAudio(string fileName, bool async = false)
    {
        using var player = new SoundPlayer(Path.Combine(PathLocator.RootPath, "Assets", "SFX", fileName));

        if (async)
            player.Play();
        else
            player.PlaySync();
    }

    public static void PlayStartAsync()
    {
        try
        {
            PlayAudio(Path.Combine(PathLocator.RootPath, "assets", "SFX", "connection-sound.wav"), async: true);
        }
        catch { }
    }

    public static void PlaySuccessAsync()
    {
        try
        {
            PlayAudio(Path.Combine(PathLocator.RootPath, "Assets", "SFX", "coins-497.wav"), async: true);
        }
        catch { }
    }

    public static void PlayFailure()
    {
        try
        {
            PlayAudio(Path.Combine(PathLocator.RootPath, "Assets", "SFX", "wrong.swf.wav"), async: false);
        }
        catch { }
    }
}
