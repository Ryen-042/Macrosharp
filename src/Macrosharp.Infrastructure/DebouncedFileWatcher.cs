namespace Macrosharp.Infrastructure;

/// <summary>
/// Watches a single file and invokes a reload action after a debounce delay.
/// </summary>
public sealed class DebouncedFileWatcher : IDisposable
{
    private readonly string _targetFilePath;
    private readonly Action _onReload;
    private readonly TimeSpan _debounceTime;
    private readonly string _componentName;
    private readonly object _reloadGate = new();

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _reloadCts;

    public bool Enabled { get; }

    public DebouncedFileWatcher(string targetFilePath, Action onReload, bool enabled, string componentName, TimeSpan? debounceTime = null)
    {
        _targetFilePath = targetFilePath;
        _onReload = onReload;
        Enabled = enabled;
        _componentName = componentName;
        _debounceTime = debounceTime ?? TimeSpan.FromMilliseconds(300);

        Initialize();
    }

    private void Initialize()
    {
        if (!Enabled)
        {
            Console.WriteLine($"[INFO] [{_componentName}] Config watching disabled for: {_targetFilePath}");
            return;
        }

        string? directory = Path.GetDirectoryName(_targetFilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        string fileName = Path.GetFileName(_targetFilePath);

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnRenamed;

        Console.WriteLine($"[INFO] [{_componentName}] Watching for changes to: {_targetFilePath}");
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[INFO] [{_componentName}] Configuration file {e.ChangeType}: {e.FullPath}");
        ScheduleReload("file change");
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"[INFO] [{_componentName}] Configuration file renamed: {e.OldFullPath} -> {e.FullPath}");

        if (e.FullPath.Equals(_targetFilePath, StringComparison.OrdinalIgnoreCase))
        {
            ScheduleReload("file rename");
        }
    }

    private void ScheduleReload(string reason)
    {
        CancellationTokenSource cts;

        lock (_reloadGate)
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = new CancellationTokenSource();
            cts = _reloadCts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceTime, cts.Token).ConfigureAwait(false);
                _onReload();
            }
            catch (OperationCanceledException)
            {
                // Expected when events are coalesced.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] [{_componentName}] Deferred reload failed after {reason}: {ex.Message}");
            }
        });
    }

    public void Dispose()
    {
        lock (_reloadGate)
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;
        }

        if (_watcher is not null)
        {
            _watcher.Changed -= OnFileEvent;
            _watcher.Created -= OnFileEvent;
            _watcher.Deleted -= OnFileEvent;
            _watcher.Renamed -= OnRenamed;
            _watcher.Dispose();
            _watcher = null;
        }
    }
}
