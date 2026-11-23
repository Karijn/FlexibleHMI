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

    public OscilloscopeChannel(Action<bool> changed) => _changed = changed;

    public string InputProperty { get => _inputProperty; set { if (SetProperty(ref _inputProperty, value ?? "")) _changed(true); } }
    public string Color
    {
        get => _color;
        set
        {
            if (!XYPointViewModel.Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(false);
        }
    }
    public string ColorProperty { get => _colorProperty; set { if (SetProperty(ref _colorProperty, value ?? "")) _changed(false); } }
    public double Amplitude
    {
        get => _amplitude;
        set
        {
            if (!OscilloscopeViewModel.ValidAmplitude(value)) throw new ArgumentOutOfRangeException(nameof(value), "Amplitude must be between 0.000000001 and 1000000000 units per division.");
            if (SetProperty(ref _amplitude, value)) _changed(false);
        }
    }
    public string AmplitudeProperty { get => _amplitudeProperty; set { if (SetProperty(ref _amplitudeProperty, value ?? "")) _changed(false); } }
    public double Offset
    {
        get => _offset;
        set
        {
            if (!OscilloscopeViewModel.ValidLevel(value)) throw new ArgumentOutOfRangeException(nameof(value), "Offset must be finite and between -1e12 and 1e12.");
            if (SetProperty(ref _offset, value)) _changed(false);
        }
    }
    public string OffsetProperty { get => _offsetProperty; set { if (SetProperty(ref _offsetProperty, value ?? "")) _changed(false); } }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    private System.Drawing.Color? _effectiveColor;
    public System.Drawing.Color? EffectiveColor { get => _effectiveColor; set => SetProperty(ref _effectiveColor, value); }
    public double? EffectiveAmplitude { get; set; }
    public double? EffectiveOffset { get; set; }
    public List<ScopeSample> Samples { get; } = [];
}

internal readonly record struct ScopeSample(double Time, double? Value);
