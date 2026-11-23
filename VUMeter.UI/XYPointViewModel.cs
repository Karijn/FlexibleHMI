using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace VUMeter.UI;

internal sealed class XYPointViewModel : ObservableObject
{
    /// <summary>
    /// The maximum number of historical positions retained for an XY point.
    /// </summary>
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
    /// <summary>
    /// Initializes a new instance of XYPointViewModel.
    /// </summary>
    public XYPointViewModel(Action<XYPointViewModel, bool> changed) => _changed = changed;
    /// <summary>
    /// Gets or sets the colors.
    /// </summary>
    public static IReadOnlyList<string> Colors { get; } = ["Red", "Blue", "Green", "Orange", "Purple", "Black", "Cyan", "Magenta", "Yellow", "White"];
    /// <summary>
    /// Gets or sets the variable name bound to the point X coordinate.
    /// </summary>
    public string XProperty
    {
        get => _xProperty;
        set { if (SetProperty(ref _xProperty, value ?? string.Empty)) _changed(this, true); }
    }
    /// <summary>
    /// Gets or sets the variable name bound to the point Y coordinate.
    /// </summary>
    public string YProperty
    {
        get => _yProperty;
        set { if (SetProperty(ref _yProperty, value ?? string.Empty)) _changed(this, true); }
    }
    /// <summary>
    /// Gets or sets the point color; only predefined plot colors are accepted.
    /// </summary>
    public string Color
    {
        get => _color;
        set
        {
            if (!Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(this, false);
        }
    }
    /// <summary>
    /// Gets or sets the point diameter in pixels; values must be finite and between 1 and 100.
    /// </summary>
    public double Size
    {
        get => _size;
        set
        {
            if (!double.IsFinite(value) || value is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(value), "Point size must be between 1 and 100 pixels.");
            if (SetProperty(ref _size, value)) _changed(this, false);
        }
    }
    /// <summary>
    /// Gets or sets whether the point history is rendered as a trail.
    /// </summary>
    public bool ShowTrail
    {
        get => _showTrail;
        set { if (SetProperty(ref _showTrail, value)) _changed(this, false); }
    }
    /// <summary>
    /// Gets or sets the retained trail length, from 1 through MaximumTrailPoints.
    /// </summary>
    public int TrailLength
    {
        get => _trailLength;
        set
        {
            if (value is < 1 or > MaximumTrailPoints) throw new ArgumentOutOfRangeException(nameof(value), $"Trail length must be between 1 and {MaximumTrailPoints} positions.");
            if (SetProperty(ref _trailLength, value)) _changed(this, false);
        }
    }
    /// <summary>
    /// Gets or sets the color property.
    /// </summary>
    public string ColorProperty { get => _colorProperty; set { if (SetProperty(ref _colorProperty, value ?? string.Empty)) _changed(this, false); } }
    /// <summary>
    /// Gets or sets the variable name bound to the point size.
    /// </summary>
    public string SizeProperty { get => _sizeProperty; set { if (SetProperty(ref _sizeProperty, value ?? string.Empty)) _changed(this, false); } }
    /// <summary>
    /// Gets or sets the variable name bound to trail visibility.
    /// </summary>
    public string TrailProperty { get => _trailProperty; set { if (SetProperty(ref _trailProperty, value ?? string.Empty)) _changed(this, false); } }
    /// <summary>
    /// Gets or sets the variable name bound to trail length.
    /// </summary>
    public string TrailLengthProperty { get => _trailLengthProperty; set { if (SetProperty(ref _trailLengthProperty, value ?? string.Empty)) _changed(this, false); } }
    /// <summary>
    /// Gets or sets the effective color.
    /// </summary>
    public System.Drawing.Color? EffectiveColor { get; private set; } = System.Drawing.Color.FromArgb(unchecked((int)0xFFFF0000));
    /// <summary>
    /// Gets or sets the effective size.
    /// </summary>
    public double? EffectiveSize { get; private set; } = 10;
    /// <summary>
    /// Gets or sets the effective show trail.
    /// </summary>
    public bool EffectiveShowTrail { get; private set; }
    /// <summary>
    /// Gets or sets the effective trail length.
    /// </summary>
    public int? EffectiveTrailLength { get; private set; } = 200;
    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public Point? Position => _position;
    /// <summary>
    /// Gets or sets the trail.
    /// </summary>
    public IReadOnlyList<Point> Trail => _trail;
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    /// <summary>
    /// Gets or sets the appearance status.
    /// </summary>
    public string AppearanceStatus { get => _appearanceStatus; private set => SetProperty(ref _appearanceStatus, value); }
    /// <summary>
    /// Updates resolved point appearance settings and their status.
    /// </summary>
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
    /// <summary>
    /// Clears the point position and its recorded trail.
    /// </summary>
    public void ResetPosition()
    {
        _position = null;
        _trail.Clear();
    }
    /// <summary>
    /// Updates the point position from supplied coordinates and retains valid history.
    /// </summary>
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
