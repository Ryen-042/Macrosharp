using Macrosharp.Devices.Core;
using Macrosharp.UserInterfaces.DynamicWindow;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.Hosts.ConsoleHost;

internal sealed class BurstClickCoordinator
{
    private readonly object _stateGate = new();
    private bool _isActive;
    private VirtualKey _burstClickKey = VirtualKey.KEY_A;
    private int _burstClickIntervalMs = KeyboardSimulator.DefaultBurstClickIntervalMs;
    private int _burstClickDurationMs = KeyboardSimulator.DefaultBurstClickDurationMs;
    private string? _burstClickStopReason;
    private CancellationTokenSource? _burstClickCancellation;
    private Task? _burstClickTask;

    public bool IsActive()
    {
        lock (_stateGate)
        {
            return _isActive;
        }
    }

    public void Stop(string reason, bool notifyWhenInactive = true)
    {
        CancellationTokenSource? cancellationToCancel;

        lock (_stateGate)
        {
            if (!_isActive)
            {
                if (notifyWhenInactive)
                {
                    Console.WriteLine("Burst click is not active.");
                }
                return;
            }

            _burstClickStopReason = reason;
            cancellationToCancel = _burstClickCancellation;
        }

        cancellationToCancel?.Cancel();
    }

    public void Start()
    {
        if (IsActive())
        {
            Console.WriteLine("Burst click is already active. Use Stop Burst Click first.");
            return;
        }

        var window = new SimpleWindow("Start Burst Click", labelWidth: 200);
        window.CreateDynamicInputWindow(["Interval (ms)", "Duration (ms, 0 = infinite)"], [KeyboardSimulator.DefaultBurstClickIntervalMs.ToString(), KeyboardSimulator.DefaultBurstClickDurationMs.ToString()], enableKeyCapture: true);

        if (window.userInputs.Count < 2)
        {
            Console.WriteLine("Burst click start canceled.");
            return;
        }

        if (window.capturedKeyVK == 0)
        {
            Console.WriteLine("Burst click requires a captured key.");
            return;
        }

        string intervalText = SanitizeWindowInput(window.userInputs[0]);
        string durationText = SanitizeWindowInput(window.userInputs[1]);

        if (!TryParseBurstInteger(intervalText, KeyboardSimulator.DefaultBurstClickIntervalMs, "Interval", allowZero: false, out int requestedIntervalMs, out string? parseError))
        {
            Console.WriteLine($"Burst click start failed: {parseError}");
            return;
        }

        if (!TryParseBurstInteger(durationText, KeyboardSimulator.DefaultBurstClickDurationMs, "Duration", allowZero: true, out int requestedDurationMs, out parseError))
        {
            Console.WriteLine($"Burst click start failed: {parseError}");
            return;
        }

        VirtualKey requestedKey = (VirtualKey)window.capturedKeyVK;
        if (!KeyboardSimulator.TryValidateBurstClickSettings(requestedKey, requestedIntervalMs, requestedDurationMs, out string? validationError))
        {
            Console.WriteLine($"Burst click start failed: {validationError}");
            return;
        }

        CancellationTokenSource localCancellation;
        lock (_stateGate)
        {
            _burstClickKey = requestedKey;
            _burstClickIntervalMs = requestedIntervalMs;
            _burstClickDurationMs = requestedDurationMs;
            _burstClickStopReason = null;
            _burstClickCancellation = new CancellationTokenSource();
            localCancellation = _burstClickCancellation;
            _isActive = true;
        }

        _burstClickTask = Task.Run(async () =>
        {
            try
            {
                await KeyboardSimulator.SimulateBurstClicksAsync(_burstClickKey, _burstClickIntervalMs, _burstClickDurationMs, localCancellation.Token);

                if (_burstClickDurationMs > 0)
                {
                    Console.WriteLine("Burst click finished after requested duration.");
                }
            }
            catch (OperationCanceledException)
            {
                string stopReason;
                lock (_stateGate)
                {
                    stopReason = _burstClickStopReason ?? "cancellation";
                }

                Console.WriteLine($"Burst click stopped ({stopReason}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Burst click failed: {ex.Message}");
            }
            finally
            {
                lock (_stateGate)
                {
                    _isActive = false;
                    _burstClickStopReason = null;

                    if (ReferenceEquals(_burstClickCancellation, localCancellation))
                    {
                        _burstClickCancellation.Dispose();
                        _burstClickCancellation = null;
                    }

                    _burstClickTask = null;
                }
            }
        });

        Console.WriteLine(
            _burstClickDurationMs == 0
                ? $"Burst click started for key {_burstClickKey} every {_burstClickIntervalMs}ms. Use tray Stop or press ESC to stop."
                : $"Burst click started for key {_burstClickKey} every {_burstClickIntervalMs}ms for {_burstClickDurationMs}ms."
        );
    }

    private static string SanitizeWindowInput(string? value)
    {
        return (value ?? string.Empty).Replace("\0", string.Empty).Trim();
    }

    private static bool TryParseBurstInteger(string rawValue, int defaultValue, string fieldName, bool allowZero, out int parsedValue, out string? error)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsedValue = defaultValue;
            error = null;
            return true;
        }

        if (!int.TryParse(rawValue, out parsedValue))
        {
            error = $"{fieldName} must be a valid integer.";
            return false;
        }

        if (allowZero)
        {
            if (parsedValue < 0)
            {
                error = $"{fieldName} must be zero or greater.";
                return false;
            }
        }
        else if (parsedValue <= 0)
        {
            error = $"{fieldName} must be greater than zero.";
            return false;
        }

        error = null;
        return true;
    }
}
