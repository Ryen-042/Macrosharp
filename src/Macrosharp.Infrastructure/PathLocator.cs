using System.Reflection;

namespace Macrosharp.Infrastructure;

public static class PathLocator
{
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

    public static string GetSfxPath(string fileName)
    {
        return Path.Combine(RootPath, "Assets", "SFX", fileName);
    }

    public static List<string> GetIconFilesFromAssets()
    {
        string iconsPath = Path.GetFullPath(Path.Combine(RootPath, "assets", "Icons"));

        if (!Directory.Exists(iconsPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(iconsPath, "*.ico", SearchOption.AllDirectories).ToList();
    }

    public static string GetConfigPath(string fileName)
    {
        return Path.Combine(RootPath, fileName);
    }
}
