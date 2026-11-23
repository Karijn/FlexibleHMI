namespace WpfPropertyGrid;
/// <summary>
/// Provides type-conversion operations through the configured property-grid converter.
/// </summary>
public static class ConversionService
{
    /// <summary>
    /// Attempts to convert the supplied value to the requested type and reports whether conversion succeeded.
    /// </summary>
    public static bool TryConvertType<T>(object? input, out T? value) => TryConvertType(input, null, out value);
    /// <summary>
    /// Attempts to convert the supplied value to the requested type and reports whether conversion succeeded.
    /// </summary>
    public static bool TryConvertType<T>(object? input, IFormatProvider? provider, out T? value)
    {
        var result = PropertyGridServiceProvider.Current.GetService<IConverter>().TryConvert(input, typeof(T), provider, out object? v);
        if (!result)
        {
            if (v == null)
            {
                if (typeof(T).IsValueType)
                {
                    value = Activator.CreateInstance<T>();
                }
                else
                {
                    value = default;
                }
            }
            else
            {
                value = (T)v;
            }
            return false;
        }

        value = (T?)v;
        return result;
    }
    /// <summary>
    /// Attempts to convert the supplied value to the requested runtime type and reports whether conversion succeeded.
    /// </summary>
    public static bool TryConvertObjectType(object? input, Type conversionType, out object? value) => TryConvertObjectType(input, conversionType, null, out value);
    /// <summary>
    /// Attempts to convert the supplied value to the requested runtime type and reports whether conversion succeeded.
    /// </summary>
    public static bool TryConvertObjectType(object? input, Type conversionType, IFormatProvider? provider, out object? value) => PropertyGridServiceProvider.Current.GetService<IConverter>().TryConvert(input, conversionType, provider, out value);
    /// <summary>
    /// Converts the supplied value to the requested type, using the optional fallback when needed.
    /// </summary>
    public static object? ConvertObjectType(object? input, Type conversionType, object? defaultValue = null, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(conversionType);
        if (defaultValue == null && conversionType.IsValueType)
        {
            defaultValue = Activator.CreateInstance(conversionType);
        }

        if (TryConvertObjectType(input, conversionType, provider, out object? value))
            return value;

        return defaultValue;
    }
    /// <summary>
    /// Converts the supplied value to the requested type, returning the optional fallback when needed.
    /// </summary>
    public static T? ConvertType<T>(object? input, T? defaultValue = default, IFormatProvider? provider = null)
    {
        if (TryConvertType(input, provider, out T? value))
            return value;

        return defaultValue;
    }
}
