namespace WpfPropertyGrid;
/// <summary>
/// Converts binding values through the configured property-grid conversion service.
/// </summary>
public class ChangeTypeConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => ConversionService.ConvertObjectType(value, targetType, null, culture);
    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
