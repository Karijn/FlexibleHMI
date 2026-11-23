using FlexibleHMI.Interfaces;
using System.Windows.Media;

namespace VUMeter.UI;

internal sealed class XYControlCreator(IServiceProvider serviceProvider)
    : TreeItemOption(KindName, "XY Control", "VU Meters", "Plot colored XY points and trails", Icon(), serviceProvider)
{
    internal const string KindName = "xycontrol";
    internal static ImageSource Icon() => CreateDiamondImage(Colors.SteelBlue);
}
