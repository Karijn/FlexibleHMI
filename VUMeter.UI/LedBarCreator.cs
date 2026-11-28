using FlexibleHMI.Interfaces;
using System.Windows.Media;

namespace VUMeter.UI;

internal sealed class LedBarCreator(IServiceProvider serviceProvider)
    : TreeItemOption(KindName, "LED Bar VU Meter", "VU Meters", "Display a numeric property as an LED bar", CreateDiamondImage(Colors.LimeGreen), serviceProvider)
{
    internal const string KindName = "ledbar-vumeter";
}
