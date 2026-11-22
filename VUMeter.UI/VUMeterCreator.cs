using FlexibleHMI.Interfaces;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VUMeter.UI;

internal class VUMeterCreator : TreeItemOption
{
    internal static string jsonkey = "vumeter";

    public VUMeterCreator(IServiceProvider serviceProvider) 
        : base(jsonkey, "Simple VU Meter", "VU Meters", "Simple VU Meter", GetIcon(), serviceProvider)
    {
    }

    internal static ImageSource GetIcon()
    {
        var uri = new Uri("pack://application:,,,/VUMeter.UI;component/Resources/VUMeter.png", UriKind.Absolute);

        return new BitmapImage(uri);
    }
}