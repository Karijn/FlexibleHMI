using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommunicationManager.UI;

/// <summary>Creates the Communication Manager option for the document tree.</summary>
/// <param name="serviceProvider">The service provider used by the tree option.</param>
public sealed class CommunicationManagerCreator(IServiceProvider serviceProvider)
    : TreeItemOption(
        DefaultKind,
        "Communication Manager",
        "Communication",
        "Exchange properties with external devices over a serial port",
        GetIcon(), // CreateDiamondImage(Colors.DarkCyan),
        serviceProvider)
{
    /// <summary>Gets the document-tree kind identifier registered for the Communication Manager.</summary>
    public const string DefaultKind = "communicationManager";

    internal static ImageSource GetIcon()
    {
        var uri = new Uri("pack://application:,,,/CommunicationManager.UI;component/Resources/CommunicationManager.png", UriKind.Absolute);

        return new BitmapImage(uri);
    }

}
