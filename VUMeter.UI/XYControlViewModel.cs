using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace VUMeter.UI;

internal sealed partial class XYControlViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly IVariableHandler _variables;
    private readonly Dispatcher _dispatcher;
    private bool _detached = true;
    private int _subscriptionVersion;
    private double _minimumX = -100;
    private double _maximumX = 100;
    private double _minimumY = -100;
    private double _maximumY = 100;
    private bool _showLegend;
    private string _showLegendProperty = "";

    public XYControlViewModel(IVariableHandler variables)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _dispatcher = Dispatcher.CurrentDispatcher;
        Name = "XY Control";
        Description = "Plot colored points from numeric properties.";
        Image = XYControlCreator.Icon();
        RefreshPropertyNames();
        Points.Add(new XYPointViewModel(OnPointChanged));
    }

    public event EventHandler? DocumentDataChanged;
    public event EventHandler<EventArgs>? PlotChanged;
    public ObservableCollection<string> NumericPropertyNames { get; } = [];
    public ObservableCollection<string> BooleanPropertyNames { get; } = [];
    public ObservableCollection<string> ColorPropertyNames { get; } = [];
    public ObservableCollection<XYPointViewModel> Points { get; } = [];
    public IReadOnlyList<string> PointColors => XYPointViewModel.Colors;
    public bool ShowLegend { get => _showLegend; set { if (SetProperty(ref _showLegend, value)) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } } }
    public string ShowLegendProperty { get => _showLegendProperty; set { if (SetProperty(ref _showLegendProperty, value ?? "")) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } } }
    public bool EffectiveShowLegend => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Visible;
    public string LegendStatus => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Status;
    private void NotifyLegendChanged()
    {
        OnPropertyChanged(nameof(EffectiveShowLegend));
        OnPropertyChanged(nameof(LegendStatus));
    }

    public double MinimumX { get => _minimumX; set => SetBound(ref _minimumX, value, nameof(MinimumX)); }
    public double MaximumX { get => _maximumX; set => SetBound(ref _maximumX, value, nameof(MaximumX)); }
    public double MinimumY { get => _minimumY; set => SetBound(ref _minimumY, value, nameof(MinimumY)); }
    public double MaximumY { get => _maximumY; set => SetBound(ref _maximumY, value, nameof(MaximumY)); }
    public bool HasValidBounds => MinimumX < MaximumX && MinimumY < MaximumY &&
        double.IsFinite(MaximumX - MinimumX) && double.IsFinite(MaximumY - MinimumY);
    public string BoundsStatus => HasValidBounds ? "" : "Each axis minimum must be less than its maximum and the range must be finite.";

    public override string Kind => XYControlCreator.KindName;
    public override int MaxChildren => 0;
    public override string ChildRule => "No Children here";
    public override bool CanAcceptChildren => false;

    [RelayCommand]
    private void AddPoint()
    {
        var point = new XYPointViewModel(OnPointChanged) { Color = XYPointViewModel.Colors[Points.Count % XYPointViewModel.Colors.Count] };
        Points.Add(point);
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemovePoint(XYPointViewModel? point)
    {
        if (point is not null && Points.Remove(point))
        {
            DocumentDataChanged?.Invoke(this, EventArgs.Empty);
            PlotChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ClearTrails()
    {
        foreach (var point in Points)
        {
            point.ResetPosition();
            UpdatePoint(point);
        }
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    public override TreeItemNode Copy()
    {
        var copy = new XYControlViewModel(_variables);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(
        MinimumX, MaximumX, MinimumY, MaximumY,
        Points.Select(point => new PointSettings(point.XProperty, point.YProperty, point.Color, point.Size, point.ShowTrail,
            point.TrailLength, point.ColorProperty, point.SizeProperty, point.TrailProperty, point.TrailLengthProperty)).ToList(), ShowLegend, ShowLegendProperty));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid XY control settings.");
        if (!double.IsFinite(settings.MinimumX) || !double.IsFinite(settings.MaximumX) ||
            !double.IsFinite(settings.MinimumY) || !double.IsFinite(settings.MaximumY) ||
            settings.ShowLegendProperty is null || settings.Points is null || settings.Points.Any(point => point is null || point.XProperty is null || point.YProperty is null ||
                !XYPointViewModel.Colors.Contains(point.Color, StringComparer.Ordinal) || !double.IsFinite(point.Size) || point.Size is < 1 or > 100 ||
                point.TrailLength is < 1 or > XYPointViewModel.MaximumTrailPoints || point.ColorProperty is null || point.SizeProperty is null ||
                point.TrailProperty is null || point.TrailLengthProperty is null))
        {
            throw new InvalidDataException("Invalid XY control bounds or point settings.");
        }

        ShowLegend = settings.ShowLegend;
        ShowLegendProperty = settings.ShowLegendProperty;
        MinimumX = settings.MinimumX;
        MaximumX = settings.MaximumX;
        MinimumY = settings.MinimumY;
        MaximumY = settings.MaximumY;
        Points.Clear();
        foreach (var saved in settings.Points)
        {
            var point = new XYPointViewModel(OnPointChanged)
            {
                XProperty = saved.XProperty,
                YProperty = saved.YProperty,
                Color = saved.Color,
                Size = saved.Size,
                ShowTrail = saved.ShowTrail,
                TrailLength = saved.TrailLength,
                ColorProperty = saved.ColorProperty,
                SizeProperty = saved.SizeProperty,
                TrailProperty = saved.TrailProperty,
                TrailLengthProperty = saved.TrailLengthProperty
            };
            Points.Add(point);
            UpdateAppearance(point);
            UpdatePoint(point);
        }
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDocumentItemDetached()
    {
        _detached = true;
        Interlocked.Increment(ref _subscriptionVersion);
        _variables.VariableChanged -= OnVariableChanged;
    }

    public void OnDocumentItemReattached()
    {
        if (_detached)
        {
            _detached = false;
            Interlocked.Increment(ref _subscriptionVersion);
            _variables.VariableChanged += OnVariableChanged;
            RefreshPropertyNames();
            NotifyLegendChanged();
            foreach (var point in Points)
            {
                UpdateAppearance(point);
                UpdatePoint(point);
            }
            PlotChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetBound(ref double field, double value, string name)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(name, "Axis limits must be finite.");
        if (SetProperty(ref field, value, name))
        {
            OnPropertyChanged(nameof(HasValidBounds));
            OnPropertyChanged(nameof(BoundsStatus));
            DocumentDataChanged?.Invoke(this, EventArgs.Empty);
            PlotChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPointChanged(XYPointViewModel point, bool reset)
    {
        UpdateAppearance(point);
        if (reset)
        {
            point.ResetPosition();
            UpdatePoint(point);
        }
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
        PlotChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var version = Volatile.Read(ref _subscriptionVersion);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _subscriptionVersion)) return;
            if (change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType())
                RefreshPropertyNames();
            if (string.Equals(ShowLegendProperty, change.Name, StringComparison.Ordinal)) NotifyLegendChanged();
            foreach (var point in Points)
            {
                if (string.Equals(point.ColorProperty, change.Name, StringComparison.Ordinal) ||
                    string.Equals(point.SizeProperty, change.Name, StringComparison.Ordinal) ||
                    string.Equals(point.TrailProperty, change.Name, StringComparison.Ordinal) ||
                    string.Equals(point.TrailLengthProperty, change.Name, StringComparison.Ordinal))
                    UpdateAppearance(point, change);
                var xChanged = string.Equals(point.XProperty, change.Name, StringComparison.Ordinal);
                var yChanged = string.Equals(point.YProperty, change.Name, StringComparison.Ordinal);
                if (xChanged || yChanged)
                {
                    var changedValue = ToNumber(change.NewValue);
                    var previous = point.Position;
                    var x = xChanged ? changedValue : previous?.X ?? ReadNumber(point.XProperty);
                    var y = yChanged ? changedValue : previous?.Y ?? ReadNumber(point.YProperty);
                    point.UpdatePosition(x, y);
                }
            }
            PlotChanged?.Invoke(this, EventArgs.Empty);
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
        var names = values.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        for (var index = target.Count - 1; index >= 0; index--)
            if (!names.Contains(target[index], StringComparer.Ordinal)) target.RemoveAt(index);
        foreach (var name in names)
            if (!target.Contains(name, StringComparer.Ordinal)) target.Add(name);
    }

    private void UpdateAppearance(XYPointViewModel point, VariableChanged? change = null)
    {
        object? Read(string name)
        {
            if (change is not null && string.Equals(name, change.Name, StringComparison.Ordinal)) return change.NewValue;
            if (_variables.TryGet<System.Drawing.Color>(name, out var color)) return color;
            if (_variables.TryGet<bool>(name, out var boolean)) return boolean;
            if (_variables.TryGet<double>(name, out var number)) return number;
            if (_variables.TryGet<int>(name, out var integer)) return integer;
            return null;
        }

        var errors = new List<string>();
        System.Drawing.Color? color = null;
        if (string.IsNullOrWhiteSpace(point.ColorProperty))
        {
            if (ColorValue.TryParse(point.Color, out var parsed)) color = parsed;
        }
        else if (Read(point.ColorProperty) is System.Drawing.Color boundColor) color = boundColor;
        else errors.Add($"Color property '{point.ColorProperty}' must be an available Color.");

        double? size = string.IsNullOrWhiteSpace(point.SizeProperty) ? point.Size : ToNumber(Read(point.SizeProperty));
        if (size is null or < 1 or > 100)
        {
            errors.Add($"Size property '{point.SizeProperty}' must be numeric and between 1 and 100.");
            size = null;
        }

        bool? trail = string.IsNullOrWhiteSpace(point.TrailProperty) ? point.ShowTrail : Read(point.TrailProperty) as bool?;
        if (trail is null) errors.Add($"Trail property '{point.TrailProperty}' must be an available Boolean.");

        var lengthValue = string.IsNullOrWhiteSpace(point.TrailLengthProperty) ? point.TrailLength : ToNumber(Read(point.TrailLengthProperty));
        int? length = null;
        if (lengthValue is >= 1 and <= XYPointViewModel.MaximumTrailPoints && lengthValue == Math.Truncate(lengthValue.Value))
            length = (int)lengthValue.Value;
        else errors.Add($"Trail length property '{point.TrailLengthProperty}' must be an integer from 1 to {XYPointViewModel.MaximumTrailPoints}.");
        point.UpdateAppearance(color, size, trail, length, string.Join(" ", errors));
    }

    private void UpdatePoint(XYPointViewModel point) => point.UpdatePosition(ReadNumber(point.XProperty), ReadNumber(point.YProperty));

    private double? ReadNumber(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (_variables.TryGet<double>(name, out var number) && double.IsFinite(number)) return number;
        if (_variables.TryGet<int>(name, out var integer)) return integer;
        return null;
    }

    private static double? ToNumber(object? value) => value switch
    {
        double number when double.IsFinite(number) => number,
        int integer => integer,
        _ => null
    };

    private sealed record Settings(double MinimumX, double MaximumX, double MinimumY, double MaximumY, List<PointSettings>? Points, bool ShowLegend = false, string ShowLegendProperty = "");
    private sealed record PointSettings(string XProperty, string YProperty, string Color, double Size, bool ShowTrail,
        int TrailLength = 200, string ColorProperty = "", string SizeProperty = "", string TrailProperty = "", string TrailLengthProperty = "");
}
