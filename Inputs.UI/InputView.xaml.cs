using System.Windows;
using System.Windows.Controls;

namespace Inputs.UI;

/// <summary>Displays the status and interactive control for a property-bound input item.</summary>
public partial class InputView : UserControl
{
    /// <summary>Identifies the template for the input control displayed by this view.</summary>
    public static readonly DependencyProperty InputTemplateProperty = DependencyProperty.Register(
        nameof(InputTemplate), typeof(DataTemplate), typeof(InputView));

    /// <summary>Gets or sets the input control template.</summary>
    public DataTemplate InputTemplate
    {
        get => (DataTemplate)GetValue(InputTemplateProperty);
        set => SetValue(InputTemplateProperty, value);
    }

    /// <summary>Creates and initializes the input editor view.</summary>
    public InputView() => InitializeComponent();
}
