using System.Text.Json;
using System.Text.Json.Serialization;
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
    private FileSystemWatcher? _watcher;
    private TextExpansionConfiguration _currentConfiguration;
    private readonly object _fileLock = new();
    private int _backupCounter = 0;
    private DateTime _lastReloadTime = DateTime.MinValue;
    private static readonly TimeSpan ReloadDebounceTime = TimeSpan.FromMilliseconds(500);

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

    /// <summary>
    /// Initializes a new instance of the <see cref="TextExpansionConfigurationManager"/> class.
    /// </summary>
    /// <param name="configFilePath">The full path to the text expansion configuration JSON file.</param>
    public TextExpansionConfigurationManager(string configFilePath)
    {
        _configFilePath = configFilePath;
        _currentConfiguration = new TextExpansionConfiguration();
        InitializeWatcher();
    }

    /// <summary>Sets up the FileSystemWatcher to monitor the configuration file.</summary>
    private void InitializeWatcher()
    {
        string? directory = Path.GetDirectoryName(_configFilePath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        string fileName = Path.GetFileName(_configFilePath);

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnConfigFileChanged;
        _watcher.Created += OnConfigFileChanged;
        _watcher.Deleted += OnConfigFileChanged;
        _watcher.Renamed += OnConfigFileRenamed;

        Console.WriteLine($"TextExpansion: Watching for changes to: {_configFilePath}");
    }

    /// <summary>Handler for file change events.</summary>
    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid file change events
        if (DateTime.Now - _lastReloadTime < ReloadDebounceTime)
            return;

        Console.WriteLine($"TextExpansion: Configuration file {e.ChangeType}: {e.FullPath}");

        // Add a small delay to ensure the file is not locked
        Task.Delay(200).ContinueWith(_ => LoadConfiguration());
    }

    /// <summary>Handler for file rename events.</summary>
    private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"TextExpansion: Configuration file Renamed: {e.OldFullPath} to {e.FullPath}");

        if (e.FullPath.Equals(_configFilePath, StringComparison.OrdinalIgnoreCase))
        {
            Task.Delay(200).ContinueWith(_ => LoadConfiguration());
        }
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
            _lastReloadTime = DateTime.Now;

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

            // Create default configuration
            _currentConfiguration = CreateDefaultConfiguration();
            SaveConfigurationInternal(_currentConfiguration);
            Console.WriteLine("TextExpansion: Configuration reverted to default.");

            string message = $"The text-expansions.json file is invalid.\nError: {errorMessage}\n\nA backup was created at:\n{backupFilePath}\n\nDefault configuration has been restored.";
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
        string jsonString = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configFilePath, jsonString);
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
        _watcher?.Dispose();
        Console.WriteLine("TextExpansionConfigurationManager disposed.");
    }
}
