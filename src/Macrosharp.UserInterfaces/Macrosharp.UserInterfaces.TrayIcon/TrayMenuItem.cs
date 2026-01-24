using System;
using System.Collections.Generic;

namespace Macrosharp.UserInterfaces.TrayIcon;

public sealed class TrayMenuItem
{
    public string Text { get; }
    public string? IconPath { get; }
    public Action? Action { get; }
    public IReadOnlyList<TrayMenuItem>? Children { get; }
    public bool IsSeparator { get; }

    private TrayMenuItem(string text, string? iconPath, Action? action, IReadOnlyList<TrayMenuItem>? children, bool isSeparator)
    {
        Text = text;
        IconPath = iconPath;
        Action = action;
        Children = children;
        IsSeparator = isSeparator;
    }

    public static TrayMenuItem ActionItem(string text, Action action, string? iconPath = null)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return new TrayMenuItem(text, iconPath, action, null, isSeparator: false);
    }

    public static TrayMenuItem Submenu(string text, IReadOnlyList<TrayMenuItem> children, string? iconPath = null)
    {
        return new TrayMenuItem(text, iconPath, action: null, children, isSeparator: false);
    }

    public static TrayMenuItem Separator()
    {
        return new TrayMenuItem(string.Empty, iconPath: null, action: null, children: null, isSeparator: true);
    }
}
