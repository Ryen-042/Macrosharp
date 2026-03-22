using Macrosharp.Devices.Core;

namespace Macrosharp.Devices.Keyboard.TextExpansion;

/// <summary>
/// Manages text expansion by monitoring keyboard input, detecting triggers, and expanding them.
/// </summary>
public class TextExpansionManager : IDisposable
{
    private readonly record struct IndexedRule(TextExpansionRule Rule, int Order);

    private readonly record struct PendingExpansion(TextExpansionRule Rule, bool IsDelimiterMode, char? DelimiterChar);

    private readonly KeyboardHookManager _keyboardHookManager;
    private readonly TextExpansionBuffer _buffer;
    private readonly PlaceholderProcessor _placeholderProcessor;

    private List<TextExpansionRule> _rules;
    private Dictionary<char, List<IndexedRule>> _immediateInsensitiveIndex;
    private Dictionary<char, List<IndexedRule>> _immediateSensitiveIndex;
    private Dictionary<char, List<IndexedRule>> _delimiterInsensitiveIndex;
    private Dictionary<char, List<IndexedRule>> _delimiterSensitiveIndex;
    private TextExpansionSettings _settings;
    private HashSet<char> _delimiters;
    private readonly Queue<PendingExpansion> _pendingExpansions;
    private int _maxQueuedExpansions;

    private bool _isExpanding = false;
    private bool _isEnabled = true;
    private readonly object _lock = new();

    // Cache the longest trigger length for buffer optimization
    private int _maxTriggerLength;

    /// <summary>Gets or sets whether text expansion is enabled.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (!value)
            {
                _buffer.Clear();
            }
        }
    }

    /// <summary>Event raised when an expansion occurs.</summary>
    public event EventHandler<TextExpansionEventArgs>? ExpansionOccurred;

    /// <summary>Event raised when an expansion error occurs.</summary>
    public event EventHandler<TextExpansionErrorEventArgs>? ExpansionError;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextExpansionManager"/> class.
    /// </summary>
    /// <param name="keyboardHookManager">The keyboard hook manager to subscribe to.</param>
    public TextExpansionManager(KeyboardHookManager keyboardHookManager)
    {
        _keyboardHookManager = keyboardHookManager ?? throw new ArgumentNullException(nameof(keyboardHookManager));
        _buffer = new TextExpansionBuffer();
        _placeholderProcessor = new PlaceholderProcessor();
        _rules = new List<TextExpansionRule>();
        _immediateInsensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _immediateSensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _delimiterInsensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _delimiterSensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _settings = new TextExpansionSettings();
        _delimiters = new HashSet<char>(_settings.Delimiters.Select(d => d.Length > 0 ? d[0] : ' '));
        _pendingExpansions = new Queue<PendingExpansion>();
        _maxQueuedExpansions = 0;
        _maxTriggerLength = 0;

        _keyboardHookManager.KeyDownHandler += OnKeyDown;
    }

    /// <summary>
    /// Loads text expansion rules and settings.
    /// </summary>
    /// <param name="configuration">The configuration to load.</param>
    public void LoadConfiguration(TextExpansionConfiguration configuration)
    {
        lock (_lock)
        {
            _rules = configuration.Rules.Where(r => r.Enabled).ToList();
            _settings = configuration.Settings;
            _isEnabled = _settings.Enabled;
            _maxQueuedExpansions = _settings.MaxQueuedExpansions;
            _pendingExpansions.Clear();
            RebuildRuleIndex();

            // Update delimiters set
            _delimiters = new HashSet<char>(_settings.Delimiters.Select(d => d.Length > 0 ? d[0] : ' '));

            // Calculate max trigger length for buffer sizing
            _maxTriggerLength = _rules.Count > 0 ? _rules.Max(r => r.Trigger.Length) : 0;

            // Update buffer size
            _buffer.Clear();

            Console.WriteLine($"TextExpansionManager: Loaded {_rules.Count} rules. Enabled: {_isEnabled}. MaxQueuedExpansions: {_maxQueuedExpansions}");
        }
    }

    /// <summary>
    /// Registers a custom placeholder.
    /// </summary>
    /// <param name="name">The placeholder name (without $ delimiters).</param>
    /// <param name="valueProvider">Function that provides the replacement value.</param>
    public void RegisterPlaceholder(string name, Func<string> valueProvider)
    {
        _placeholderProcessor.RegisterPlaceholder(name, valueProvider);
    }

    /// <summary>Handles key down events from the keyboard hook.</summary>
    private void OnKeyDown(object? sender, KeyboardEvent e)
    {
        // Skip if disabled or already handled
        if (!_isEnabled || e.Handled)
            return;

        // Ignore injected events (from our own typing)
        if (e.IsInjected)
            return;

        lock (_lock)
        {
            // Handle special keys
            switch (e.KeyCode)
            {
                case VirtualKey.BACK:
                    _buffer.RemoveLast();
                    return;

                case VirtualKey.ESCAPE:
                case VirtualKey.RETURN:
                case VirtualKey.TAB:
                    // For RETURN and TAB, check if they're delimiters first
                    if (e.KeyCode == VirtualKey.RETURN || e.KeyCode == VirtualKey.TAB)
                    {
                        char delimChar = e.KeyCode == VirtualKey.RETURN ? '\n' : '\t';
                        if (_delimiters.Contains(delimChar))
                        {
                            // Append delimiter and check for trigger
                            _buffer.Append(delimChar);
                            if (TryExpandOnDelimiter(delimChar, e))
                                return;
                        }
                    }
                    _buffer.Clear();
                    return;

                // Skip modifier keys
                case VirtualKey.CONTROL:
                case VirtualKey.LCONTROL:
                case VirtualKey.RCONTROL:
                case VirtualKey.SHIFT:
                case VirtualKey.LSHIFT:
                case VirtualKey.RSHIFT:
                case VirtualKey.MENU:
                case VirtualKey.LMENU:
                case VirtualKey.RMENU:
                case VirtualKey.LWIN:
                case VirtualKey.RWIN:
                    return;

                // Skip function keys and navigation keys
                case VirtualKey key when key >= VirtualKey.F1 && key <= VirtualKey.F24:
                case VirtualKey.HOME:
                case VirtualKey.END:
                case VirtualKey.PRIOR: // Page Up
                case VirtualKey.NEXT: // Page Down
                case VirtualKey.INSERT:
                case VirtualKey.DELETE:
                case VirtualKey.LEFT:
                case VirtualKey.RIGHT:
                case VirtualKey.UP:
                case VirtualKey.DOWN:
                    _buffer.Clear();
                    return;
            }

            // Skip if any modifier other than Shift is held (Shift is okay for uppercase)
            if (Modifiers.HasModifier(Modifiers.CTRL) || Modifiers.HasModifier(Modifiers.ALT) || Modifiers.HasModifier(Modifiers.WIN))
            {
                _buffer.Clear();
                return;
            }

            // Convert key to character
            char? typedChar = GetCharFromKeyEvent(e);
            if (typedChar == null)
                return;

            char c = typedChar.Value;

            // Append to buffer
            _buffer.Append(c);

            // Check for immediate mode triggers
            var immediateTrigger = FindMatchingRule(TriggerMode.Immediate);
            if (immediateTrigger != null)
            {
                PerformExpansion(immediateTrigger, e, isDelimiterMode: false, delimiterChar: null);
                return;
            }

            // Check for delimiter mode triggers
            if (_delimiters.Contains(c))
            {
                TryExpandOnDelimiter(c, e);
            }
        }
    }

    /// <summary>Tries to expand when a delimiter character is typed.</summary>
    private bool TryExpandOnDelimiter(char delimiter, KeyboardEvent e)
    {
        var delimiterTrigger = FindMatchingRule(TriggerMode.OnDelimiter);
        if (delimiterTrigger != null)
        {
            PerformExpansion(delimiterTrigger, e, isDelimiterMode: true, delimiterChar: delimiter);
            return true;
        }
        return false;
    }

    /// <summary>Finds a matching rule for the current buffer state.</summary>
    private TextExpansionRule? FindMatchingRule(TriggerMode mode)
    {
        char? terminalChar = mode == TriggerMode.Immediate ? _buffer.LastChar : _buffer.CharFromEnd(1);
        if (terminalChar is null)
        {
            return null;
        }

        var orderedCandidates = GetOrderedCandidates(mode, terminalChar.Value);
        foreach (var indexed in orderedCandidates)
        {
            var rule = indexed.Rule;
            bool matches = mode == TriggerMode.Immediate ? _buffer.EndsWithTrigger(rule.Trigger, rule.CaseSensitive) : _buffer.EndsWithTriggerBeforeLastChar(rule.Trigger, rule.CaseSensitive);

            if (matches)
                return rule;
        }

        return null;
    }

    private List<IndexedRule> GetOrderedCandidates(TriggerMode mode, char terminalChar)
    {
        var sensitiveIndex = mode == TriggerMode.Immediate ? _immediateSensitiveIndex : _delimiterSensitiveIndex;
        var insensitiveIndex = mode == TriggerMode.Immediate ? _immediateInsensitiveIndex : _delimiterInsensitiveIndex;

        sensitiveIndex.TryGetValue(terminalChar, out var sensitiveCandidates);
        insensitiveIndex.TryGetValue(char.ToLowerInvariant(terminalChar), out var insensitiveCandidates);

        if (sensitiveCandidates is null && insensitiveCandidates is null)
        {
            return new List<IndexedRule>();
        }

        if (sensitiveCandidates is null)
        {
            return insensitiveCandidates!;
        }

        if (insensitiveCandidates is null)
        {
            return sensitiveCandidates;
        }

        var merged = new List<IndexedRule>(sensitiveCandidates.Count + insensitiveCandidates.Count);
        merged.AddRange(sensitiveCandidates);
        merged.AddRange(insensitiveCandidates);
        merged.Sort((a, b) => a.Order.CompareTo(b.Order));
        return merged;
    }

    private void RebuildRuleIndex()
    {
        _immediateInsensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _immediateSensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _delimiterInsensitiveIndex = new Dictionary<char, List<IndexedRule>>();
        _delimiterSensitiveIndex = new Dictionary<char, List<IndexedRule>>();

        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (string.IsNullOrEmpty(rule.Trigger))
            {
                continue;
            }

            char terminalChar = rule.Trigger[^1];
            var indexedRule = new IndexedRule(rule, i);
            bool isImmediate = rule.Mode == TriggerMode.Immediate;

            if (rule.CaseSensitive)
            {
                AddToIndex(isImmediate ? _immediateSensitiveIndex : _delimiterSensitiveIndex, terminalChar, indexedRule);
            }
            else
            {
                AddToIndex(isImmediate ? _immediateInsensitiveIndex : _delimiterInsensitiveIndex, char.ToLowerInvariant(terminalChar), indexedRule);
            }
        }
    }

    private static void AddToIndex(Dictionary<char, List<IndexedRule>> index, char key, IndexedRule rule)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<IndexedRule>();
            index[key] = list;
        }

        list.Add(rule);
    }

    /// <summary>Performs the text expansion.</summary>
    private void PerformExpansion(TextExpansionRule rule, KeyboardEvent e, bool isDelimiterMode, char? delimiterChar)
    {
        PendingExpansion request = new(rule, isDelimiterMode, delimiterChar);
        bool shouldStartNow;

        lock (_lock)
        {
            if (!_isExpanding)
            {
                _isExpanding = true;
                shouldStartNow = true;
            }
            else
            {
                shouldStartNow = false;

                if (_maxQueuedExpansions <= 0)
                {
                    return;
                }

                if (_pendingExpansions.Count >= _maxQueuedExpansions)
                {
                    return;
                }

                _pendingExpansions.Enqueue(request);
                Console.WriteLine($"TextExpansion: queued pending expansion ({_pendingExpansions.Count}/{_maxQueuedExpansions}).");
            }

            e.Handled = true;
            _buffer.Clear();
        }

        if (shouldStartNow)
        {
            StartExpansionWorker(request);
        }
    }

    private void StartExpansionWorker(PendingExpansion request)
    {
        Task.Run(() =>
        {
            var rule = request.Rule;

            try
            {
                Thread.Sleep(10);

                PlaceholderResult result = _placeholderProcessor.Process(rule.Expansion);
                string expansionText = result.Text;

                int backspaceCount = request.IsDelimiterMode ? rule.Trigger.Length : rule.Trigger.Length - 1;
                if (backspaceCount > 0)
                {
                    KeyboardSimulator.SendBackspaces(backspaceCount, _settings.BackspaceDelayMs);
                }

                KeyboardSimulator.PasteText(expansionText, _settings.PasteDelayMs);

                if (result.HasCursorPosition && result.CursorOffsetFromEnd > 0)
                {
                    KeyboardSimulator.MoveCursorLeft(result.CursorOffsetFromEnd, _settings.BackspaceDelayMs);
                }

                ExpansionOccurred?.Invoke(this, new TextExpansionEventArgs(rule, expansionText));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TextExpansion error: {ex.Message}");
                ExpansionError?.Invoke(this, new TextExpansionErrorEventArgs(rule, ex));
            }
            finally
            {
                CompleteExpansionWorker();
            }
        });
    }

    private void CompleteExpansionWorker()
    {
        PendingExpansion? next = null;

        lock (_lock)
        {
            if (_pendingExpansions.Count > 0)
            {
                next = _pendingExpansions.Dequeue();
            }
            else
            {
                _isExpanding = false;
            }
        }

        if (next.HasValue)
        {
            StartExpansionWorker(next.Value);
        }
    }

    /// <summary>Converts a keyboard event to its corresponding character.</summary>
    private static char? GetCharFromKeyEvent(KeyboardEvent e)
    {
        bool isShiftDown = e.IsShiftDown || Modifiers.HasModifier(Modifiers.SHIFT);
        bool isCapsLock = Modifiers.IsCapsLockOn;

        // Get the display name which gives us the character
        string? displayName = KeysMapper.GetDisplayName(e.KeyCode, isShiftDown, isCapsLock);

        // If it's a single character, return it
        if (!string.IsNullOrEmpty(displayName) && displayName.Length == 1)
        {
            return displayName[0];
        }

        // Handle special cases for common keys
        return e.KeyCode switch
        {
            VirtualKey.SPACE => ' ',
            VirtualKey.OEM_PERIOD => isShiftDown ? '>' : '.',
            VirtualKey.OEM_COMMA => isShiftDown ? '<' : ',',
            VirtualKey.OEM_MINUS => isShiftDown ? '_' : '-',
            VirtualKey.OEM_PLUS => isShiftDown ? '+' : '=',
            VirtualKey.OEM_1 => isShiftDown ? ':' : ';',
            VirtualKey.OEM_2 => isShiftDown ? '?' : '/',
            VirtualKey.OEM_3 => isShiftDown ? '~' : '`',
            VirtualKey.OEM_4 => isShiftDown ? '{' : '[',
            VirtualKey.OEM_5 => isShiftDown ? '|' : '\\',
            VirtualKey.OEM_6 => isShiftDown ? '}' : ']',
            VirtualKey.OEM_7 => isShiftDown ? '"' : '\'',
            _ => null,
        };
    }

    /// <summary>Disposes of the TextExpansionManager.</summary>
    public void Dispose()
    {
        _keyboardHookManager.KeyDownHandler -= OnKeyDown;
        _buffer.Clear();
        Console.WriteLine("TextExpansionManager disposed.");
    }
}

/// <summary>Event arguments for when an expansion occurs.</summary>
public class TextExpansionEventArgs : EventArgs
{
    /// <summary>The rule that was triggered.</summary>
    public TextExpansionRule Rule { get; }

    /// <summary>The expanded text (after placeholder processing).</summary>
    public string ExpandedText { get; }

    public TextExpansionEventArgs(TextExpansionRule rule, string expandedText)
    {
        Rule = rule;
        ExpandedText = expandedText;
    }
}

/// <summary>Event arguments for expansion errors.</summary>
public class TextExpansionErrorEventArgs : EventArgs
{
    /// <summary>The rule that was being expanded.</summary>
    public TextExpansionRule Rule { get; }

    /// <summary>The exception that occurred.</summary>
    public Exception Exception { get; }

    public TextExpansionErrorEventArgs(TextExpansionRule rule, Exception exception)
    {
        Rule = rule;
        Exception = exception;
    }
}
