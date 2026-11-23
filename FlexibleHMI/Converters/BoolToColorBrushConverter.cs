using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;



namespace FlexibleHMI.Converters;

/// <summary>Converts a Boolean value to a configurable true/false solid-color brush for WPF bindings.</summary>
[ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolToColorBrushConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        /// <summary>Converts a Boolean value to a solid brush, optionally using colors and opacity from the converter parameter.</summary>
        /// <param name="value">The Boolean value selecting the true or false color.</param>
        /// <param name="targetType">The requested target type; the result is a <see cref="SolidColorBrush"/>.</param>
        /// <param name="parameter">Optional semicolon-separated true color, false color, and opacity values; defaults to LimeGreen, Transparent, and 1.</param>
        /// <param name="culture">The binding culture (not used by this converter).</param>
        /// <returns>A brush using the color selected by <paramref name="value"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush color;
            // Setting default values
            var colorIfTrue = Colors.LimeGreen;
            var colorIfFalse = Colors.Transparent;
            double opacity = 1;
            // Parsing converter parameter
            if (parameter != null)
            {
                // Parameter format: [ColorNameIfTrue;ColorNameIfFalse;OpacityNumber]
                var parameterstring = parameter.ToString();
                if (!string.IsNullOrEmpty(parameterstring))
                {
                    var parameters = parameterstring.Split(';');
                    var count = parameters.Length;
                    if (count > 0 && !string.IsNullOrEmpty(parameters[0]))
                    {
                        colorIfTrue = ColorFromName(parameters[0]);
                    }
                    if (count > 1 && !string.IsNullOrEmpty(parameters[1]))
                    {
                        colorIfFalse = ColorFromName(parameters[1]);
                    }
                    if (count > 2 && !string.IsNullOrEmpty(parameters[2]))
                    {
                        double dblTemp;
                        if (double.TryParse(parameters[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out dblTemp))
                            opacity = dblTemp;
                    }
                }
            }
            // Creating Color Brush
            if ((bool)value)
            {
                color = new SolidColorBrush(colorIfTrue);
                color.Opacity = opacity;
            }
            else
            {
                color = new SolidColorBrush(colorIfFalse);
                color.Opacity = opacity;
            }
            return color;
        }


        /// <summary>This one-way converter does not support converting a brush back to a Boolean.</summary>
        /// <param name="value">The value produced by <see cref="Convert"/>.</param>
        /// <param name="targetType">The requested source type.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The binding culture.</param>
        /// <returns>This method does not return; it throws <see cref="NotImplementedException"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>Resolves a named color using <see cref="System.Drawing.Color.FromName(string)"/> and converts it to a WPF color.</summary>
        /// <param name="colorName">The color name to resolve.</param>
        /// <returns>The resolved color's ARGB components as a WPF <see cref="Color"/>.</returns>
        public static Color ColorFromName(string colorName)
        {
            System.Drawing.Color systemColor = System.Drawing.Color.FromName(colorName);
            return Color.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
        }
    }