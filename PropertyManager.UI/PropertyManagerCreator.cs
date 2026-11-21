using FlexibleHMI.Interfaces;
using System.Windows.Media;

namespace PropertyManager.UI
{
    public class PropertyManagerCreator : TreeItemOption
    {
        public PropertyManagerCreator(IServiceProvider serviceProvider)
            : base(defaultkind, defaultname, defaultgroup, defaultdescription, defaultimage(), serviceProvider)
        {
        }

        internal const string defaultkind = "propertymanager";
        internal const string defaultname = "property manager";
        internal const string defaultgroup = "Properties";
        internal const string defaultdescription = "a property manager";

        internal static ImageSource defaultimage() => CreateDiamondImage(Colors.BlueViolet);

        private static ImageSource CreateDiamondImage(Color color)
        {
            var geometry = Geometry.Parse("M 8,0 L 16,8 L 8,16 L 0,8 Z");
            geometry.Freeze();

            var drawing = new GeometryDrawing(new SolidColorBrush(color), null, geometry);
            drawing.Freeze();

            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

    }
}