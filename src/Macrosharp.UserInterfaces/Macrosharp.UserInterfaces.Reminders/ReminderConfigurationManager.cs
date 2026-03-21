using System.Text.Json;
using System.Text.Json.Serialization;
using Macrosharp.Infrastructure;

namespace Macrosharp.UserInterfaces.Reminders;

public sealed class ReminderConfigurationManager : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _configPath;
    private readonly DebouncedFileWatcher _configWatcher;
    private readonly object _gate = new();
    private int _backupCounter;

    public ReminderConfiguration CurrentConfiguration { get; private set; } = new();

    public string ConfigPath => _configPath;

    public event EventHandler<ReminderConfiguration>? ConfigurationChanged;

    public ReminderConfigurationManager(string configPath, bool watchForChanges = false)
    {
        _configPath = configPath;
        _configWatcher = new DebouncedFileWatcher(_configPath, () => _ = LoadConfiguration(), watchForChanges, nameof(ReminderConfigurationManager), TimeSpan.FromMilliseconds(500));
    }

    public ReminderConfiguration LoadConfiguration()
    {
        lock (_gate)
        {
            if (!File.Exists(_configPath))
            {
                CurrentConfiguration = CreateDefaultConfiguration();
                SaveConfigurationInternal(CurrentConfiguration);
                ConfigurationChanged?.Invoke(this, CurrentConfiguration);
                return CurrentConfiguration;
            }

            string? raw = null;
            try
            {
                raw = File.ReadAllText(_configPath);
                var parsed = JsonSerializer.Deserialize<ReminderConfiguration>(raw, JsonOptions);
                if (parsed is null)
                {
                    throw new JsonException("Deserialization resulted in null reminder configuration.");
                }

                Normalize(parsed);
                CurrentConfiguration = parsed;
                ConfigurationChanged?.Invoke(this, CurrentConfiguration);
                return CurrentConfiguration;
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                Console.WriteLine($"Reminder config load error: {ex.Message}");
                HandleInvalidConfig(raw, ex.Message);
                return CurrentConfiguration;
            }
        }
    }

    public void ReloadNow()
    {
        LoadConfiguration();
    }

    public void SaveConfiguration(ReminderConfiguration configuration)
    {
        lock (_gate)
        {
            Normalize(configuration);
            SaveConfigurationInternal(configuration);
            CurrentConfiguration = configuration;
            ConfigurationChanged?.Invoke(this, CurrentConfiguration);
        }
    }

    private void SaveConfigurationInternal(ReminderConfiguration configuration)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    private void HandleInvalidConfig(string? content, string message)
    {
        try
        {
            var backupDirectory = Path.GetDirectoryName(_configPath) ?? AppContext.BaseDirectory;
            Directory.CreateDirectory(backupDirectory);
            var backupFileName = $"{Path.GetFileNameWithoutExtension(_configPath)}.bak{_backupCounter++}{Path.GetExtension(_configPath)}";
            var backupPath = Path.Combine(backupDirectory, backupFileName);

            if (!string.IsNullOrEmpty(content))
            {
                File.WriteAllText(backupPath, content);
            }

            if (CurrentConfiguration.Reminders.Count == 0)
            {
                CurrentConfiguration = CreateDefaultConfiguration();
            }

            SaveConfigurationInternal(CurrentConfiguration);
            Console.WriteLine($"Reminder configuration reverted to last known good state. Backup: {backupPath}. Error: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reminder config recovery failed: {ex.Message}");
        }
    }

    private static ReminderConfiguration CreateDefaultConfiguration()
    {
        return new ReminderConfiguration
        {
            Reminders = new List<ReminderDefinition>
            {
                new()
                {
                    Id = "break-20",
                    Title = "Eye break",
                    Message = "[b]20-20-20 rule:[/b] Look at something [color=#5dade2]20 feet[/color] away for [i]20 seconds[/i].",
                    Recurrence = new ReminderRecurrence
                    {
                        Kind = ReminderRecurrenceKind.EveryInterval,
                        Interval = "00:20:00",
                        Anchor = ReminderIntervalAnchor.ProgramStart,
                    },
                },
                new()
                {
                    Id = "hydrate-hourly",
                    Title = "Hydration",
                    Message = "Drink some [b][color=#58d68d]water[/color][/b].",
                    Recurrence = new ReminderRecurrence
                    {
                        Kind = ReminderRecurrenceKind.EveryInterval,
                        Interval = "01:00:00",
                        Anchor = ReminderIntervalAnchor.ProgramStart,
                    },
                },
            },
        };
    }

    private static void Normalize(ReminderConfiguration config)
    {
        config.Version = Math.Max(config.Version, 1);
        config.Settings ??= new ReminderSettings();
        config.Settings.GlobalVolumePercent = Math.Clamp(config.Settings.GlobalVolumePercent, 0, 100);
        config.Settings.DefaultChannels ??= new ReminderChannels();
        config.Settings.PopupDefaults ??= new ReminderPopupOptions();
        config.Settings.PopupDefaults.MonitorIndex = config.Settings.PopupDefaults.MonitorIndex is >= 0 ? config.Settings.PopupDefaults.MonitorIndex : null;
        config.Reminders ??= new List<ReminderDefinition>();

        foreach (var reminder in config.Reminders)
        {
            if (string.IsNullOrWhiteSpace(reminder.Id))
            {
                reminder.Id = Guid.NewGuid().ToString("N");
            }

            reminder.Title = string.IsNullOrWhiteSpace(reminder.Title) ? "Reminder" : reminder.Title;
            reminder.Message ??= string.Empty;
            reminder.SoundVolumePercent = reminder.SoundVolumePercent.HasValue ? Math.Clamp(reminder.SoundVolumePercent.Value, 0, 100) : null;
            reminder.Recurrence ??= new ReminderRecurrence();
            reminder.Channels ??= new ReminderChannels
            {
                Toast = config.Settings.DefaultChannels.Toast,
                Popup = config.Settings.DefaultChannels.Popup,
                Sound = config.Settings.DefaultChannels.Sound,
            };

            reminder.Popup ??= new ReminderPopupOptions
            {
                Enabled = config.Settings.PopupDefaults.Enabled,
                Position = config.Settings.PopupDefaults.Position,
                MonitorIndex = config.Settings.PopupDefaults.MonitorIndex,
                DurationSeconds = config.Settings.PopupDefaults.DurationSeconds,
                OpacityPercent = config.Settings.PopupDefaults.OpacityPercent,
                SnoozeMinutes = new List<int>(config.Settings.PopupDefaults.SnoozeMinutes),
            };

            reminder.Popup.MonitorIndex = reminder.Popup.MonitorIndex is >= 0 ? reminder.Popup.MonitorIndex : null;
            reminder.Popup.DurationSeconds = Math.Clamp(reminder.Popup.DurationSeconds, 3, 120);
            reminder.Popup.OpacityPercent = Math.Clamp(reminder.Popup.OpacityPercent, 30, 100);
            if (reminder.Popup.SnoozeMinutes.Count == 0)
            {
                reminder.Popup.SnoozeMinutes = new List<int> { 5, 10, 15 };
            }
        }
    }

    public void Dispose()
    {
        _configWatcher.Dispose();
    }
}
