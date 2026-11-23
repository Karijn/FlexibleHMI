using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace VUMeter.UI;

internal sealed class XYPointViewModel : ObservableObject
{
    public const int MaximumTrailPoints = 10000;
    private readonly Action<XYPointViewModel, bool> _changed;
    private string _xProperty = string.Empty;
    private string _yProperty = string.Empty;
    private string _color = "Red";
    private double _size = 10;
    private bool _showTrail;
    private int _trailLength = 200;
    private string _colorProperty = string.Empty;
    private string _sizeProperty = string.Empty;
    private string _trailProperty = string.Empty;
    private string _trailLengthProperty = string.Empty;
    private Point? _position;
    private string _status = "Select X and Y properties.";
    private string _appearanceStatus = string.Empty;
    private readonly List<Point> _trail = [];

    public XYPointViewModel(Action<XYPointViewModel, bool> changed) => _changed = changed;

    public static IReadOnlyList<string> Colors { get; } = ["Red", "Blue", "Green", "Orange", "Purple", "Black", "Cyan", "Magenta", "Yellow", "White"];

    public string XProperty
    {
        get => _xProperty;
        set { if (SetProperty(ref _xProperty, value ?? string.Empty)) _changed(this, true); }
    }

    public string YProperty
    {
        get => _yProperty;
        set { if (SetProperty(ref _yProperty, value ?? string.Empty)) _changed(this, true); }
    }

    public string Color
    {
        get => _color;
        set
        {
            if (!Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(this, false);
        }
    }

    public double Size
    {
        get => _size;
        set
        {
            if (!double.IsFinite(value) || value is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(value), "Point size must be between 1 and 100 pixels.");
            if (SetProperty(ref _size, value)) _changed(this, false);
        }
    }

    public bool ShowTrail
    {
        get => _showTrail;
        set { if (SetProperty(ref _showTrail, value)) _changed(this, false); }
    }

    public int TrailLength
    {
        get => _trailLength;
        set
        {
            if (value is < 1 or > MaximumTrailPoints) throw new ArgumentOutOfRangeException(nameof(value), $"Trail length must be between 1 and {MaximumTrailPoints} positions.");
            if (SetProperty(ref _trailLength, value)) _changed(this, false);
        }
    }

    public string ColorProperty { get => _colorProperty; set { if (SetProperty(ref _colorProperty, value ?? string.Empty)) _changed(this, false); } }
    public string SizeProperty { get => _sizeProperty; set { if (SetProperty(ref _sizeProperty, value ?? string.Empty)) _changed(this, false); } }
    public string TrailProperty { get => _trailProperty; set { if (SetProperty(ref _trailProperty, value ?? string.Empty)) _changed(this, false); } }
    public string TrailLengthProperty { get => _trailLengthProperty; set { if (SetProperty(ref _trailLengthProperty, value ?? string.Empty)) _changed(this, false); } }

    public System.Drawing.Color? EffectiveColor { get; private set; } = System.Drawing.Color.FromArgb(unchecked((int)0xFFFF0000));
    public double? EffectiveSize { get; private set; } = 10;
    public bool EffectiveShowTrail { get; private set; }
    public int? EffectiveTrailLength { get; private set; } = 200;
    public Point? Position => _position;
    public IReadOnlyList<Point> Trail => _trail;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string AppearanceStatus { get => _appearanceStatus; private set => SetProperty(ref _appearanceStatus, value); }

    public void UpdateAppearance(System.Drawing.Color? color, double? size, bool? showTrail, int? trailLength, string status)
    {
        if (EffectiveColor != color)
        {
            EffectiveColor = color;
            OnPropertyChanged(nameof(EffectiveColor));
        }
        EffectiveSize = size;
        EffectiveTrailLength = trailLength;
        var enabled = showTrail == true && trailLength.HasValue;
        if (enabled != EffectiveShowTrail)
        {
            _trail.Clear();
            if (enabled && _position is { } position) _trail.Add(position);
        }
        EffectiveShowTrail = enabled;
        if (trailLength.HasValue && _trail.Count > trailLength.Value)
            _trail.RemoveRange(0, _trail.Count - trailLength.Value);
        AppearanceStatus = status;
    }

    public void ResetPosition()
    {
        _position = null;
        _trail.Clear();
    }

    public void UpdatePosition(double? x, double? y)
    {
        if (x is null || y is null)
        {
            _position = null;
            _trail.Clear();
            Status = "Select available, finite numeric X and Y properties.";
            return;
        }

        var position = new Point(x.Value, y.Value);
        if (_position != position)
        {
            _position = position;
            if (EffectiveShowTrail && EffectiveTrailLength is { } length)
            {
                _trail.Add(position);
                if (_trail.Count > length) _trail.RemoveAt(0);
            }
        }

        Status = $"X: {x.Value:0.###}, Y: {y.Value:0.###}";
    }
}
