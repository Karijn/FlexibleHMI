namespace WpfPropertyGrid;
/// <summary>
/// Provides WPF binding conversions for property-grid values.
/// </summary>
public class PropertyGridConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) => targetType == null ? value : ConversionService.ConvertObjectType(value, targetType, null, culture);
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var parameterType = GetParameterAsType(parameter);
        if (parameterType != null)
        {
            value = ConversionService.ConvertObjectType(value, parameterType, null, culture);
        }

        return targetType == null ? value : ConversionService.ConvertObjectType(value, targetType, null, culture);
    }

    private static Type? GetParameterAsType(object? parameter)
    {
        if (parameter == null)
            return null;

        var typeName = string.Format("{0}", parameter);
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        return TypeResolutionService.ResolveType(typeName);
    }
}