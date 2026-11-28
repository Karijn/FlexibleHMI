using System.Windows.Controls;

namespace Calculator.UI;

/// <summary>
/// WPF view for <see cref="CalculatorViewModel"/>. The host supplies the view model as its data context.
/// </summary>
public partial class CalculatorView : UserControl
{
    /// <summary>Initializes the calculator view and its XAML bindings.</summary>
    public CalculatorView()
    {
        InitializeComponent();
    }
}
