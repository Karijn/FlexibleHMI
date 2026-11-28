namespace WpfPropertyGrid;

/// <summary>
/// Converts a WPF color to a solid brush for property-grid editors.
/// </summary>
public sealed class ColorToBrushConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
            throw new ArgumentException("The value must be a WPF Color.", nameof(value));

        return new SolidColorBrush(color);
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("ColorToBrushConverter is one-way.");
}
