using CommunityToolkit.Mvvm.ComponentModel;

namespace VUMeter.UI;

internal sealed class OscilloscopeChannel : ObservableObject
{
    private readonly Action<bool> _changed;
    private string _inputProperty = "";
    private string _color = "Red";
    private string _colorProperty = "";
    private double _amplitude = 1;
    private string _amplitudeProperty = "";
    private double _offset;
    private string _offsetProperty = "";
    private string _status = "";
    /// <summary>
    /// Initializes a new instance of OscilloscopeChannel.
    /// </summary>
    public OscilloscopeChannel(Action<bool> changed) => _changed = changed;
    /// <summary>
    /// Gets or sets the input property.
    /// </summary>
    public string InputProperty { get => _inputProperty; set { if (SetProperty(ref _inputProperty, value ?? "")) _changed(true); } }
    /// <summary>
    /// Gets or sets the channel color; only predefined plot colors are accepted.
    /// </summary>
    public string Color
    {
        get => _color;
        set
        {
            if (!XYPointViewModel.Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(false);
        }
    }
    /// <summary>
    /// Gets or sets the color property.
    /// </summary>
    public string ColorProperty { get => _colorProperty; set { if (SetProperty(ref _colorProperty, value ?? "")) _changed(false); } }
    /// <summary>
    /// Gets or sets the channel amplitude in units per division; values must be finite and within the supported range.
    /// </summary>
    public double Amplitude
    {
        get => _amplitude;
        set
        {
            if (!OscilloscopeViewModel.ValidAmplitude(value)) throw new ArgumentOutOfRangeException(nameof(value), "Amplitude must be between 0.000000001 and 1000000000 units per division.");
            if (SetProperty(ref _amplitude, value)) _changed(false);
        }
    }
    /// <summary>
    /// Gets or sets the amplitude property.
    /// </summary>
    public string AmplitudeProperty { get => _amplitudeProperty; set { if (SetProperty(ref _amplitudeProperty, value ?? "")) _changed(false); } }
    /// <summary>
    /// Gets or sets the channel vertical offset; values must be finite and within the supported range.
    /// </summary>
    public double Offset
    {
        get => _offset;
        set
        {
            if (!OscilloscopeViewModel.ValidLevel(value)) throw new ArgumentOutOfRangeException(nameof(value), "Offset must be finite and between -1e12 and 1e12.");
            if (SetProperty(ref _offset, value)) _changed(false);
        }
    }
    /// <summary>
    /// Gets or sets the offset property.
    /// </summary>
    public string OffsetProperty { get => _offsetProperty; set { if (SetProperty(ref _offsetProperty, value ?? "")) _changed(false); } }
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    private System.Drawing.Color? _effectiveColor;
    /// <summary>
    /// Gets or sets the effective color.
    /// </summary>
    public System.Drawing.Color? EffectiveColor { get => _effectiveColor; set => SetProperty(ref _effectiveColor, value); }
    /// <summary>
    /// Gets or sets the effective amplitude.
    /// </summary>
    public double? EffectiveAmplitude { get; set; }
    /// <summary>
    /// Gets or sets the effective offset.
    /// </summary>
    public double? EffectiveOffset { get; set; }
    /// <summary>
    /// Gets the samples.
    /// </summary>
    public List<ScopeSample> Samples { get; } = [];
}

internal readonly record struct ScopeSample(double Time, double? Value);
