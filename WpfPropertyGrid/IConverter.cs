namespace WpfPropertyGrid;
/// <summary>
/// Defines the service used to convert a value to a requested type.
/// </summary>
public interface IConverter
{
    /// <summary>
    /// Attempts to convert a value to the requested type.
    /// </summary>
    /// <param name="input">The value to convert.</param>
    /// <param name="conversionType">The required result type.</param>
    /// <param name="provider">Culture-specific formatting information, if required.</param>
    /// <param name="value">When this method returns true, contains the converted value; otherwise, contains the default value.</param>
    /// <returns>true if conversion succeeded; otherwise, false.</returns>
    bool TryConvert(object? input, Type conversionType, IFormatProvider? provider, out object? value);
}
