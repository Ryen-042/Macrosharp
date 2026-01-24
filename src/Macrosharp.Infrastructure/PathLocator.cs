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
}
