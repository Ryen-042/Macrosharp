using System.Text.Json;
using System.Text.Json.Serialization;

namespace Macrosharp.Hosts.ConsoleHost;

public sealed class MainConfiguration
{
    public int Version { get; set; } = 1;
    public MainTraySettings Tray { get; set; } = new();
    public MainDiagnosticsSettings Diagnostics { get; set; } = new();
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

public sealed class MainConfigurationManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _configPath;

    public MainConfigurationManager(string configPath)
    {
        _configPath = configPath;
    }

    public string ConfigPath => _configPath;

    public MainConfiguration LoadOrCreate()
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

    public void Save(MainConfiguration configuration)
    {
        Normalize(configuration);
        EnsureDirectory();
        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configPath, json);
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
        };
    }

    private static void Normalize(MainConfiguration configuration)
    {
        configuration.Version = Math.Max(1, configuration.Version);
        configuration.Tray ??= new MainTraySettings();
        configuration.Diagnostics ??= new MainDiagnosticsSettings();
    }
}
