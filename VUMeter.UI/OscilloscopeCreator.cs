using FlexibleHMI.Interfaces;
using System.Windows.Media;

namespace VUMeter.UI;

internal sealed class OscilloscopeCreator(IServiceProvider serviceProvider)
    : TreeItemOption(KindName, "Oscilloscope", "VU Meters", "Plot multiple sampled signals over time", CreateDiamondImage(Colors.LimeGreen), serviceProvider)
{
    internal const string KindName = "oscilloscope";
}
