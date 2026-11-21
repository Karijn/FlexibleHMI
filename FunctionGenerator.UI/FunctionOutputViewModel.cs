using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FunctionGenerator;
using PropertyManager.Interfaces;
using System.Diagnostics;

namespace FunctionGenerator.UI;

public partial class FunctionOutputViewModel : ObservableObject
{
    private readonly IVariableHandler _variableHandler;
    private CancellationTokenSource? _cancellationTokenSource;

    public FunctionOutputViewModel(IVariableHandler variableHandler, string propertyName)
    {
        _variableHandler = variableHandler;
        PropertyName = propertyName;
        Waveforms = Enum.GetValues<SignalWaveform>();
    }

    public IReadOnlyList<SignalWaveform> Waveforms { get; }

    [ObservableProperty]
    private string _propertyName = string.Empty;

    [ObservableProperty]
    private SignalWaveform _waveform = SignalWaveform.Sinus;

    [ObservableProperty]
    private double _frequency = 1;

    [ObservableProperty]
    private double _amplitude = 1;

    [ObservableProperty]
    private double _offset;

    [ObservableProperty]
    private int _updateIntervalMilliseconds = 50;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _status = "Stopped";

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(PropertyName) ||
            !double.IsFinite(Frequency) || Frequency is <= 0 or > 1000 ||
            !double.IsFinite(Amplitude) || !double.IsFinite(Offset) ||
            UpdateIntervalMilliseconds is < 1 or > 10000)
        {
            Status = "Set a property name, frequency above 0 and up to 1000 Hz, finite amplitude/offset, and 1-10000 ms update interval.";
            return;
        }

        var propertyName = PropertyName.Trim();
        var waveform = Waveform;
        var frequency = Frequency;
        var amplitude = Amplitude;
        var offset = Offset;
        var updateInterval = UpdateIntervalMilliseconds;
        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationTokenSource = cancellationTokenSource;
        IsRunning = true;
        Status = SignalGenerator.IsNoise(waveform)
            ? "Running noise (sample rate is set by the update interval)"
            : "Running (stop and restart to apply setting changes)";
        NotifyCommands();

        try
        {
            await Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var noiseGenerator = new SignalNoiseGenerator();
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(updateInterval));
                while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token))
                {
                    var phase = stopwatch.Elapsed.TotalSeconds * frequency;
                    _variableHandler.Set(
                        propertyName,
                        SignalGenerator.Generate(waveform, phase, amplitude, offset, noiseGenerator));
                }
            });
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            Status = $"Stopped: {exception.Message}";
        }
        finally
        {
            cancellationTokenSource.Dispose();
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
            {
                _cancellationTokenSource = null;
                IsRunning = false;
                if (Status.StartsWith("Running", StringComparison.Ordinal))
                {
                    Status = "Stopped";
                }

                NotifyCommands();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void StopGeneration()
    {
        _cancellationTokenSource?.Cancel();
    }

    public FunctionOutputViewModel Copy(IVariableHandler variableHandler) => new(variableHandler, PropertyName)
    {
        Waveform = Waveform,
        Frequency = Frequency,
        Amplitude = Amplitude,
        Offset = Offset,
        UpdateIntervalMilliseconds = UpdateIntervalMilliseconds
    };

    private bool CanStart() => !IsRunning;

    private void NotifyCommands()
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}
