using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommunicationManager.UI;

public sealed class CommunicationManagerCreator(IServiceProvider serviceProvider)
    : TreeItemOption(
        DefaultKind,
        "Communication Manager",
        "Communication",
        "Exchange properties with external devices over a serial port",
        GetIcon(), // CreateDiamondImage(Colors.DarkCyan),
        serviceProvider)
{
    public const string DefaultKind = "communicationManager";

    internal static ImageSource GetIcon()
    {
        var uri = new Uri("pack://application:,,,/CommunicationManager.UI;component/Resources/CommunicationManager.png", UriKind.Absolute);

        return new BitmapImage(uri);
    }

}
