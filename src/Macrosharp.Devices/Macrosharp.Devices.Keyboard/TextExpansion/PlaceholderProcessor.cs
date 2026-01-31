using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;

namespace Macrosharp.Devices.Keyboard.TextExpansion;

/// <summary>Result of processing placeholders in an expansion string.</summary>
public readonly struct PlaceholderResult
{
    /// <summary>The processed text with all placeholders replaced.</summary>
    public string Text { get; init; }

    /// <summary>
    /// Number of characters from the end of the text where the cursor should be positioned.
    /// 0 means cursor stays at the end.
    /// </summary>
    public int CursorOffsetFromEnd { get; init; }

    /// <summary>Whether a $CURSOR$ placeholder was found and processed.</summary>
    public bool HasCursorPosition { get; init; }
}

/// <summary>
/// Processes placeholders in expansion text, replacing them with dynamic values.
/// </summary>
public partial class PlaceholderProcessor
{
    // Manual P/Invoke for GlobalLock since CsWin32's version returns void*
    [LibraryImport("kernel32.dll", EntryPoint = "GlobalLock", SetLastError = true)]
    private static partial nint GlobalLockManual(nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalUnlock", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlockManual(nint hMem);

    /// <summary>The placeholder marker for cursor positioning.</summary>
    public const string CursorPlaceholder = "$CURSOR$";

    private readonly Dictionary<string, Func<string>> _placeholders;
    private static readonly Regex PlaceholderPattern = new(@"\$([A-Z_]+)\$", RegexOptions.Compiled);

    /// <summary>Initializes a new instance with default placeholders.</summary>
    public PlaceholderProcessor()
    {
        _placeholders = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "DATE", () => DateTime.Now.ToString("yyyy-MM-dd") },
            { "TIME", () => DateTime.Now.ToString("HH:mm:ss") },
            { "DATETIME", () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "USER", () => Environment.UserName },
            { "CLIPBOARD", GetClipboardText },
            { "YEAR", () => DateTime.Now.Year.ToString() },
            { "MONTH", () => DateTime.Now.ToString("MMMM") },
            { "DAY", () => DateTime.Now.Day.ToString() },
            { "WEEKDAY", () => DateTime.Now.ToString("dddd") },
            { "MACHINE", () => Environment.MachineName },
        };
    }

    /// <summary>
    /// Registers a custom placeholder.
    /// </summary>
    /// <param name="name">The placeholder name (without $ delimiters).</param>
    /// <param name="valueProvider">Function that provides the replacement value.</param>
    public void RegisterPlaceholder(string name, Func<string> valueProvider)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Placeholder name cannot be empty.", nameof(name));

        _placeholders[name.ToUpperInvariant()] = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
    }

    /// <summary>
    /// Un-registers a custom placeholder.
    /// </summary>
    /// <param name="name">The placeholder name to remove.</param>
    /// <returns>True if the placeholder was removed; false if it didn't exist.</returns>
    public bool UnregisterPlaceholder(string name)
    {
        return _placeholders.Remove(name.ToUpperInvariant());
    }

    /// <summary>
    /// Processes the expansion text, replacing all placeholders with their values.
    /// </summary>
    /// <param name="expansion">The expansion text containing placeholders.</param>
    /// <returns>A <see cref="PlaceholderResult"/> with the processed text and cursor info.</returns>
    public PlaceholderResult Process(string expansion)
    {
        if (string.IsNullOrEmpty(expansion))
        {
            return new PlaceholderResult
            {
                Text = string.Empty,
                CursorOffsetFromEnd = 0,
                HasCursorPosition = false,
            };
        }

        // First, handle the cursor placeholder specially
        bool hasCursor = expansion.Contains(CursorPlaceholder, StringComparison.OrdinalIgnoreCase);
        string textWithoutCursor = expansion;
        int cursorOffset = 0;

        if (hasCursor)
        {
            // Find the first occurrence of $CURSOR$ and calculate offset
            int cursorIndex = expansion.IndexOf(CursorPlaceholder, StringComparison.OrdinalIgnoreCase);

            // Remove $CURSOR$ from the text
            textWithoutCursor = expansion.Remove(cursorIndex, CursorPlaceholder.Length);
        }

        // Now replace all other placeholders
        string processedText = PlaceholderPattern.Replace(
            textWithoutCursor,
            match =>
            {
                string placeholderName = match.Groups[1].Value;

                // Skip CURSOR placeholder as we've already handled it
                if (placeholderName.Equals("CURSOR", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                if (_placeholders.TryGetValue(placeholderName, out var provider))
                {
                    try
                    {
                        return provider();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error evaluating placeholder ${placeholderName}$: {ex.Message}");
                        return match.Value; // Return original placeholder on error
                    }
                }

                // Unknown placeholder - leave as-is
                return match.Value;
            }
        );

        // Recalculate cursor offset after all replacements
        if (hasCursor)
        {
            // Find where $CURSOR$ was in the original string, then calculate offset from end
            // We need to process the text before cursor and after cursor separately

            int originalCursorIndex = expansion.IndexOf(CursorPlaceholder, StringComparison.OrdinalIgnoreCase);
            string beforeCursor = expansion[..originalCursorIndex];
            string afterCursor = expansion[(originalCursorIndex + CursorPlaceholder.Length)..];

            // Process placeholders in both parts
            string processedBefore = PlaceholderPattern.Replace(beforeCursor, ReplacePlaceholder);
            string processedAfter = PlaceholderPattern.Replace(afterCursor, ReplacePlaceholder);

            processedText = processedBefore + processedAfter;
            cursorOffset = processedAfter.Length;
        }

        return new PlaceholderResult
        {
            Text = processedText,
            CursorOffsetFromEnd = cursorOffset,
            HasCursorPosition = hasCursor,
        };
    }

    private string ReplacePlaceholder(Match match)
    {
        string placeholderName = match.Groups[1].Value;

        if (placeholderName.Equals("CURSOR", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (_placeholders.TryGetValue(placeholderName, out var provider))
        {
            try
            {
                return provider();
            }
            catch
            {
                return match.Value;
            }
        }

        return match.Value;
    }

    /// <summary>Gets the current clipboard text, or empty string if unavailable.</summary>
    private static string GetClipboardText()
    {
        try
        {
            // Use Win32 clipboard APIs for thread safety
            if (!PInvoke.OpenClipboard(default))
                return string.Empty;

            try
            {
                if (!PInvoke.IsClipboardFormatAvailable(13)) // CF_UNICODETEXT
                    return string.Empty;

                nint hData = PInvoke.GetClipboardData(13); // CF_UNICODETEXT
                if (hData == nint.Zero)
                    return string.Empty;

                nint pData = GlobalLockManual(hData);
                if (pData == nint.Zero)
                    return string.Empty;

                try
                {
                    return Marshal.PtrToStringUni(pData) ?? string.Empty;
                }
                finally
                {
                    GlobalUnlockManual(hData);
                }
            }
            finally
            {
                PInvoke.CloseClipboard();
            }
        }
        catch
        {
            return string.Empty;
        }
    }
}
