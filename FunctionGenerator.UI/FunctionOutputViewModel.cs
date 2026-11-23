using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyManager.Interfaces;
using System.Diagnostics;

namespace FunctionGenerator.UI;

/// <summary>Configures and controls one continuously generated waveform output.</summary>
public partial class FunctionOutputViewModel : ObservableObject
{
    private readonly IVariableHandler _variableHandler;
    private readonly Func<string, bool>? _isControlProperty;
    private readonly Action<FunctionOutputViewModel>? _controlChanged;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _generationTask;

    /// <summary>Creates an output configuration and associates it with a live-property handler.</summary>
    /// <param name="variableHandler">Handler used to read bound inputs and write generated samples.</param>
    /// <param name="propertyName">Initial destination property name.</param>
    /// <param name="isControlProperty">Optional predicate identifying properties reserved for start/stop control.</param>
    /// <param name="controlChanged">Optional callback invoked when the per-output running-property name changes.</param>
    public FunctionOutputViewModel(IVariableHandler variableHandler, string propertyName, Func<string, bool>? isControlProperty = null, Action<FunctionOutputViewModel>? controlChanged = null)
    {
        _variableHandler = variableHandler;
        _isControlProperty = isControlProperty;
        _controlChanged = controlChanged;
        PropertyName = propertyName;
        Waveforms = Enum.GetValues<SignalWaveform>();
    }

    /// <summary>Gets the waveform values offered by the view.</summary>
    public IReadOnlyList<SignalWaveform> Waveforms { get; }
    internal string? ActiveOutputPropertyName { get; private set; }

    /// <summary>Gets or sets the property receiving generated samples.</summary>
    [ObservableProperty]
    private string _propertyName = string.Empty;

    /// <summary>Gets or sets the waveform shape or noise distribution.</summary>
    [ObservableProperty]
    private SignalWaveform _waveform = SignalWaveform.Sinus;

    /// <summary>Gets or sets the fallback frequency in hertz.</summary>
    [ObservableProperty]
    private double _frequency = 1;

    /// <summary>Gets or sets the optional numeric property supplying the live frequency.</summary>
    [ObservableProperty]
    private string _frequencyPropertyName = string.Empty;

    /// <summary>Gets or sets the fallback amplitude.</summary>
    [ObservableProperty]
    private double _amplitude = 1;

    /// <summary>Gets or sets the optional numeric property supplying the live amplitude.</summary>
    [ObservableProperty]
    private string _amplitudePropertyName = string.Empty;

    /// <summary>Gets or sets the fallback offset added to generated samples.</summary>
    [ObservableProperty]
    private double _offset;

    /// <summary>Gets or sets the optional numeric property supplying the live offset.</summary>
    [ObservableProperty]
    private string _offsetPropertyName = string.Empty;

    /// <summary>Gets or sets the fallback delay between output samples, in milliseconds.</summary>
    [ObservableProperty]
    private int _updateIntervalMilliseconds = 50;

    /// <summary>Gets or sets the optional numeric property supplying the live sample interval.</summary>
    [ObservableProperty]
    private string _updateIntervalPropertyName = string.Empty;

    /// <summary>Gets whether sample generation is currently active.</summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>Gets the current generation state or validation/runtime error.</summary>
    [ObservableProperty]
    private string _status = "Stopped";

    /// <summary>Gets or sets the optional Boolean property controlling this output.</summary>
    [ObservableProperty]
    private string _runningPropertyName = string.Empty;

    /// <summary>Gets the status of the per-output Boolean control binding.</summary>
    [ObservableProperty]
    private string _runningControlStatus = string.Empty;

    partial void OnRunningPropertyNameChanged(string value) => _controlChanged?.Invoke(this);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(PropertyName) ||
            !TryGetInputValue(FrequencyPropertyName, Frequency, out var initialFrequency) || initialFrequency is <= 0 or > 1000 ||
            !TryGetInputValue(AmplitudePropertyName, Amplitude, out _) ||
            !TryGetInputValue(OffsetPropertyName, Offset, out _) ||
            !TryGetIntervalValue(UpdateIntervalPropertyName, UpdateIntervalMilliseconds, out _))
        {
            Status = "Set a property name, frequency above 0 and up to 1000 Hz, finite amplitude/offset, and 1-10000 ms update interval.";
            return;
        }

        var propertyName = PropertyName.Trim();
        if (_isControlProperty?.Invoke(propertyName) == true || string.Equals(propertyName, RunningPropertyName.Trim(), StringComparison.Ordinal))
        {
            Status = "The output property must differ from all start/stop properties.";
            return;
        }
        ActiveOutputPropertyName = propertyName;
        var frequencyPropertyName = FrequencyPropertyName.Trim();
        var amplitudePropertyName = AmplitudePropertyName.Trim();
        var offsetPropertyName = OffsetPropertyName.Trim();
        var updateIntervalPropertyName = UpdateIntervalPropertyName.Trim();
        var fallbackFrequency = Frequency;
        var fallbackAmplitude = Amplitude;
        var fallbackOffset = Offset;
        var fallbackUpdateIntervalMilliseconds = UpdateIntervalMilliseconds;
        var waveform = Waveform;
        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationTokenSource = cancellationTokenSource;
        IsRunning = true;
        Status = SignalGenerator.IsNoise(waveform)
            ? "Running noise (bound inputs and sample rate update live)"
            : "Running (bound inputs update live; stop and restart to change waveform or output property)";
        NotifyCommands();

        Task? generationTask = null;
        try
        {
            generationTask = Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var noiseGenerator = new SignalNoiseGenerator();
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (!TryGetIntervalValue(updateIntervalPropertyName, fallbackUpdateIntervalMilliseconds, out var currentUpdateInterval))
                    {
                        throw new InvalidOperationException("A bound update interval must contain a numeric value from 1 to 10000 milliseconds.");
                    }

                    await Task.Delay(currentUpdateInterval, cancellationTokenSource.Token);
                    if (!TryGetInputValue(frequencyPropertyName, fallbackFrequency, out var frequency) || frequency is <= 0 or > 1000 ||
                        !TryGetInputValue(amplitudePropertyName, fallbackAmplitude, out var amplitude) ||
                        !TryGetInputValue(offsetPropertyName, fallbackOffset, out var offset))
                    {
                        throw new InvalidOperationException("A bound input property must contain a finite numeric value, and frequency must be above 0 and no greater than 1000 Hz.");
                    }

                    var phase = stopwatch.Elapsed.TotalSeconds * frequency;
                    _variableHandler.Set(
                        propertyName,
                        SignalGenerator.Generate(waveform, phase, amplitude, offset, noiseGenerator));
                }
            });
            _generationTask = generationTask;
            await generationTask;
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                Status = $"Stopped: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(_generationTask, generationTask))
            {
                _generationTask = null;
            }

            cancellationTokenSource.Dispose();
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
            {
                _cancellationTokenSource = null;
                ActiveOutputPropertyName = null;
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
    private void Stop() => StopGeneration();

    /// <summary>Starts sample generation if this output is stopped and its settings are valid.</summary>
    /// <remarks>Validation and runtime failures are reported through <see cref="Status"/>.</remarks>
    public void StartGeneration()
    {
        if (CanStart())
        {
            _ = StartAsync();
        }
    }

    /// <summary>Cancels sample generation and waits for the generation task to finish.</summary>
    public void StopGeneration()
    {
        var cancellationTokenSource = _cancellationTokenSource;
        cancellationTokenSource?.Cancel();

        var generationTask = _generationTask;
        if (generationTask is null)
        {
            return;
        }

        try
        {
            generationTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested == true)
        {
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            Status = $"Stopped: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
            {
                _cancellationTokenSource = null;
                _generationTask = null;
                ActiveOutputPropertyName = null;
                IsRunning = false;
                if (Status.StartsWith("Running", StringComparison.Ordinal)) Status = "Stopped";
                NotifyCommands();
            }
        }
    }

    /// <summary>Creates an output with matching configuration and the supplied property/control services.</summary>
    /// <param name="variableHandler">Handler for the copied output.</param>
    /// <param name="isControlProperty">Optional predicate identifying reserved control properties.</param>
    /// <param name="controlChanged">Optional callback for changes to the running-property binding.</param>
    /// <returns>A stopped output with the same waveform and bound-input settings.</returns>
    public FunctionOutputViewModel Copy(IVariableHandler variableHandler, Func<string, bool>? isControlProperty = null, Action<FunctionOutputViewModel>? controlChanged = null) => new(variableHandler, PropertyName, isControlProperty, controlChanged)
    {
        RunningPropertyName = RunningPropertyName,
        Waveform = Waveform,
        Frequency = Frequency,
        FrequencyPropertyName = FrequencyPropertyName,
        Amplitude = Amplitude,
        AmplitudePropertyName = AmplitudePropertyName,
        Offset = Offset,
        OffsetPropertyName = OffsetPropertyName,
        UpdateIntervalMilliseconds = UpdateIntervalMilliseconds,
        UpdateIntervalPropertyName = UpdateIntervalPropertyName
    };

    private bool CanStart() => !IsRunning;

    private bool TryGetIntervalValue(string propertyName, int fallbackValue, out int value)
    {
        value = fallbackValue;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return value is >= 1 and <= 10000;
        }

        if (!TryGetInputValue(propertyName, fallbackValue, out var numericValue) ||
            !double.IsFinite(numericValue) || numericValue is < 1 or > 10000)
        {
            return false;
        }

        value = (int)Math.Round(numericValue, MidpointRounding.ToEven);
        return value is >= 1 and <= 10000;
    }

    private bool TryGetInputValue(string propertyName, double fallbackValue, out double value)
    {
        value = fallbackValue;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return double.IsFinite(value);
        }

        if (_variableHandler.TryGet<double>(propertyName.Trim(), out var number) && double.IsFinite(number))
        {
            value = number;
            return true;
        }

        if (_variableHandler.TryGet<int>(propertyName.Trim(), out var integer))
        {
            value = integer;
            return true;
        }

        return false;
    }

    private void NotifyCommands()
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}
