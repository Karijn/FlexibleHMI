using System.Windows.Controls;

namespace FunctionGenerator.UI;

/// <summary>
/// WPF view for <see cref="FunctionGeneratorViewModel"/>. The host supplies the view model as its data context.
/// </summary>
public partial class FunctionGeneratorView : UserControl
{
    /// <summary>Initializes the function-generator view and its XAML bindings.</summary>
    public FunctionGeneratorView()
    {
        InitializeComponent();
        //DataContext = vm;
    }
}
