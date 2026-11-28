using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace VUMeter.UI;

internal sealed class LedBarColorStop : ObservableObject
{
    private readonly Action _changed;
    private readonly Func<double, bool> _isValid;
    private double _height;
    private Color _color;

    public LedBarColorStop(double height, Color color, Action changed, Func<double, bool> isValid)
    {
        _height = height;
        _color = color;
        _changed = changed;
        _isValid = isValid;
    }

    public double Height
    {
        get => _height;
        set
        {
            if (!_isValid(value)) throw new ArgumentOutOfRangeException(nameof(value), "Height must be finite and within the current height range.");
            if (SetProperty(ref _height, value)) _changed();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (SetProperty(ref _color, value))
            {
                OnPropertyChanged(nameof(ColorText));
                _changed();
            }
        }
    }

    public string ColorText
    {
        get => _color.ToString();
        set
        {
            object? parsed;
            try { parsed = ColorConverter.ConvertFromString(value?.Trim()); }
            catch (FormatException) { parsed = null; }
            if (parsed is not Color color) throw new FormatException("Enter a color such as #FF8800, #80FF8800 or Orange.");
            Color = color;
        }
    }

    internal void ConvertHeight(double value)
    {
        _height = value;
        OnPropertyChanged(nameof(Height));
    }
}

internal enum LedBarHeightUnit { Percentage, Segment, PropertyValue }
