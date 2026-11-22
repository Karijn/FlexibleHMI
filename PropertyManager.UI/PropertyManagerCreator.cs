using FlexibleHMI.Interfaces;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PropertyManager.UI
{
    public class PropertyManagerCreator : TreeItemOption
    {
        public PropertyManagerCreator(IServiceProvider serviceProvider)
            : base(jsonkey, defaultname, defaultgroup, defaultdescription, GetIcon(), serviceProvider)
        {
        }

        internal const string jsonkey = "propertymanager";
        internal const string defaultname = "property manager";
        internal const string defaultgroup = "Properties";
        internal const string defaultdescription = "a property manager";

        internal static ImageSource defaultimage() => GetIcon(); //  CreateDiamondImage(Colors.BlueViolet);

        internal static ImageSource GetIcon()
        {
            var uri = new Uri("pack://application:,,,/PropertyManager.UI;component/Resources/Properties.png", UriKind.Absolute);

            return new BitmapImage(uri);
        }
    }
}