using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Inputs.UI;

public partial class InputView : UserControl
{
    public InputView() => InitializeComponent();
}

public sealed class OptionSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is string option && values[1] is string selected && option == selected;
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
