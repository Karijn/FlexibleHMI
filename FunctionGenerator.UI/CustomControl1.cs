using System.Windows;
using System.Windows.Controls;

namespace FunctionGenerator.UI
{
    /// <summary>
    /// Provides the default-style-key hook for the function-generator UI project's placeholder WPF control.
    /// The functional add-in interface is <see cref="FunctionGeneratorView"/>.
    /// </summary>
    public class CustomControl1 : Control
    {
        static CustomControl1()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomControl1), new FrameworkPropertyMetadata(typeof(CustomControl1)));
        }
    }
}