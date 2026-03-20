using System.Reflection;

namespace Macrosharp.Infrastructure;

public static class PathLocator
{
    public static Action<string, bool>? IssueNotifier { get; set; }

    public static readonly string RootPath = GetRootPath();

    private static string GetRootPath()
    {
        var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var directory = new DirectoryInfo(currentPath ?? "");
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Macrosharp.sln")))
        {
            if (directory.Parent == null)
                break;

            directory = directory.Parent;
        }

        return directory?.FullName ?? "";
    }

    public static void NotifyIssue(string message, bool isLongRunningOperation)
    {
        Console.WriteLine($"[WARN] [PathLocator] {message}");

        if (IssueNotifier is null)
        {
            return;
        }

        try
        {
            IssueNotifier(message, isLongRunningOperation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] [PathLocator] Failed to dispatch issue notification. Error='{ex.Message}'.");
        }
    }

    private static string ResolveBasePath()
    {
        if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
        {
            return RootPath;
        }

        string fallback = AppContext.BaseDirectory;
        NotifyIssue($"Root path was unavailable. Falling back to '{fallback}'.", isLongRunningOperation: false);
        return fallback;
    }

    private static bool IsUnsafeRelativeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        string trimmed = value.Trim();
        if (Path.IsPathRooted(trimmed))
            return true;

        if (trimmed.Contains("..", StringComparison.Ordinal))
            return true;

        if (trimmed.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
            return true;

        return !string.Equals(trimmed, Path.GetFileName(trimmed), StringComparison.Ordinal);
    }

    private static string ResolveSafeLeaf(string requestedName, string fallbackName, string context)
    {
        if (!IsUnsafeRelativeName(requestedName))
        {
            return requestedName.Trim();
        }

        NotifyIssue($"Invalid path leaf '{requestedName}' for {context}. Using fallback '{fallbackName}'.", isLongRunningOperation: false);
        return fallbackName;
    }

    public static string GetSfxPath(string fileName)
    {
        string safeName = ResolveSafeLeaf(fileName, "notification.wav", nameof(GetSfxPath));
        return Path.Combine(ResolveBasePath(), "Assets", "SFX", safeName);
    }

    public static List<string> GetIconFilesFromAssets()
    {
        string basePath = ResolveBasePath();
        string iconsPath = Path.GetFullPath(Path.Combine(basePath, "assets", "Icons"));
        string rootFullPath = Path.GetFullPath(basePath);

        if (!iconsPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            NotifyIssue($"Resolved icons path '{iconsPath}' escapes base path '{rootFullPath}'. Returning no icons.", isLongRunningOperation: false);
            return new List<string>();
        }

        if (!Directory.Exists(iconsPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(iconsPath, "*.ico", SearchOption.AllDirectories).ToList();
    }

    public static string GetConfigPath(string fileName)
    {
        string safeName = ResolveSafeLeaf(fileName, "macrosharp.config.json", nameof(GetConfigPath));
        return Path.Combine(ResolveBasePath(), safeName);
    }
}
