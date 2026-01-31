using System.Text.Json;
using Windows.Win32; // For PInvoke.MessageBox
using Windows.Win32.Foundation; // For HWND
using Windows.Win32.UI.WindowsAndMessaging; // For MESSAGEBOX_STYLE

namespace Macrosharp.Devices.Keyboard.HotkeyBindings;

// Manages loading, saving, and monitoring changes to the hotkey configuration file.
public class HotkeyConfigurationManager : IDisposable
{
    private readonly string _configFilePath;
    private FileSystemWatcher? _watcher;
    private List<HotkeyDefinition> _currentDefinitions;
    private readonly object _fileLock = new object(); // To prevent concurrent file access issues
    private int _backupCounter = 0; // Counter for backup files

    // Event raised when the configuration file changes and is reloaded.
    public event EventHandler<List<HotkeyDefinition>>? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="HotkeyConfigurationManager"/> class.
    /// </summary>
    /// <param name="configFilePath">The full path to the hotkey configuration JSON file.</param>
    public HotkeyConfigurationManager(string configFilePath)
    {
        _configFilePath = configFilePath;
        _currentDefinitions = new List<HotkeyDefinition>();
        InitializeWatcher();
    }

    // Sets up the FileSystemWatcher to monitor the configuration file.
    private void InitializeWatcher()
    {
        string? directory = Path.GetDirectoryName(_configFilePath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = AppContext.BaseDirectory; // Fallback to application base directory
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

        Console.WriteLine($"Watching for changes to: {_configFilePath}");
    }

    // Handler for file change events.
    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Configuration file {e.ChangeType}: {e.FullPath}");
        // Add a small delay to ensure the file is not locked by another process
        // and to allow the writing process to complete.
        Task.Delay(200).Wait();
        LoadConfiguration();
    }

    // Handler for file rename events.
    private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"Configuration file Renamed: {e.OldFullPath} to {e.FullPath}");
        // If the file was renamed to our target path, load it.
        if (e.FullPath.Equals(_configFilePath, StringComparison.OrdinalIgnoreCase))
        {
            Task.Delay(200).Wait();
            LoadConfiguration();
        }
    }

    /// <summary>
    /// Loads the hotkey definitions from the configuration file.
    /// If the file doesn't exist, it creates a default empty one.
    /// If the file is invalid, it creates a backup and reverts to the last valid configuration.
    /// </summary>
    /// <returns>A list of loaded hotkey definitions.</returns>
    public List<HotkeyDefinition> LoadConfiguration()
    {
        lock (_fileLock) // Ensure exclusive access to the file
        {
            if (!File.Exists(_configFilePath))
            {
                Console.WriteLine($"Configuration file not found: {_configFilePath}. Creating a default empty one.");
                _currentDefinitions = new List<HotkeyDefinition>();
                SaveConfigurationInternal(_currentDefinitions); // Create an empty config
                ConfigurationChanged?.Invoke(this, _currentDefinitions); // Notify even if empty
                return _currentDefinitions;
            }

            List<HotkeyDefinition>? loadedDefinitions = null;
            string? jsonString = null;

            try
            {
                jsonString = File.ReadAllText(_configFilePath);
                loadedDefinitions = JsonSerializer.Deserialize<List<HotkeyDefinition>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (loadedDefinitions == null)
                {
                    throw new JsonException("Deserialization resulted in a null list of hotkey definitions.");
                }

                _currentDefinitions = loadedDefinitions;
                Console.WriteLine($"Configuration loaded. Hotkeys found: {_currentDefinitions.Count}");
                ConfigurationChanged?.Invoke(this, _currentDefinitions);
                return _currentDefinitions;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing hotkey configuration: {ex.Message}. Invalid JSON format detected.");
                HandleInvalidConfigFile(jsonString, ex.Message);
                return _currentDefinitions; // Return the last valid configuration
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO Error reading configuration file: {ex.Message}. This might be a temporary file lock.");
                // This can happen if the file is still being written to.
                // A more robust solution might involve retries.
                return _currentDefinitions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while loading configuration: {ex.Message}");
                HandleInvalidConfigFile(jsonString, ex.Message);
                return _currentDefinitions; // Return the last valid configuration
            }
        }
    }

    // Handles an invalid configuration file by creating a backup and reverting.
    private void HandleInvalidConfigFile(string? invalidContent, string errorMessage)
    {
        string backupFileName = $"{Path.GetFileNameWithoutExtension(_configFilePath)}.bak{_backupCounter++}{Path.GetExtension(_configFilePath)}";
        string backupFilePath = Path.Combine(Path.GetDirectoryName(_configFilePath)!, backupFileName);

        try
        {
            if (invalidContent != null)
            {
                File.WriteAllText(backupFilePath, invalidContent);
                Console.WriteLine($"Invalid configuration file backed up to: {backupFilePath}");
            }
            else
            {
                Console.WriteLine("Could not read content for backup. Backup not created.");
            }

            // Overwrite the invalid file with the last known good configuration
            SaveConfigurationInternal(_currentDefinitions);
            Console.WriteLine("Configuration file reverted to last valid state.");

            string message =
                $"The hotkeys.json file is invalid and could not be loaded.\nError: {errorMessage}\n\nA backup of the invalid file has been created at:\n{backupFilePath}\n\nThe configuration has been reverted to its previous valid state.";
            PInvoke.MessageBox(HWND.Null, message, "Hotkey Configuration Error", MESSAGEBOX_STYLE.MB_ICONERROR);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during invalid file handling (backup/revert): {ex.Message}");
            string message = $"The hotkeys.json file is invalid and could not be loaded.\nError: {errorMessage}\n\nAttempted to create backup and revert, but failed: {ex.Message}";
            PInvoke.MessageBox(HWND.Null, message, "Hotkey Configuration Error", MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }

    /// <summary>
    /// Saves the current list of hotkey definitions to the configuration file.
    /// This method is called internally and by public Add/Remove methods.
    /// </summary>
    /// <param name="definitions">The list of hotkey definitions to save.</param>
    private void SaveConfigurationInternal(List<HotkeyDefinition> definitions)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(definitions, options);
        File.WriteAllText(_configFilePath, jsonString);
    }

    /// <summary>
    /// Adds a new hotkey definition to the configuration and saves it.
    /// Prevents adding duplicate hotkeys based on key, modifiers, and lock keys.
    /// </summary>
    /// <param name="newDefinition">The hotkey definition to add.</param>
    public void AddHotkeyDefinition(HotkeyDefinition newDefinition)
    {
        lock (_fileLock)
        {
            // Normalize modifiers and lock keys for comparison
            var normalizedNewModifiers = newDefinition.Modifiers.OrderBy(m => m).ToList();
            var normalizedNewLockKeys = newDefinition.LockKeys.OrderBy(l => l).ToList();

            // Check for duplicates before adding
            if (_currentDefinitions.Any(d => d.Key.Equals(newDefinition.Key, StringComparison.OrdinalIgnoreCase) && d.Modifiers.OrderBy(m => m).SequenceEqual(normalizedNewModifiers) && d.LockKeys.OrderBy(l => l).SequenceEqual(normalizedNewLockKeys)))
            {
                Console.WriteLine($"Hotkey definition for '{newDefinition.Key}' with specified modifiers/lock keys already exists. Not adding.");
                return;
            }

            _currentDefinitions.Add(newDefinition);
            SaveConfigurationInternal(_currentDefinitions);
            Console.WriteLine($"Added hotkey definition: {newDefinition.Key} with modifiers {string.Join("+", newDefinition.Modifiers)}");
            // Trigger reload to update registered hotkeys immediately
            ConfigurationChanged?.Invoke(this, _currentDefinitions);
        }
    }

    /// <summary>
    /// Removes a hotkey definition from the configuration and saves it.
    /// </summary>
    /// <param name="key">The main key of the hotkey to remove.</param>
    /// <param name="modifiers">The list of modifier keys for the hotkey to remove.</param>
    /// <param name="lockKeys">The list of lock keys for the hotkey to remove.</param>
    /// <returns>True if the hotkey was removed successfully; otherwise, false.</returns>
    public bool RemoveHotkeyDefinition(string key, List<string> modifiers, List<string> lockKeys)
    {
        lock (_fileLock)
        {
            // Normalize modifiers and lock keys for comparison
            var normalizedModifiers = modifiers.OrderBy(m => m).ToList();
            var normalizedLockKeys = lockKeys.OrderBy(l => l).ToList();

            var hotkeyToRemove = _currentDefinitions.FirstOrDefault(d =>
                d.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && d.Modifiers.OrderBy(m => m).SequenceEqual(normalizedModifiers) && d.LockKeys.OrderBy(l => l).SequenceEqual(normalizedLockKeys)
            );

            if (hotkeyToRemove != null)
            {
                _currentDefinitions.Remove(hotkeyToRemove);
                SaveConfigurationInternal(_currentDefinitions);
                Console.WriteLine($"Removed hotkey definition for '{key}' with modifiers '{string.Join("+", modifiers)}'.");
                // Trigger reload to update registered hotkeys immediately
                ConfigurationChanged?.Invoke(this, _currentDefinitions);
                return true;
            }
            Console.WriteLine($"Hotkey definition for '{key}' with modifiers '{string.Join("+", modifiers)}' not found for removal.");
            return false;
        }
    }

    /// <summary>
    /// Disposes the <see cref="HotkeyConfigurationManager"/> and its internal <see cref="FileSystemWatcher"/>.
    /// </summary>
    public void Dispose()
    {
        _watcher?.Dispose();
        Console.WriteLine("HotkeyConfigurationManager disposed.");
    }
}
