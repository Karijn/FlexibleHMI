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

    public VUMeterInputViewModel(Action<VUMeterInputViewModel> changed) => _changed = changed;

    public string PropertyName
    {
        get => _propertyName;
        set { if (SetProperty(ref _propertyName, value?.Trim() ?? string.Empty)) _changed(this); }
    }

    public string Color
    {
        get => _color;
        set
        {
            if (!XYPointViewModel.Colors.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Select a supported color.");
            if (SetProperty(ref _color, value)) _changed(this);
        }
    }

    public double Angle { get => _angle; private set => SetProperty(ref _angle, value); }
    public bool IsAvailable { get => _isAvailable; private set => SetProperty(ref _isAvailable, value); }
    public bool IsPeakActive { get => _isPeakActive; private set => SetProperty(ref _isPeakActive, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public void Update(double? value, double maximumAngle)
    {
        IsAvailable = value.HasValue;
        Angle = value.HasValue ? Math.Clamp(value.Value, -maximumAngle, maximumAngle) : 0;
        IsPeakActive = value.HasValue && Angle >= maximumAngle - 5;
        Status = value.HasValue ? $"{PropertyName}: {value.Value:0.##}°" :
            string.IsNullOrEmpty(PropertyName) ? "Select a numeric property." : $"Property '{PropertyName}' is unavailable or not numeric.";
    }
}
