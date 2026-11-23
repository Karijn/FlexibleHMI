using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

/// <summary>Describes a tree item option backed by a keyed dependency-injection registration.</summary>
/// <param name="kind">The item kind and service key.</param>
/// <param name="name">The display name.</param>
/// <param name="group">The display group.</param>
/// <param name="description">The item description.</param>
/// <param name="image">The item icon.</param>
/// <param name="serviceProvider">The provider used to resolve items.</param>
public class TreeItemOption(string kind, string name, string group, string description, ImageSource image, IServiceProvider serviceProvider) : 
    ITreeItemOption
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public string Kind => kind;

    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public string Group => group;

    /// <inheritdoc />
    public string Description => description;

    /// <inheritdoc />
    public ImageSource Image => image;

    //public virtual TreeItemNode Create() => creator();
    /// <inheritdoc />
    public TreeItemNode Create() => _serviceProvider.GetKeyedService<TreeItemNode>(Kind);

    /// <summary>Returns the display name.</summary>
    /// <returns>The option name.</returns>
    public override string ToString() => Name;

    /// <summary>Creates a frozen diamond-shaped icon.</summary>
    /// <param name="color">The diamond fill color.</param>
    /// <returns>The icon image.</returns>
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

