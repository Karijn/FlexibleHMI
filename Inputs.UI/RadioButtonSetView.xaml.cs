using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Inputs.UI;

/// <summary>Displays the radio button set input editor.</summary>
public partial class RadioButtonSetView : UserControl
{
    /// <summary>Creates the radio button set input editor.</summary>
    public RadioButtonSetView() => InitializeComponent();
}

/// <summary>Converts a radio-option and selected-option pair to a checked state.</summary>
public sealed class OptionSelectedConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is string option && values[1] is string selected && option == selected;
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Conversion from selected state to source values is not supported.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
