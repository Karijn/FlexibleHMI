using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace VUMeter.UI;

internal sealed partial class OscilloscopeViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly IVariableHandler _variables;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private bool _detached = true;
    private int _version;
    private double _speed = 0.2;
    private string _speedProperty = "";
    private bool _triggerEnabled;
    private string _triggerEnabledProperty = "";
    private string _triggerSource = "";
    private double _triggerLevel;
    private string _triggerLevelProperty = "";
    private bool _risingEdge = true;
    private string _risingEdgeProperty = "";
    private string _status = "";
    private bool _showLegend;
    private string _showLegendProperty = "";
    private SweepSettings? _sweepSettings;
    private double? _previousTrigger;
    private bool _capturing;
    private double _captureStart;

    public OscilloscopeViewModel(IVariableHandler variables)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _dispatcher = Dispatcher.CurrentDispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher) { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) => Sample(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        Name = "Oscilloscope";
        Description = "Sample and display numeric properties over time.";
        Image = TreeItemOption.CreateDiamondImage(System.Windows.Media.Colors.LimeGreen);
        RefreshPropertyNames();
        Channels.Add(new OscilloscopeChannel(ChannelChanged));
    }

    public event EventHandler? DocumentDataChanged;
    public event EventHandler<EventArgs>? PlotChanged;
    public ObservableCollection<string> NumericPropertyNames { get; } = [];
    public ObservableCollection<string> BooleanPropertyNames { get; } = [];
    public ObservableCollection<string> ColorPropertyNames { get; } = [];
    public ObservableCollection<OscilloscopeChannel> Channels { get; } = [];
    public IReadOnlyList<string> Colors => XYPointViewModel.Colors;
    public double DisplayStart { get; private set; }
    public double DisplayDuration { get; private set; } = 2;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool ShowLegend { get => _showLegend; set { if (SetProperty(ref _showLegend, value)) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } } }
    public string ShowLegendProperty { get => _showLegendProperty; set { if (SetProperty(ref _showLegendProperty, value ?? "")) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } } }
    public bool EffectiveShowLegend => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Visible;
    public string LegendStatus => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Status;
    private void NotifyLegendChanged()
    {
        OnPropertyChanged(nameof(EffectiveShowLegend));
        OnPropertyChanged(nameof(LegendStatus));
    }

    public double Speed
    {
        get => _speed;
        set
        {
            if (!ValidSpeed(value)) throw new ArgumentOutOfRangeException(nameof(value), "Speed must be between 0.02 and 6 seconds per division.");
            if (SetProperty(ref _speed, value)) SettingsChanged();
        }
    }
    public string SpeedProperty { get => _speedProperty; set { if (SetProperty(ref _speedProperty, value ?? "")) SettingsChanged(); } }
    public bool TriggerEnabled { get => _triggerEnabled; set { if (SetProperty(ref _triggerEnabled, value)) SettingsChanged(); } }
    public string TriggerEnabledProperty { get => _triggerEnabledProperty; set { if (SetProperty(ref _triggerEnabledProperty, value ?? "")) SettingsChanged(); } }
    public string TriggerSource { get => _triggerSource; set { if (SetProperty(ref _triggerSource, value ?? "")) SettingsChanged(); } }
    public double TriggerLevel
    {
        get => _triggerLevel;
        set
        {
            if (!ValidLevel(value)) throw new ArgumentOutOfRangeException(nameof(value), "Trigger level must be finite and between -1e12 and 1e12.");
            if (SetProperty(ref _triggerLevel, value)) SettingsChanged();
        }
    }
    public string TriggerLevelProperty { get => _triggerLevelProperty; set { if (SetProperty(ref _triggerLevelProperty, value ?? "")) SettingsChanged(); } }
    public bool RisingEdge { get => _risingEdge; set { if (SetProperty(ref _risingEdge, value)) SettingsChanged(); } }
    public string RisingEdgeProperty { get => _risingEdgeProperty; set { if (SetProperty(ref _risingEdgeProperty, value ?? "")) SettingsChanged(); } }

    public override string Kind => OscilloscopeCreator.KindName;
    public override int MaxChildren => 0;
    public override string ChildRule => "No Children here";
    public override bool CanAcceptChildren => false;

    internal static bool ValidSpeed(double value) => double.IsFinite(value) && value is >= 0.02 and <= 6;
    internal static bool ValidAmplitude(double value) => double.IsFinite(value) && value is >= 1e-9 and <= 1e9;
    internal static bool ValidLevel(double value) => double.IsFinite(value) && value is >= -1e12 and <= 1e12;

    [RelayCommand]
    private void AddChannel()
    {
        var channel = new OscilloscopeChannel(ChannelChanged) { Color = Colors[Channels.Count % Colors.Count] };
        Channels.Add(channel);
        SettingsChanged();
    }

    [RelayCommand]
    private void RemoveChannel(OscilloscopeChannel? channel)
    {
        if (channel is not null && Channels.Remove(channel)) SettingsChanged();
    }

    [RelayCommand]
    private void Clear() => ResetSweep();

    public override TreeItemNode Copy()
    {
        var copy = new OscilloscopeViewModel(_variables);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(
        Speed, SpeedProperty, TriggerEnabled, TriggerEnabledProperty, TriggerSource, TriggerLevel, TriggerLevelProperty,
        RisingEdge, RisingEdgeProperty, Channels.Select(c => new ChannelSettings(c.InputProperty, c.Color, c.ColorProperty,
            c.Amplitude, c.AmplitudeProperty, c.Offset, c.OffsetProperty)).ToList(), ShowLegend, ShowLegendProperty));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid oscilloscope settings.");
        if (!ValidSpeed(settings.Speed) || !ValidLevel(settings.TriggerLevel) || settings.SpeedProperty is null ||
            settings.TriggerEnabledProperty is null || settings.TriggerSource is null || settings.TriggerLevelProperty is null ||
            settings.RisingEdgeProperty is null || settings.ShowLegendProperty is null || settings.Channels is null || settings.Channels.Any(c => c is null ||
                c.InputProperty is null || c.ColorProperty is null || c.AmplitudeProperty is null || c.OffsetProperty is null ||
                !XYPointViewModel.Colors.Contains(c.Color, StringComparer.Ordinal) || !ValidAmplitude(c.Amplitude) || !ValidLevel(c.Offset)))
            throw new InvalidDataException("Invalid oscilloscope settings.");
        ShowLegend = settings.ShowLegend;
        ShowLegendProperty = settings.ShowLegendProperty;
        Speed = settings.Speed;
        SpeedProperty = settings.SpeedProperty;
        TriggerEnabled = settings.TriggerEnabled;
        TriggerEnabledProperty = settings.TriggerEnabledProperty;
        TriggerSource = settings.TriggerSource;
        TriggerLevel = settings.TriggerLevel;
        TriggerLevelProperty = settings.TriggerLevelProperty;
        RisingEdge = settings.RisingEdge;
        RisingEdgeProperty = settings.RisingEdgeProperty;
        Channels.Clear();
        foreach (var saved in settings.Channels)
            Channels.Add(new OscilloscopeChannel(ChannelChanged) { InputProperty = saved.InputProperty, Color = saved.Color,
                ColorProperty = saved.ColorProperty, Amplitude = saved.Amplitude, AmplitudeProperty = saved.AmplitudeProperty,
                Offset = saved.Offset, OffsetProperty = saved.OffsetProperty });
        ResetSweep();
    }

    public void OnDocumentItemDetached()
    {
        _detached = true;
        Interlocked.Increment(ref _version);
        _timer.Stop();
        _variables.VariableChanged -= OnVariableChanged;
    }

    public void OnDocumentItemReattached()
    {
        if (!_detached) return;
        _detached = false;
        Interlocked.Increment(ref _version);
        _variables.VariableChanged += OnVariableChanged;
        RefreshPropertyNames();
        NotifyLegendChanged();
        ResetSweep();
        _timer.Start();
    }

    private void ChannelChanged(bool reset)
    {
        if (reset) ResetSweep();
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsChanged()
    {
        ResetSweep();
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetSweep()
    {
        _sweepSettings = null;
        _previousTrigger = null;
        _capturing = false;
        foreach (var channel in Channels) channel.Samples.Clear();
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var namesChanged = change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType();
        if (!namesChanged && !string.Equals(ShowLegendProperty, change.Name, StringComparison.Ordinal)) return;
        var version = Volatile.Read(ref _version);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _version)) return;
            if (namesChanged) RefreshPropertyNames();
            if (string.Equals(ShowLegendProperty, change.Name, StringComparison.Ordinal)) NotifyLegendChanged();
        }
        if (_dispatcher.CheckAccess()) Apply();
        else _dispatcher.BeginInvoke(new Action(Apply));
    }

    private void RefreshPropertyNames()
    {
        RefreshNames(NumericPropertyNames, _variables.GetVariableNames<double>().Concat(_variables.GetVariableNames<int>()));
        RefreshNames(BooleanPropertyNames, _variables.GetVariableNames<bool>());
        RefreshNames(ColorPropertyNames, _variables.GetVariableNames<System.Drawing.Color>());
    }

    private static void RefreshNames(ObservableCollection<string> target, IEnumerable<string> values)
    {
        var names = values.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        for (var i = target.Count - 1; i >= 0; i--) if (!names.Contains(target[i], StringComparer.Ordinal)) target.RemoveAt(i);
        foreach (var name in names) if (!target.Contains(name, StringComparer.Ordinal)) target.Add(name);
    }

    private double? Number(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (_variables.TryGet<double>(name, out var number) && double.IsFinite(number)) return number;
        if (_variables.TryGet<int>(name, out var integer)) return integer;
        return null;
    }

    private double? Number(string binding, double fallback) => string.IsNullOrWhiteSpace(binding) ? fallback : Number(binding);
    private bool? Boolean(string binding, bool fallback) => string.IsNullOrWhiteSpace(binding) ? fallback :
        _variables.TryGet<bool>(binding, out var value) ? value : null;

    private void Sample(double now)
    {
        var speed = Number(SpeedProperty, Speed);
        var enabled = Boolean(TriggerEnabledProperty, TriggerEnabled);
        var rising = Boolean(RisingEdgeProperty, RisingEdge);
        var level = Number(TriggerLevelProperty, TriggerLevel);
        var errors = new List<string>();
        if (speed is null || !ValidSpeed(speed.Value)) errors.Add("Speed must be numeric, from 0.02 to 6 seconds per division.");
        if (enabled is null) errors.Add("Trigger enabled property must be Boolean.");
        if (enabled == true && rising is null) errors.Add("Trigger edge property must be Boolean.");
        if (enabled == true && (level is null || !ValidLevel(level.Value))) errors.Add("Trigger level must be numeric and finite, between -1e12 and 1e12.");
        var trigger = enabled == true ? Number(TriggerSource) : null;
        if (enabled == true && trigger is null) errors.Add("Select an available numeric trigger source.");
        foreach (var channel in Channels) UpdateChannel(channel);
        if (errors.Count != 0)
        {
            if (_sweepSettings is not null) ResetSweep();
            Status = string.Join(" ", errors);
            PlotChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var settings = enabled == true
            ? new SweepSettings(speed!.Value, true, TriggerSource, level!.Value, rising!.Value)
            : new SweepSettings(speed!.Value, false, "", 0, true);
        if (_sweepSettings != settings)
        {
            ResetSweep();
            _sweepSettings = settings;
        }
        DisplayDuration = settings.Speed * 10;
        if (!settings.Enabled)
        {
            DisplayStart = now - DisplayDuration;
            AddSamples(now, DisplayStart);
            Status = "Free running";
        }
        else
        {
            var crossing = _previousTrigger is { } previous && (settings.Rising
                ? previous < settings.Level && trigger >= settings.Level
                : previous > settings.Level && trigger <= settings.Level);
            _previousTrigger = trigger;
            if (!_capturing && crossing)
            {
                foreach (var channel in Channels) channel.Samples.Clear();
                _captureStart = now;
                DisplayStart = now;
                _capturing = true;
            }
            if (_capturing)
            {
                if (now - _captureStart <= DisplayDuration) AddSamples(now, _captureStart);
                else _capturing = false;
            }
            Status = _capturing ? "Triggered sweep" : "Waiting for trigger";
        }
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddSamples(double now, double cutoff)
    {
        foreach (var channel in Channels)
        {
            channel.Samples.Add(new ScopeSample(now, Number(channel.InputProperty)));
            var remove = channel.Samples.FindIndex(sample => sample.Time >= cutoff);
            if (remove < 0) channel.Samples.Clear();
            else if (remove > 0) channel.Samples.RemoveRange(0, remove);
            if (channel.Samples.Count > 4000) channel.Samples.RemoveRange(0, channel.Samples.Count - 4000);
        }
    }

    private void UpdateChannel(OscilloscopeChannel channel)
    {
        var errors = new List<string>();
        if (Number(channel.InputProperty) is null) errors.Add("Select an available finite numeric input.");
        System.Drawing.Color? color = null;
        if (string.IsNullOrWhiteSpace(channel.ColorProperty))
        {
            if (ColorValue.TryParse(channel.Color, out var manual)) color = manual;
        }
        else if (_variables.TryGet<System.Drawing.Color>(channel.ColorProperty, out var boundColor)) color = boundColor;
        if (color is null) errors.Add("Color property must be an available Color.");
        var amplitude = Number(channel.AmplitudeProperty, channel.Amplitude);
        if (amplitude is null || !ValidAmplitude(amplitude.Value)) { errors.Add("Amplitude must be numeric, from 1e-9 to 1e9 units per division."); amplitude = null; }
        var offset = Number(channel.OffsetProperty, channel.Offset);
        if (offset is null || !ValidLevel(offset.Value)) { errors.Add("Offset must be numeric and finite, between -1e12 and 1e12."); offset = null; }
        channel.EffectiveColor = color;
        channel.EffectiveAmplitude = amplitude;
        channel.EffectiveOffset = offset;
        channel.Status = string.Join(" ", errors);
    }

    private readonly record struct SweepSettings(double Speed, bool Enabled, string Source, double Level, bool Rising);
    private sealed record Settings(double Speed, string SpeedProperty, bool TriggerEnabled, string TriggerEnabledProperty,
        string TriggerSource, double TriggerLevel, string TriggerLevelProperty, bool RisingEdge, string RisingEdgeProperty, List<ChannelSettings>? Channels, bool ShowLegend = false, string ShowLegendProperty = "");
    private sealed record ChannelSettings(string InputProperty, string Color, string ColorProperty, double Amplitude,
        string AmplitudeProperty, double Offset, string OffsetProperty);
}
