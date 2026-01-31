using System.Text;
using Windows.Win32;

namespace Macrosharp.Devices.Keyboard.TextExpansion;

/// <summary>
/// A rolling character buffer that tracks recently typed characters for trigger detection.
/// Automatically clears when the foreground window changes.
/// </summary>
public class TextExpansionBuffer
{
    private readonly StringBuilder _buffer;
    private readonly int _maxSize;
    private nint _lastForegroundWindow;
    private readonly object _lock = new();

    /// <summary>Gets the current buffer contents.</summary>
    public string Content
    {
        get
        {
            lock (_lock)
            {
                return _buffer.ToString();
            }
        }
    }

    /// <summary>Gets the current buffer length.</summary>
    public int Length
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Length;
            }
        }
    }

    /// <summary>Initializes a new instance of the <see cref="TextExpansionBuffer"/> class.</summary>
    /// <param name="maxSize">Maximum number of characters to buffer.</param>
    public TextExpansionBuffer(int maxSize = 64)
    {
        _maxSize = maxSize;
        _buffer = new StringBuilder(maxSize);
        _lastForegroundWindow = nint.Zero;
    }

    /// <summary>
    /// Appends a character to the buffer. Clears the buffer if the foreground window has changed.
    /// </summary>
    /// <param name="c">The character to append.</param>
    /// <returns>True if the character was appended; false if the buffer was cleared due to window change.</returns>
    public bool Append(char c)
    {
        lock (_lock)
        {
            // Check if foreground window changed
            nint currentWindow = PInvoke.GetForegroundWindow();
            if (currentWindow != _lastForegroundWindow)
            {
                _buffer.Clear();
                _lastForegroundWindow = currentWindow;
            }

            // Trim from start if buffer exceeds max size
            if (_buffer.Length >= _maxSize)
            {
                _buffer.Remove(0, _buffer.Length - _maxSize + 1);
            }

            _buffer.Append(c);
            return true;
        }
    }

    /// <summary>
    /// Removes the last N characters from the buffer (simulates backspace).
    /// </summary>
    /// <param name="count">Number of characters to remove.</param>
    public void RemoveLast(int count = 1)
    {
        lock (_lock)
        {
            int toRemove = Math.Min(count, _buffer.Length);
            if (toRemove > 0)
            {
                _buffer.Remove(_buffer.Length - toRemove, toRemove);
            }
        }
    }

    /// <summary>Clears the buffer.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }

    /// <summary>
    /// Checks if the buffer ends with the specified trigger string.
    /// </summary>
    /// <param name="trigger">The trigger string to check for.</param>
    /// <param name="caseSensitive">Whether the comparison is case-sensitive.</param>
    /// <returns>True if the buffer ends with the trigger; otherwise, false.</returns>
    public bool EndsWithTrigger(string trigger, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(trigger))
            return false;

        lock (_lock)
        {
            if (_buffer.Length < trigger.Length)
                return false;

            int startIndex = _buffer.Length - trigger.Length;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Extract the relevant portion and compare
            string bufferEnd = _buffer.ToString(startIndex, trigger.Length);
            return bufferEnd.Equals(trigger, comparison);
        }
    }

    /// <summary>
    /// Checks if the buffer (excluding the last character) ends with the specified trigger.
    /// Used for OnDelimiter mode where the last character is the delimiter.
    /// </summary>
    /// <param name="trigger">The trigger string to check for.</param>
    /// <param name="caseSensitive">Whether the comparison is case-sensitive.</param>
    /// <returns>True if the buffer (minus last char) ends with the trigger; otherwise, false.</returns>
    public bool EndsWithTriggerBeforeLastChar(string trigger, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(trigger))
            return false;

        lock (_lock)
        {
            // Need at least trigger length + 1 (for the delimiter)
            if (_buffer.Length < trigger.Length + 1)
                return false;

            int startIndex = _buffer.Length - trigger.Length - 1;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Extract the portion before the last character
            string bufferPortion = _buffer.ToString(startIndex, trigger.Length);
            return bufferPortion.Equals(trigger, comparison);
        }
    }

    /// <summary>Gets the last character in the buffer, or null if empty.</summary>
    public char? LastChar
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Length > 0 ? _buffer[_buffer.Length - 1] : null;
            }
        }
    }

    /// <summary>Updates the tracked foreground window. Call this to force a window context update.</summary>
    public void UpdateForegroundWindow()
    {
        lock (_lock)
        {
            _lastForegroundWindow = PInvoke.GetForegroundWindow();
        }
    }
}
