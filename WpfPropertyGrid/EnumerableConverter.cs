namespace WpfPropertyGrid;
/// <summary>
/// Converts enumerable values to and from values supported by a WPF binding.
/// </summary>
public class EnumerableConverter : DependencyObject, IValueConverter
{
    /// <summary>
    /// Identifies the maximum item count dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxItemsProperty =
        DependencyProperty.Register("MaxItems", typeof(int), typeof(EnumerableConverter), new PropertyMetadata(10));
    /// <summary>
    /// Identifies the item separator dependency property.
    /// </summary>
    public static readonly DependencyProperty SeparatorProperty =
        DependencyProperty.Register("Separator", typeof(string), typeof(EnumerableConverter), new PropertyMetadata(", "));
    /// <summary>
    /// Identifies the item format dependency property.
    /// </summary>
    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register("Format", typeof(string), typeof(EnumerableConverter), new PropertyMetadata("{0}"));
    /// <inheritdoc/>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    /// <inheritdoc/>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(string) && value is not string && value is IEnumerable)
        {
            var sb = new StringBuilder();
            if (value is IEnumerable enumerable)
            {
                foreach (var obj in enumerable)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(Separator);
                    }
                    sb.AppendFormat(Format, obj);
                }
            }
            return sb.ToString();
        }
        return ConversionService.ConvertObjectType(value, targetType, provider: culture);
    }
    /// <summary>
    /// Gets or sets the max items.
    /// </summary>
    public virtual int MaxItems { get => (int)GetValue(MaxItemsProperty); set => SetValue(MaxItemsProperty, value); }
    /// <summary>
    /// Gets or sets the format.
    /// </summary>
    public virtual string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
    /// <summary>
    /// Gets or sets the separator.
    /// </summary>
    public virtual string Separator { get => (string)GetValue(SeparatorProperty); set => SetValue(SeparatorProperty, value); }
}
