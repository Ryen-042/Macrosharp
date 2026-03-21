using System.Text.Json;
using System.Text.Json.Serialization;
using Macrosharp.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Macrosharp.Devices.Keyboard.TextExpansion;

/// <summary>
/// Manages loading, saving, and monitoring changes to the text expansion configuration file.
/// </summary>
public class TextExpansionConfigurationManager : IDisposable
{
    private readonly string _configFilePath;
    private readonly DebouncedFileWatcher _configWatcher;
    private TextExpansionConfiguration _currentConfiguration;
    private readonly object _fileLock = new();
    private int _backupCounter = 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Event raised when the configuration file changes and is reloaded.</summary>
    public event EventHandler<TextExpansionConfiguration>? ConfigurationChanged;

    /// <summary>Gets the current configuration.</summary>
    public TextExpansionConfiguration CurrentConfiguration => _currentConfiguration;

    public string ConfigPath => _configFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextExpansionConfigurationManager"/> class.
    /// </summary>
    /// <param name="configFilePath">The full path to the text expansion configuration JSON file.</param>
    public TextExpansionConfigurationManager(string configFilePath, bool watchForChanges = false)
    {
        _configFilePath = configFilePath;
        _currentConfiguration = new TextExpansionConfiguration();
        _configWatcher = new DebouncedFileWatcher(_configFilePath, () => _ = LoadConfiguration(), watchForChanges, nameof(TextExpansionConfigurationManager), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Loads the text expansion configuration from the file.
    /// Creates a default configuration if the file doesn't exist.
    /// </summary>
    /// <returns>The loaded configuration.</returns>
    public TextExpansionConfiguration LoadConfiguration()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_configFilePath))
            {
                Console.WriteLine($"TextExpansion: Configuration file not found: {_configFilePath}. Creating default.");
                _currentConfiguration = CreateDefaultConfiguration();
                SaveConfigurationInternal(_currentConfiguration);
                ConfigurationChanged?.Invoke(this, _currentConfiguration);
                return _currentConfiguration;
            }

            TextExpansionConfiguration? loadedConfig = null;
            string? jsonString = null;

            try
            {
                jsonString = File.ReadAllText(_configFilePath);
                loadedConfig = JsonSerializer.Deserialize<TextExpansionConfiguration>(jsonString, JsonOptions);

                if (loadedConfig == null)
                {
                    throw new JsonException("Deserialization resulted in a null configuration.");
                }

                Normalize(loadedConfig);

                _currentConfiguration = loadedConfig;
                Console.WriteLine($"TextExpansion: Configuration loaded. Rules: {_currentConfiguration.Rules.Count}");
                ConfigurationChanged?.Invoke(this, _currentConfiguration);
                return _currentConfiguration;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"TextExpansion: Error deserializing configuration: {ex.Message}");
                HandleInvalidConfigFile(jsonString, ex.Message);
                return _currentConfiguration;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"TextExpansion: IO Error reading configuration: {ex.Message}");
                return _currentConfiguration;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TextExpansion: Unexpected error loading configuration: {ex.Message}");
                HandleInvalidConfigFile(jsonString, ex.Message);
                return _currentConfiguration;
            }
        }
    }

    /// <summary>Creates a default configuration with example rules.</summary>
    private static TextExpansionConfiguration CreateDefaultConfiguration()
    {
        return new TextExpansionConfiguration
        {
            Rules = new List<TextExpansionRule>
            {
                new()
                {
                    Trigger = ":sig",
                    Expansion = "Best regards,\nJohn Doe",
                    Mode = TriggerMode.OnDelimiter,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = ":date",
                    Expansion = "$DATE$",
                    Mode = TriggerMode.Immediate,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = ":time",
                    Expansion = "$TIME$",
                    Mode = TriggerMode.Immediate,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = ":email",
                    Expansion = "Hello $CLIPBOARD$,\n\n$CURSOR$\n\nBest regards,\n$USER$",
                    Mode = TriggerMode.OnDelimiter,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = ":shrug",
                    Expansion = "¯\\_(ツ)_/¯",
                    Mode = TriggerMode.Immediate,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = ":lod",
                    Expansion = "( ͡° ͜ʖ ͡°)",
                    Mode = TriggerMode.Immediate,
                    CaseSensitive = false,
                    Enabled = true,
                },
                new()
                {
                    Trigger = "btw",
                    Expansion = "by the way",
                    Mode = TriggerMode.OnDelimiter,
                    CaseSensitive = false,
                    Enabled = false,
                },
            },
            Settings = new TextExpansionSettings
            {
                Delimiters = new List<string> { " ", "\t", "\n", ".", ",", ";", ":", "!", "?" },
                BufferSize = 64,
                BackspaceDelayMs = 2,
                PasteDelayMs = 30,
                Enabled = true,
            },
        };
    }

    /// <summary>Handles an invalid configuration file by creating a backup and reverting.</summary>
    private void HandleInvalidConfigFile(string? invalidContent, string errorMessage)
    {
        string backupFileName = $"{Path.GetFileNameWithoutExtension(_configFilePath)}.bak{_backupCounter++}{Path.GetExtension(_configFilePath)}";
        string backupFilePath = Path.Combine(Path.GetDirectoryName(_configFilePath)!, backupFileName);

        try
        {
            if (invalidContent != null)
            {
                File.WriteAllText(backupFilePath, invalidContent);
                Console.WriteLine($"TextExpansion: Invalid configuration backed up to: {backupFilePath}");
            }

            if (_currentConfiguration.Rules.Count == 0)
            {
                _currentConfiguration = CreateDefaultConfiguration();
            }

            Normalize(_currentConfiguration);
            SaveConfigurationInternal(_currentConfiguration);
            Console.WriteLine("TextExpansion: Configuration reverted to last known good state.");

            string message = $"The text-expansions.json file is invalid.\nError: {errorMessage}\n\nA backup was created at:\n{backupFilePath}\n\nThe configuration has been reverted to the last known good state.";
            PInvoke.MessageBox(HWND.Null, message, "Text Expansion Configuration Error", MESSAGEBOX_STYLE.MB_ICONERROR);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TextExpansion: Error during invalid file handling: {ex.Message}");
        }
    }

    /// <summary>Saves the current configuration to the file.</summary>
    private void SaveConfigurationInternal(TextExpansionConfiguration configuration)
    {
        EnsureDirectory();
        Normalize(configuration);
        string jsonString = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configFilePath, jsonString);
    }

    public void ReloadNow()
    {
        _ = LoadConfiguration();
    }

    public void SaveConfiguration(TextExpansionConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        lock (_fileLock)
        {
            Normalize(configuration);
            _currentConfiguration = configuration;
            SaveConfigurationInternal(_currentConfiguration);
            ConfigurationChanged?.Invoke(this, _currentConfiguration);
        }
    }

    private void EnsureDirectory()
    {
        string? directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void Normalize(TextExpansionConfiguration config)
    {
        config.Rules ??= new List<TextExpansionRule>();
        config.Settings ??= new TextExpansionSettings();

        config.Settings.Delimiters ??= new List<string>();
        config.Settings.BufferSize = Math.Clamp(config.Settings.BufferSize, 8, 512);
        config.Settings.BackspaceDelayMs = Math.Clamp(config.Settings.BackspaceDelayMs, 0, 200);
        config.Settings.PasteDelayMs = Math.Clamp(config.Settings.PasteDelayMs, 0, 500);

        foreach (var rule in config.Rules)
        {
            rule.Trigger ??= string.Empty;
            rule.Expansion ??= string.Empty;
        }
    }

    /// <summary>
    /// Adds a new text expansion rule.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>True if the rule was added; false if a rule with the same trigger already exists.</returns>
    public bool AddRule(TextExpansionRule rule)
    {
        lock (_fileLock)
        {
            // Check for duplicate triggers
            if (_currentConfiguration.Rules.Any(r => r.Trigger.Equals(rule.Trigger, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"TextExpansion: Rule with trigger '{rule.Trigger}' already exists.");
                return false;
            }

            _currentConfiguration.Rules.Add(rule);
            SaveConfigurationInternal(_currentConfiguration);
            Console.WriteLine($"TextExpansion: Added rule: {rule}");
            ConfigurationChanged?.Invoke(this, _currentConfiguration);
            return true;
        }
    }

    /// <summary>
    /// Removes a text expansion rule by trigger.
    /// </summary>
    /// <param name="trigger">The trigger of the rule to remove.</param>
    /// <returns>True if the rule was removed; false if not found.</returns>
    public bool RemoveRule(string trigger)
    {
        lock (_fileLock)
        {
            var rule = _currentConfiguration.Rules.FirstOrDefault(r => r.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                Console.WriteLine($"TextExpansion: Rule with trigger '{trigger}' not found.");
                return false;
            }

            _currentConfiguration.Rules.Remove(rule);
            SaveConfigurationInternal(_currentConfiguration);
            Console.WriteLine($"TextExpansion: Removed rule with trigger: {trigger}");
            ConfigurationChanged?.Invoke(this, _currentConfiguration);
            return true;
        }
    }

    /// <summary>
    /// Updates an existing rule or adds it if not found.
    /// </summary>
    /// <param name="rule">The rule to update or add.</param>
    public void UpdateRule(TextExpansionRule rule)
    {
        lock (_fileLock)
        {
            var existingIndex = _currentConfiguration.Rules.FindIndex(r => r.Trigger.Equals(rule.Trigger, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _currentConfiguration.Rules[existingIndex] = rule;
            }
            else
            {
                _currentConfiguration.Rules.Add(rule);
            }

            SaveConfigurationInternal(_currentConfiguration);
            Console.WriteLine($"TextExpansion: Updated rule: {rule}");
            ConfigurationChanged?.Invoke(this, _currentConfiguration);
        }
    }

    /// <summary>Disposes of the configuration manager.</summary>
    public void Dispose()
    {
        _configWatcher.Dispose();
        Console.WriteLine("TextExpansionConfigurationManager disposed.");
    }
}
