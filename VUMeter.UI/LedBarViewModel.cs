using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;

namespace VUMeter.UI;

internal sealed class LedBarViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly IVariableHandler _variables;
    private readonly Dispatcher _dispatcher;
    private bool _detached = true;
    private int _version;
    private readonly Dictionary<int, string> _columnProperties = new();
    private readonly Dictionary<string, int> _levels = new(StringComparer.Ordinal);
    private int _selectedColumn = 1;
    private int _legacyColumnCount;
    private string _legacyPropertyName = "";
    private int _visibleRows;
    private double _fadeMilliseconds = 200;
    private int _displayRevision;
    private int _segmentCount = 10;
    private double _minimum;
    private double _maximum = 100;
    private int _litSegments;
    private string _status = "Select a numeric property.";
    private int _columnCount = 1;
    private Color _baseColor = Colors.LimeGreen;
    private bool _useGradient;
    private LedBarHeightUnit _heightUnit;
    private GradientStopCollection _colorStops = new();

    public LedBarViewModel(IVariableHandler variables)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _dispatcher = Dispatcher.CurrentDispatcher;
        Name = "LED Bar VU Meter";
        Description = "Display a numeric property as an LED bar.";
        Image = TreeItemOption.CreateDiamondImage(Colors.LimeGreen);
        AddColorStopCommand = new RelayCommand(AddColorStop, () => HeightUnit != LedBarHeightUnit.PropertyValue || HasValidRange);
        RemoveColorStopCommand = new RelayCommand<LedBarColorStop>(RemoveColorStop);
        RefreshColors();
        RefreshNames();
    }

    public event EventHandler? DocumentDataChanged;
    public ObservableCollection<string> NumericPropertyNames { get; } = [];
    public ObservableCollection<LedBarColorStop> ColorStops { get; } = [];
    public IReadOnlyList<LedBarHeightUnit> HeightUnits { get; } = Enum.GetValues<LedBarHeightUnit>();
    public IRelayCommand AddColorStopCommand { get; }
    public IRelayCommand<LedBarColorStop> RemoveColorStopCommand { get; }
    public override string Kind => LedBarCreator.KindName;
    public override int MaxChildren => 0;
    public override string ChildRule => "No Children here";
    public override bool CanAcceptChildren => false;

    public int SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (value < 1 || value > ColumnCount) throw new ArgumentOutOfRangeException(nameof(value), "Select an existing column.");
            if (SetProperty(ref _selectedColumn, value))
            {
                OnPropertyChanged(nameof(PropertyName));
                UpdateValue();
            }
        }
    }

    public string PropertyName
    {
        get => GetColumnProperty(SelectedColumn);
        set
        {
            value = value?.Trim() ?? "";
            if (value == PropertyName) return;
            _columnProperties[SelectedColumn] = value;
            OnPropertyChanged();
            SettingsChanged();
        }
    }

    internal string GetColumnProperty(int column) => _columnProperties.TryGetValue(column, out var value)
        ? value : column <= _legacyColumnCount ? _legacyPropertyName : "";

    public int VisibleRows
    {
        get => _visibleRows;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "The number of visible rows cannot be negative.");
            if (SetProperty(ref _visibleRows, value)) SettingsChanged();
        }
    }

    public double FadeMilliseconds
    {
        get => _fadeMilliseconds;
        set
        {
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Fade duration must be a non-negative finite number of milliseconds.");
            if (SetProperty(ref _fadeMilliseconds, value)) SettingsChanged();
        }
    }

    public int DisplayRevision { get => _displayRevision; private set => SetProperty(ref _displayRevision, value); }

    public int SegmentCount
    {
        get => _segmentCount;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "The segment count must be at least 1.");
            if (SetProperty(ref _segmentCount, value)) SettingsChanged();
        }
    }

    public double Minimum
    {
        get => _minimum;
        set
        {
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "The minimum must be finite.");
            if (SetProperty(ref _minimum, value)) SettingsChanged();
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "The maximum must be finite.");
            if (SetProperty(ref _maximum, value)) SettingsChanged();
        }
    }

    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "The column count must be at least 1.");
            if (SetProperty(ref _columnCount, value))
            {
                if (SelectedColumn > value) SelectedColumn = value;
                SettingsChanged();
            }
        }
    }

    public Color BaseColor { get => _baseColor; set { if (SetProperty(ref _baseColor, value)) SettingsChanged(); } }
    public bool UseGradient { get => _useGradient; set { if (SetProperty(ref _useGradient, value)) SettingsChanged(); } }
    public GradientStopCollection DisplayColorStops { get => _colorStops; private set => SetProperty(ref _colorStops, value); }
    public LedBarHeightUnit HeightUnit
    {
        get => _heightUnit;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_heightUnit == value) return;
            if (!HasValidRange && (value == LedBarHeightUnit.PropertyValue || _heightUnit == LedBarHeightUnit.PropertyValue))
                throw new InvalidOperationException("Set a valid range before switching to or from property-value heights.");
            var heights = ColorStops.Select(stop => FromFraction(ToFraction(stop.Height, _heightUnit), value)).ToArray();
            for (var index = 0; index < heights.Length; index++) ColorStops[index].ConvertHeight(heights[index]);
            SetProperty(ref _heightUnit, value);
            SettingsChanged();
        }
    }

    public int LitSegments { get => _litSegments; private set => SetProperty(ref _litSegments, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    private bool HasValidRange => Minimum < Maximum && double.IsFinite(Maximum - Minimum);

    private double ToFraction(double height, LedBarHeightUnit unit) => unit switch
    {
        LedBarHeightUnit.Percentage => Math.Clamp(height / 100, 0, 1),
        LedBarHeightUnit.Segment => Math.Clamp(height / SegmentCount, 0, 1),
        LedBarHeightUnit.PropertyValue => HasValidRange ? (Math.Clamp(height, Minimum, Maximum) - Minimum) / (Maximum - Minimum) : 0,
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private double FromFraction(double fraction, LedBarHeightUnit unit) => unit switch
    {
        LedBarHeightUnit.Percentage => fraction * 100,
        LedBarHeightUnit.Segment => fraction * SegmentCount,
        LedBarHeightUnit.PropertyValue => Minimum + fraction * (Maximum - Minimum),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private bool IsValidHeight(double value) => double.IsFinite(value) && HeightUnit switch
    {
        LedBarHeightUnit.Percentage => value is >= 0 and <= 100,
        LedBarHeightUnit.Segment => value >= 0 && value <= SegmentCount,
        LedBarHeightUnit.PropertyValue => HasValidRange && value >= Minimum && value <= Maximum,
        _ => false
    };

    private void AddColorStop()
    {
        if (HeightUnit == LedBarHeightUnit.PropertyValue && !HasValidRange)
            throw new InvalidOperationException("Set a valid range before adding a color stop.");
        ColorStops.Add(new LedBarColorStop(FromFraction(1, HeightUnit), Colors.Red, SettingsChanged, IsValidHeight));
        SettingsChanged();
    }

    private void RemoveColorStop(LedBarColorStop? stop)
    {
        if (stop is not null && ColorStops.Remove(stop)) SettingsChanged();
    }

    private void RefreshColors()
    {
        var stops = new GradientStopCollection { new GradientStop(BaseColor, 0) };
        foreach (var stop in ColorStops.OrderBy(stop => stop.Height))
            stops.Add(new GradientStop(stop.Color, Math.Clamp(ToFraction(stop.Height, HeightUnit), 0, 1)));
        stops.Freeze();
        DisplayColorStops = stops;
    }

    private void SettingsChanged()
    {
        UpdateValue();
        RefreshLevels();
        RefreshColors();
        DisplayRevision = unchecked(DisplayRevision + 1);
        AddColorStopCommand.NotifyCanExecuteChanged();
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }

    internal int GetColumnLevel(int column) => _levels.TryGetValue(GetColumnProperty(column), out var level) ? level : 0;

    private void RefreshLevels()
    {
        _levels.Clear();
        foreach (var name in _columnProperties.Values.Append(_legacyPropertyName).Where(name => !string.IsNullOrEmpty(name)).Distinct(StringComparer.Ordinal))
            _levels[name] = ResolveLevel(name);
    }

    private int ResolveLevel(string name)
    {
        if (!HasValidRange) return 0;
        double value;
        if (_variables.TryGet<double>(name, out var number) && double.IsFinite(number)) value = number;
        else if (_variables.TryGet<int>(name, out var integer)) value = integer;
        else return 0;
        var fraction = (Math.Clamp(value, Minimum, Maximum) - Minimum) / (Maximum - Minimum);
        return (int)Math.Floor(fraction * SegmentCount);
    }

    private void UpdateValue()
    {
        if (Minimum >= Maximum || !double.IsFinite(Maximum - Minimum))
        {
            LitSegments = 0;
            Status = "Minimum must be less than maximum and the range must be finite.";
            return;
        }

        if (string.IsNullOrEmpty(PropertyName))
        {
            LitSegments = 0;
            Status = "Select a numeric property.";
            return;
        }

        double value;
        if (_variables.TryGet<double>(PropertyName, out var number) && double.IsFinite(number)) value = number;
        else if (_variables.TryGet<int>(PropertyName, out var integer)) value = integer;
        else
        {
            LitSegments = 0;
            Status = $"Property '{PropertyName}' is unavailable or not a finite number.";
            return;
        }

        var fraction = (Math.Clamp(value, Minimum, Maximum) - Minimum) / (Maximum - Minimum);
        LitSegments = (int)Math.Floor(fraction * SegmentCount);
        Status = value < Minimum || value > Maximum ? $"{PropertyName}: {value:0.##} (outside range; display clamped)." : $"{PropertyName}: {value:0.##}";
    }

    private void RefreshNames()
    {
        var names = _variables.GetVariableNames<double>().Concat(_variables.GetVariableNames<int>())
            .Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        for (var index = NumericPropertyNames.Count - 1; index >= 0; index--)
            if (!names.Contains(NumericPropertyNames[index], StringComparer.Ordinal)) NumericPropertyNames.RemoveAt(index);
        foreach (var name in names)
            if (!NumericPropertyNames.Contains(name, StringComparer.Ordinal)) NumericPropertyNames.Add(name);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var version = Volatile.Read(ref _version);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _version)) return;
            if (change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType()) RefreshNames();
            if (string.Equals(PropertyName, change.Name, StringComparison.Ordinal)) UpdateValue();
            if (_levels.ContainsKey(change.Name))
            {
                _levels[change.Name] = ResolveLevel(change.Name);
                DisplayRevision = unchecked(DisplayRevision + 1);
            }
        }
        if (_dispatcher.CheckAccess()) Apply();
        else _dispatcher.BeginInvoke(new Action(Apply));
    }

    public void OnDocumentItemDetached()
    {
        if (_detached) return;
        _detached = true;
        Interlocked.Increment(ref _version);
        _variables.VariableChanged -= OnVariableChanged;
    }

    public void OnDocumentItemReattached()
    {
        if (!_detached) return;
        _detached = false;
        Interlocked.Increment(ref _version);
        _variables.VariableChanged += OnVariableChanged;
        RefreshNames();
        UpdateValue();
        RefreshLevels();
        DisplayRevision = unchecked(DisplayRevision + 1);
    }

    public override TreeItemNode Copy()
    {
        var copy = new LedBarViewModel(_variables);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(_legacyPropertyName, SegmentCount, Minimum, Maximum,
        ColumnCount, BaseColor.ToString(), UseGradient, HeightUnit, ColorStops.Select(stop => new StopSettings(stop.Height, stop.Color.ToString())).ToList(),
        _columnProperties.OrderBy(entry => entry.Key).Select(entry => new ColumnSettings(entry.Key, entry.Value)).ToList(), _legacyColumnCount, VisibleRows, FadeMilliseconds));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid LED bar settings.");
        if (settings.PropertyName is null || settings.SegmentCount < 1 || settings.ColumnCount < 1 || settings.VisibleRows < 0 ||
            !double.IsFinite(settings.FadeMilliseconds) || settings.FadeMilliseconds < 0 || settings.LegacyColumnCount < 0 ||
            !double.IsFinite(settings.Minimum) || !double.IsFinite(settings.Maximum) || !Enum.IsDefined(settings.HeightUnit) ||
            settings.Stops?.Any(stop => stop is null || !double.IsFinite(stop.Height) || stop.Color is null) == true ||
            settings.Columns?.Any(column => column is null || column.Column < 1 || column.PropertyName is null) == true ||
            (settings.Columns is not null && settings.Columns.Select(column => column.Column).Distinct().Count() != settings.Columns.Count))
            throw new InvalidDataException("Invalid LED bar property, column, color stops, fade, or range.");
        Color baseColor;
        Color[] colors;
        try
        {
            baseColor = (Color)ColorConverter.ConvertFromString(settings.BaseColor ?? "LimeGreen")!;
            colors = (settings.Stops ?? []).Select(stop => (Color)ColorConverter.ConvertFromString(stop.Color)!).ToArray();
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or ArgumentException)
        {
            throw new InvalidDataException("Invalid LED bar color.", ex);
        }
        ColorStops.Clear();
        _columnProperties.Clear();
        _legacyPropertyName = settings.PropertyName;
        _legacyColumnCount = settings.Columns is null ? settings.ColumnCount : settings.LegacyColumnCount;
        if (settings.Columns is not null)
            foreach (var column in settings.Columns) _columnProperties.Add(column.Column, column.PropertyName);
        _heightUnit = settings.HeightUnit;
        OnPropertyChanged(nameof(HeightUnit));
        OnPropertyChanged(nameof(PropertyName));
        SegmentCount = settings.SegmentCount;
        Minimum = settings.Minimum;
        Maximum = settings.Maximum;
        ColumnCount = settings.ColumnCount;
        VisibleRows = settings.VisibleRows;
        FadeMilliseconds = settings.FadeMilliseconds;
        BaseColor = baseColor;
        UseGradient = settings.UseGradient;
        var savedStops = settings.Stops ?? [];
        for (var index = 0; index < savedStops.Count; index++)
            ColorStops.Add(new LedBarColorStop(savedStops[index].Height, colors[index], SettingsChanged, IsValidHeight));
        UpdateValue();
        RefreshLevels();
        RefreshColors();
        DisplayRevision = unchecked(DisplayRevision + 1);
    }

    private sealed record Settings(string PropertyName, int SegmentCount, double Minimum, double Maximum,
        int ColumnCount = 1, string? BaseColor = null, bool UseGradient = false, LedBarHeightUnit HeightUnit = LedBarHeightUnit.Percentage, List<StopSettings>? Stops = null,
        List<ColumnSettings>? Columns = null, int LegacyColumnCount = 0, int VisibleRows = 0, double FadeMilliseconds = 200);
    private sealed record StopSettings(double Height, string Color);
    private sealed record ColumnSettings(int Column, string PropertyName);
}
