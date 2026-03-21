namespace Macrosharp.Hosts.ConsoleHost;

internal static class ProgramStartupBanner
{
    public static void Write()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║           Macrosharp — Ready                    ║");
        Console.WriteLine("║  Win+Esc        : Quit                          ║");
        Console.WriteLine("║  Win+?          : Show running notification     ║");
        Console.WriteLine("║  Ctrl+Win+/     : Show hotkeys window           ║");
        Console.WriteLine("║  Win+CapsLock   : Toggle Scroll Lock            ║");
        Console.WriteLine("║  Ctrl+Alt+T     : Toggle text expansion         ║");
        Console.WriteLine("║  Ctrl+Alt+Win+P : Pause/resume event handling   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
}
