namespace Macrosharp.UserInterfaces.TrayIcon;

/// <summary>
/// Cycles through a collection of icon file paths in a round-robin fashion.
/// </summary>
public sealed class IconCycler
{
    private readonly IReadOnlyList<string> iconPaths;
    private int currentIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconCycler"/> class.
    /// </summary>
    /// <param name="iconPaths">The collection of icon file paths to cycle through.</param>
    public IconCycler(IReadOnlyList<string> iconPaths)
    {
        this.iconPaths = iconPaths ?? Array.Empty<string>();
        currentIndex = -1;
    }

    /// <summary>
    /// Gets a value indicating whether the cycler has any icons to cycle through.
    /// </summary>
    public bool HasIcons => iconPaths.Count > 0;

    /// <summary>
    /// Gets the number of icons available for cycling.
    /// </summary>
    public int Count => iconPaths.Count;

    /// <summary>
    /// Gets the current icon path without advancing to the next one.
    /// Returns null if no icons are available.
    /// </summary>
    public string? Current => currentIndex >= 0 && currentIndex < iconPaths.Count ? iconPaths[currentIndex] : null;

    /// <summary>
    /// Advances to the next icon and returns its path.
    /// Wraps around to the first icon after reaching the end.
    /// </summary>
    /// <returns>The next icon path, or null if no icons are available.</returns>
    public string? GetNext()
    {
        if (iconPaths.Count == 0)
        {
            return null;
        }

        currentIndex = (currentIndex + 1) % iconPaths.Count;
        return iconPaths[currentIndex];
    }

    /// <summary>
    /// Resets the cycler to the beginning, so the next call to <see cref="GetNext"/>
    /// returns the first icon.
    /// </summary>
    public void Reset()
    {
        currentIndex = -1;
    }

    /// <summary>
    /// Creates an <see cref="IconCycler"/> from the specified icon paths.
    /// Returns an empty cycler if the collection is null or empty.
    /// </summary>
    /// <param name="iconPaths">The collection of icon file paths.</param>
    /// <returns>A new <see cref="IconCycler"/> instance.</returns>
    public static IconCycler Create(IReadOnlyList<string>? iconPaths)
    {
        return new IconCycler(iconPaths ?? Array.Empty<string>());
    }
}
