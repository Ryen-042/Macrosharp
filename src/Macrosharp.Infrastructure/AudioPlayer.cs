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
}
