using FlexibleHMI.Interfaces;
using System.Windows.Media;

namespace Containers.UI;

internal sealed class GridContainerCreator(IServiceProvider serviceProvider)
    : TreeItemOption(KindName, "Grid Container", "Containers", "Hold controls in configurable rows and columns", CreateDiamondImage(Colors.Goldenrod), serviceProvider)
{
    internal const string KindName = "grid-container";
}
