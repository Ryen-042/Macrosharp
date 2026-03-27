namespace Macrosharp.Runtime.Core;

public static class ProgramStartupBanner
{
    public static void Write()
    {
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine("|                Macrosharp - Ready                       |");
        Console.WriteLine("|  Program Management & Related Shortcuts                 |");
        Console.WriteLine("|  Win+Esc            : Confirm quit                      |");
        Console.WriteLine("|  Alt+Win+Esc        : Quit immediately                  |");
        Console.WriteLine("|  Ctrl+Win+/         : Show hotkeys window              |");
        Console.WriteLine("|  Ctrl+Alt+Win+/     : Show text expansions window      |");
        Console.WriteLine("|  Shift+Win+/        : Show running notification         |");
        Console.WriteLine("|  Shift+Win+Delete   : Clear console                     |");
        Console.WriteLine("|  Shift+Win+Insert   : Toggle console visibility         |");
        Console.WriteLine("|  Ctrl+Alt+Win+P     : Pause/resume event handling       |");
        Console.WriteLine("|  Ctrl+Alt+Win+B     : Toggle burst click                |");
        Console.WriteLine("|  Ctrl+Alt+T         : Toggle text expansion             |");
        Console.WriteLine("|  Win+CapsLock       : Toggle Scroll Lock                |");
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine();
    }
}
