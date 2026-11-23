using CommunityToolkit.Mvvm.ComponentModel;

namespace VUMeter.UI;

internal sealed class VUMeterInputViewModel : ObservableObject
{
    private readonly Action<VUMeterInputViewModel> _changed;
    private string _propertyName = string.Empty;
    private string _color = "Black";
    private double _angle;
    private bool _isAvailable;
    private bool _isPeakActive;
    private string _status = "Select a numeric property.";
    /// <summary>
    /// Initializes a new instance of VUMeterInputViewModel.
    /// </summary>
    public VUMeterInputViewModel(Action<VUMeterInputViewModel> changed) => _changed = changed;
    /// <summary>
    /// Gets or sets the name of the variable monitored by this meter input.
    /// </summary>
    public string PropertyName
    {
        get => _propertyName;
        set { if (SetProperty(ref _propertyName, value?.Trim() ?? string.Empty)) _changed(this); }
    }
    /// <summary>
    /// Gets or sets the color used to draw this meter input.
    /// </summary>
    public string Color
    {
        get => _color;
        set
        {
            if (!XYPointViewModel.Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(this);
        }
    }
    /// <summary>
    /// Gets the current needle angle, clamped to the meter range.
    /// </summary>
    public double Angle { get => _angle; private set => SetProperty(ref _angle, value); }
    /// <summary>
    /// Gets whether a valid input value is available for display.
    /// </summary>
    public bool IsAvailable { get => _isAvailable; private set => SetProperty(ref _isAvailable, value); }
    /// <summary>
    /// Gets whether the peak indicator is active.
    /// </summary>
    public bool IsPeakActive { get => _isPeakActive; private set => SetProperty(ref _isPeakActive, value); }
    /// <summary>
    /// Gets the input status, including when its variable is unavailable or not numeric.
    /// </summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    /// <summary>
    /// Updates the displayed input state from a resolved value and maximum angle.
    /// </summary>
    public void Update(double? value, double maximumAngle)
    {
        IsAvailable = value.HasValue;
        Angle = value.HasValue ? Math.Clamp(value.Value, -maximumAngle, maximumAngle) : 0;
        IsPeakActive = value.HasValue && Angle >= maximumAngle - 5;
        Status = value.HasValue ? $"{PropertyName}: {value.Value:0.##}°" :
            string.IsNullOrEmpty(PropertyName) ? "Select a numeric property." : $"Property '{PropertyName}' is unavailable or not numeric.";
    }
}
