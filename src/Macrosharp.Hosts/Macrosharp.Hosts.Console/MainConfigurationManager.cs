using System.Text.Json;
using System.Text.Json.Serialization;
using Macrosharp.Infrastructure;

namespace Macrosharp.Hosts.ConsoleHost;

public sealed class MainConfiguration
{
    public int Version { get; set; } = 1;
    public MainTraySettings Tray { get; set; } = new();
    public MainDiagnosticsSettings Diagnostics { get; set; } = new();
    public MainFileWatchingSettings FileWatching { get; set; } = new();
}

public sealed class MainTraySettings
{
    public bool NotificationsHidden { get; set; }
    public bool ReminderSoundsMuted { get; set; }
}

public sealed class MainDiagnosticsSettings
{
    public bool TerminalMessagesEnabled { get; set; }
}

public sealed class MainFileWatchingSettings
{
    public bool MainConfig { get; set; }
    public bool HotkeysConfig { get; set; }
    public bool TextExpansionsConfig { get; set; }
    public bool RemindersConfig { get; set; }
}

public sealed class MainConfigurationManager : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _configPath;
    private readonly object _gate = new();
    private DebouncedFileWatcher? _configWatcher;

    public event EventHandler<MainConfiguration>? ConfigurationChanged;

    public MainConfiguration CurrentConfiguration { get; private set; } = new();

    public MainConfigurationManager(string configPath, bool watchForChanges = false)
    {
        _configPath = configPath;

        if (watchForChanges)
        {
            EnableWatching();
        }
    }

    public string ConfigPath => _configPath;

    public MainConfiguration LoadOrCreate()
    {
        lock (_gate)
        {
            if (!File.Exists(_configPath))
            {
                var defaults = CreateDefaultConfiguration();
                Save(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<MainConfiguration>(json, JsonOptions) ?? CreateDefaultConfiguration();
                Normalize(config);
                Save(config);
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Main config load failed: {ex.Message}. Reverting to defaults.");
                var defaults = CreateDefaultConfiguration();
                Save(defaults);
                return defaults;
            }
        }
    }

    public void Save(MainConfiguration configuration)
    {
        Normalize(configuration);
        EnsureDirectory();
        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configPath, json);

        CurrentConfiguration = configuration;
        ConfigurationChanged?.Invoke(this, CurrentConfiguration);
    }

    public void ReloadNow()
    {
        _ = LoadOrCreate();
    }

    public void EnableWatching()
    {
        if (_configWatcher is not null)
        {
            return;
        }

        _configWatcher = new DebouncedFileWatcher(_configPath, ReloadNow, enabled: true, nameof(MainConfigurationManager), TimeSpan.FromMilliseconds(500));
    }

    private void EnsureDirectory()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static MainConfiguration CreateDefaultConfiguration()
    {
        return new MainConfiguration
        {
            Version = 1,
            Tray = new MainTraySettings
            {
                NotificationsHidden = false,
                ReminderSoundsMuted = false,
            },
            Diagnostics = new MainDiagnosticsSettings
            {
                TerminalMessagesEnabled = false,
            },
            FileWatching = new MainFileWatchingSettings
            {
                MainConfig = false,
                HotkeysConfig = false,
                TextExpansionsConfig = false,
                RemindersConfig = false,
            },
        };
    }

    private static void Normalize(MainConfiguration configuration)
    {
        configuration.Version = Math.Max(1, configuration.Version);
        configuration.Tray ??= new MainTraySettings();
        configuration.Diagnostics ??= new MainDiagnosticsSettings();
        configuration.FileWatching ??= new MainFileWatchingSettings();
    }

    public void Dispose()
    {
        _configWatcher?.Dispose();
        _configWatcher = null;
    }
}
