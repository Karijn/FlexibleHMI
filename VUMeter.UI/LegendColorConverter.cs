using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VUMeter.UI;
/// <summary>
/// Converts a System.Drawing color to a WPF brush for legend display.
/// </summary>
public sealed class LegendColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is System.Drawing.Color color
            ? new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
            : Brushes.Transparent;
    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
