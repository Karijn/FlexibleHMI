namespace WpfPropertyGrid;
/// <summary>
/// Describes one conditional conversion case.
/// </summary>
public class UniversalConverterCase
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public virtual object? Value { get; set; }
    /// <summary>
    /// Gets or sets the converted value.
    /// </summary>
    public virtual object? ConvertedValue { get; set; }
    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public virtual object? MinimumValue { get; set; }
    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public virtual object? MaximumValue { get; set; }
    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    public virtual UniversalConverterOptions Options { get; set; }
    /// <summary>
    /// Gets or sets the operator.
    /// </summary>
    public virtual UniversalConverterOperator Operator { get; set; }
    /// <summary>
    /// Gets or sets the string comparison.
    /// </summary>
    public virtual StringComparison StringComparison { get; set; } = StringComparison.CurrentCultureIgnoreCase;
    /// <summary>
    /// Gets or sets the reverse.
    /// </summary>
    public virtual bool Reverse { get; set; }
    /// <summary>
    /// Determines whether the input matches this conversion case.
    /// </summary>
    public virtual bool Matches(object? value, object? parameter, IFormatProvider? culture)
    {
        var input = new UniversalConverterInput
        {
            MaximumValue = MaximumValue,
            MinimumValue = MinimumValue,
            Operator = Operator,
            Options = Options,
            Value = Value,
            ValueToCompare = value,
            Reverse = Reverse,
            StringComparison = StringComparison,
            ConverterParameter = parameter
        };
        return input.Matches(culture);
    }
}
