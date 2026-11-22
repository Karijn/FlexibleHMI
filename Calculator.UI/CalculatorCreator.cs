using FlexibleHMI.Interfaces;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Calculator.UI;

internal class CalculatorCreator : TreeItemOption
{
    public CalculatorCreator(IServiceProvider serviceProvider)
        : base(jsonkey, "Calculator", "Properties", "Simple calculator",
            //TreeItemOption.CreateDiamondImage(Colors.DarkOrange), 
            GetIcon(),
            serviceProvider)
    {

    }

    internal static string jsonkey = "calculation";

    internal static ImageSource GetIcon()
    {
        var uri = new Uri("pack://application:,,,/Calculator.UI;component/Resources/Calculator.png", UriKind.Absolute);

        return new BitmapImage(uri);
    }
}