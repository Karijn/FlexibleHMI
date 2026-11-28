using System.Globalization;
using System.Windows.Data;

namespace Containers.UI;

/// <summary>Maps zero to WPF's automatic size.</summary>
public sealed class GridDimensionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var size = value is double number ? number : 0;
        return parameter is string ? (size > 0 ? 0d : 80d) : (size > 0 ? size : double.NaN);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
