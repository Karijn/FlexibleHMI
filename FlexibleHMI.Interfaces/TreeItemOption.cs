using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

public class TreeItemOption(string kind, string name, string group, string description, ImageSource image, IServiceProvider serviceProvider) : 
    ITreeItemOption
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public string Kind => kind;

    public string Name => name;

    public string Group => group;

    public string Description => description;

    public ImageSource Image => image;

    //public virtual TreeItemNode Create() => creator();
    public TreeItemNode Create() => _serviceProvider.GetKeyedService<TreeItemNode>(Kind);

    public override string ToString() => Name;


    public static ImageSource CreateDiamondImage(Color color)
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

