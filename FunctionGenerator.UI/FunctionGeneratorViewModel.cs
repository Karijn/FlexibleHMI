using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FunctionGenerator.UI;

public partial class FunctionGeneratorViewModel : TreeItemNode, ITreeItemDocumentData, IRuntimeStartStopController, IDocumentItemDetached
{
    private readonly IVariableHandler _variableHandler;
    private bool _isDetached = true;
    private int _subscriptionVersion;
    private int _nextOutputNumber = 1;

    public FunctionGeneratorViewModel(IVariableHandler variableHandler)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        Name = "Function Generator";
        Image = FunctionGeneratorCreator.GetIcon();
        RefreshAvailableProperties();
        Outputs.Add(CreateOutput());
    }

    public ObservableCollection<FunctionOutputViewModel> Outputs { get; } = [];

    public void OnDocumentItemDetached()
    {
        _isDetached = true;
        Interlocked.Increment(ref _subscriptionVersion);
        _variableHandler.VariableChanged -= OnVariableChanged;
        Stop();
    }

    public void OnDocumentItemReattached()
    {
        if (_isDetached)
        {
            _isDetached = false;
            Interlocked.Increment(ref _subscriptionVersion);
            _variableHandler.VariableChanged += OnVariableChanged;
            RefreshAvailableProperties();
            ApplyStartStopProperty();
            foreach (var output in Outputs) ApplyOutputControl(output);
        }
    }

    public ObservableCollection<string> AvailableProperties { get; } = [];

    public ObservableCollection<string> BooleanProperties { get; } = [];

    [ObservableProperty]
    private string _startStopPropertyName = string.Empty;

    [ObservableProperty]
    private string _controlStatus = string.Empty;

    public override string Kind => FunctionGeneratorCreator.jsonkey;

    public override int MaxChildren => -1;

    public override string ChildRule => "Any number of children";

    public override bool CanAcceptChildren => true;

    [RelayCommand]
    private void AddOutput()
    {
        var output = CreateOutput();
        Outputs.Add(output);
        if (!_isDetached && !string.IsNullOrWhiteSpace(StartStopPropertyName) &&
            _variableHandler.TryGet<bool>(StartStopPropertyName.Trim(), out var shouldRun) && shouldRun)
            output.StartGeneration();
    }

    [RelayCommand]
    private void StartAll() => Start();

    [RelayCommand]
    private void StopAll() => Stop();

    partial void OnStartStopPropertyNameChanged(string value)
    {
        ApplyStartStopProperty();
        foreach (var output in Outputs) ApplyOutputControl(output);
    }

    [RelayCommand]
    private void RemoveOutput(FunctionOutputViewModel? output)
    {
        if (output is not null && Outputs.Remove(output))
        {
            output.StopGeneration();
        }
    }

    public void Start()
    {
        foreach (var output in Outputs)
        {
            output.StartGeneration();
        }
    }

    public void Stop()
    {
        foreach (var output in Outputs)
        {
            output.StopGeneration();
        }
    }

    public override void StopRuntimeActivity() => Stop();

    public override TreeItemNode Copy()
    {
        var copy = new FunctionGeneratorViewModel(_variableHandler);
        Copy(copy);
        copy.Outputs.Clear();
        copy.StartStopPropertyName = StartStopPropertyName;
        foreach (var output in Outputs)
        {
            copy.Outputs.Add(output.Copy(_variableHandler, copy.IsControlProperty, copy.ApplyOutputControl));
        }

        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        new FunctionGeneratorSettings(
            StartStopPropertyName,
            Outputs.Select(output => new FunctionOutputSettings(
                output.PropertyName,
                output.Waveform,
                output.Frequency,
                output.Amplitude,
                output.Offset,
                output.UpdateIntervalMilliseconds,
                output.FrequencyPropertyName,
                output.AmplitudePropertyName,
                output.OffsetPropertyName,
                output.UpdateIntervalPropertyName,
                output.RunningPropertyName)).ToList()));

    public void LoadDocumentData(JsonElement data)
    {
        List<FunctionOutputSettings> settings;
        if (data.ValueKind == JsonValueKind.Array)
        {
            settings = data.Deserialize<List<FunctionOutputSettings>>()
                ?? throw new InvalidDataException("Function generator data is invalid.");
            StartStopPropertyName = string.Empty;
        }
        else
        {
            var generatorSettings = data.Deserialize<FunctionGeneratorSettings>()
                ?? throw new InvalidDataException("Function generator data is invalid.");
            settings = generatorSettings.Outputs
                ?? throw new InvalidDataException("Function generator data is missing outputs.");
            StartStopPropertyName = generatorSettings.StartStopPropertyName ?? string.Empty;
        }

        foreach (var output in settings)
        {
            if (string.IsNullOrWhiteSpace(output.PropertyName) ||
                !Enum.IsDefined(output.Waveform) ||
                !double.IsFinite(output.Frequency) || output.Frequency is <= 0 or > 1000 ||
                !double.IsFinite(output.Amplitude) || !double.IsFinite(output.Offset) ||
                output.UpdateIntervalMilliseconds is < 1 or > 10000)
            {
                throw new InvalidDataException("The document contains invalid function generator settings.");
            }
        }

        foreach (var output in Outputs)
        {
            output.StopGeneration();
        }

        Outputs.Clear();
        foreach (var output in settings)
        {
            Outputs.Add(new FunctionOutputViewModel(_variableHandler, output.PropertyName, IsControlProperty, ApplyOutputControl)
            {
                Waveform = output.Waveform,
                Frequency = output.Frequency,
                Amplitude = output.Amplitude,
                Offset = output.Offset,
                UpdateIntervalMilliseconds = output.UpdateIntervalMilliseconds,
                FrequencyPropertyName = output.FrequencyPropertyName ?? string.Empty,
                AmplitudePropertyName = output.AmplitudePropertyName ?? string.Empty,
                OffsetPropertyName = output.OffsetPropertyName ?? string.Empty,
                UpdateIntervalPropertyName = output.UpdateIntervalPropertyName ?? string.Empty,
                RunningPropertyName = output.RunningPropertyName ?? string.Empty
            });
        }

        _nextOutputNumber = Outputs.Count + 1;
        ApplyStartStopProperty();
        foreach (var output in Outputs) ApplyOutputControl(output);
    }

    private FunctionOutputViewModel CreateOutput()
    {
        string name;
        do
        {
            name = $"Signal{_nextOutputNumber++}";
        }
        while (Outputs.Any(output => string.Equals(output.PropertyName, name, StringComparison.Ordinal)));

        return new FunctionOutputViewModel(_variableHandler, name, IsControlProperty, ApplyOutputControl);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        var version = Volatile.Read(ref _subscriptionVersion);
        void ApplyChange()
        {
            if (_isDetached || version != Volatile.Read(ref _subscriptionVersion))
            {
                return;
            }

            if (change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType())
                RefreshAvailableProperties();
            if (string.Equals(change.Name, StartStopPropertyName.Trim(), StringComparison.Ordinal))
            {
                ApplyStartStopProperty();
            }
            foreach (var output in Outputs)
                if (string.Equals(change.Name, output.RunningPropertyName.Trim(), StringComparison.Ordinal)) ApplyOutputControl(output);
        }

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyChange();
            return;
        }

        dispatcher.BeginInvoke(new Action(ApplyChange));
    }

    private void RefreshAvailableProperties()
    {
        var numericNames = _variableHandler.GetVariableNames()
            .Where(name => _variableHandler.TryGet<double>(name, out _) || _variableHandler.TryGet<int>(name, out _))
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var booleanNames = _variableHandler.GetVariableNames()
            .Where(name => _variableHandler.TryGet<bool>(name, out _))
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        foreach (var name in numericNames.Where(name => !AvailableProperties.Contains(name, StringComparer.Ordinal)))
        {
            AvailableProperties.Add(name);
        }

        foreach (var name in AvailableProperties.Where(name => !numericNames.Contains(name, StringComparer.Ordinal)).ToArray())
        {
            AvailableProperties.Remove(name);
        }

        foreach (var name in booleanNames.Where(name => !BooleanProperties.Contains(name, StringComparer.Ordinal)))
        {
            BooleanProperties.Add(name);
        }

        foreach (var name in BooleanProperties.Where(name => !booleanNames.Contains(name, StringComparer.Ordinal)).ToArray())
        {
            BooleanProperties.Remove(name);
        }
    }

    private void ApplyStartStopProperty()
    {
        var propertyName = StartStopPropertyName.Trim();
        if (propertyName.Length == 0)
        {
            ControlStatus = string.Empty;
            return;
        }

        if (_isDetached) return;
        if (Outputs.Any(output => string.Equals(output.ActiveOutputPropertyName, propertyName, StringComparison.Ordinal)))
        {
            Stop();
            ControlStatus = "An output was writing to the start/stop property; outputs stopped.";
            return;
        }
        if (!_variableHandler.TryGet<bool>(propertyName, out var shouldRun))
        {
            Stop();
            ControlStatus = $"Start/stop property '{propertyName}' was not found or is not Boolean; outputs stopped.";
            return;
        }

        ControlStatus = string.Empty;
        if (shouldRun)
        {
            foreach (var output in Outputs)
            {
                if (string.IsNullOrWhiteSpace(output.RunningPropertyName)) output.StartGeneration();
                else ApplyOutputControl(output);
            }
        }
        else
        {
            Stop();
        }
    }

    private bool IsControlProperty(string name) =>
        string.Equals(name, StartStopPropertyName.Trim(), StringComparison.Ordinal) ||
        Outputs.Any(output => string.Equals(name, output.RunningPropertyName.Trim(), StringComparison.Ordinal));

    private void ApplyOutputControl(FunctionOutputViewModel output)
    {
        if (_isDetached || !Outputs.Contains(output)) return;
        var name = output.RunningPropertyName.Trim();
        if (name.Length == 0) { output.RunningControlStatus = string.Empty; return; }
        foreach (var running in Outputs.Where(item => string.Equals(item.ActiveOutputPropertyName, name, StringComparison.Ordinal)).ToArray())
        {
            running.StopGeneration();
            running.Status = "Stopped: an output cannot write to a start/stop property.";
        }
        if (!_variableHandler.TryGet<bool>(name, out var shouldRun))
        {
            output.StopGeneration();
            output.RunningControlStatus = $"Running property '{name}' was not found or is not Boolean; output stopped.";
            return;
        }
        output.RunningControlStatus = string.Empty;
        var globalName = StartStopPropertyName.Trim();
        var globalAllowsRun = globalName.Length == 0 || (_variableHandler.TryGet<bool>(globalName, out var globalRun) && globalRun);
        if (shouldRun && globalAllowsRun) output.StartGeneration();
        else output.StopGeneration();
    }

    private sealed record FunctionGeneratorSettings(
        string? StartStopPropertyName,
        List<FunctionOutputSettings>? Outputs);

    public sealed record FunctionOutputSettings(
        string PropertyName,
        SignalWaveform Waveform,
        double Frequency,
        double Amplitude,
        double Offset,
        int UpdateIntervalMilliseconds,
        string? FrequencyPropertyName = null,
        string? AmplitudePropertyName = null,
        string? OffsetPropertyName = null,
        string? UpdateIntervalPropertyName = null,
        string? RunningPropertyName = null);
}

public class FunctionGeneratorCreator : TreeItemOption
{
    public FunctionGeneratorCreator(IServiceProvider serviceProvider)
        : base(jsonkey, "FunctionGenerator", "Properties", "Generate waveforms into properties", 
            GetIcon(),
            serviceProvider)
    {
    }

    internal static string jsonkey = "functiongenerator";

    internal static ImageSource GetIcon()
    {
        var uri = new Uri("pack://application:,,,/FunctionGenerator.UI;component/Resources/FunctionGenerator.ico", UriKind.Absolute);

        return new BitmapImage(uri);
    }

}
