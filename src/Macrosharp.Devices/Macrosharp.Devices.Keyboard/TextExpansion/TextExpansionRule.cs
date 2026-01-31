namespace Macrosharp.Devices.Keyboard.TextExpansion;

/// <summary>Specifies when a text expansion trigger should activate.</summary>
public enum TriggerMode
{
    /// <summary>Expand immediately after the last character of the trigger is typed.</summary>
    Immediate,

    /// <summary>Expand only when a delimiter (space, tab, punctuation) follows the trigger.</summary>
    OnDelimiter,
}

/// <summary>Represents a single text expansion rule mapping a trigger to an expansion.</summary>
public class TextExpansionRule
{
    /// <summary>The abbreviation or keyword that triggers expansion (e.g., ":sig", "btw").</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// The text to expand to. Supports placeholders:
    /// <list type="bullet">
    ///   <item><c>$CURSOR$</c> - Position caret here after expansion</item>
    ///   <item><c>$DATE$</c> - Current date (yyyy-MM-dd)</item>
    ///   <item><c>$TIME$</c> - Current time (HH:mm:ss)</item>
    ///   <item><c>$DATETIME$</c> - Current date and time</item>
    ///   <item><c>$USER$</c> - Current Windows username</item>
    ///   <item><c>$CLIPBOARD$</c> - Current clipboard text</item>
    /// </list>
    /// </summary>
    public string Expansion { get; set; } = string.Empty;

    /// <summary>Specifies when the trigger should activate. Default is <see cref="TriggerMode.OnDelimiter"/>.</summary>
    public TriggerMode Mode { get; set; } = TriggerMode.OnDelimiter;

    /// <summary>Whether the trigger match is case-sensitive. Default is false.</summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>Whether this rule is active. Default is true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Returns a string representation of the rule.</summary>
    public override string ToString()
    {
        return $"[{(Enabled ? "ON" : "OFF")}] '{Trigger}' → '{(Expansion.Length > 30 ? Expansion[..30] + "..." : Expansion)}' ({Mode})";
    }
}

/// <summary>Root configuration object for text expansion settings.</summary>
public class TextExpansionConfiguration
{
    /// <summary>List of text expansion rules.</summary>
    public List<TextExpansionRule> Rules { get; set; } = new();

    /// <summary>Global settings for text expansion behavior.</summary>
    public TextExpansionSettings Settings { get; set; } = new();
}

/// <summary>Global settings for the text expansion system.</summary>
public class TextExpansionSettings
{
    /// <summary>Characters that trigger expansion in <see cref="TriggerMode.OnDelimiter"/> mode.</summary>
    public List<string> Delimiters { get; set; } = new() { " ", "\t", "\n", ".", ",", ";", ":", "!", "?" };

    /// <summary>Maximum number of characters to buffer for trigger detection.</summary>
    public int BufferSize { get; set; } = 64;

    /// <summary>Delay in milliseconds between each backspace when deleting the trigger.</summary>
    public int BackspaceDelayMs { get; set; } = 2;

    /// <summary>Delay in milliseconds after pasting before moving cursor.</summary>
    public int PasteDelayMs { get; set; } = 30;

    /// <summary>Whether text expansion is globally enabled.</summary>
    public bool Enabled { get; set; } = true;
}
