using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Calculator.UI
{
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
}
