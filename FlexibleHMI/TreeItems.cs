using CommunityToolkit.Mvvm.ComponentModel;
using FlexibleHMI.Interfaces;
using System.Windows.Media;


namespace FlexibleHMI;


/// <summary>A tree node that does not accept child items.</summary>
public sealed class LeafItemNode : TreeItemNode 
{
    /// <summary>Creates an empty leaf node.</summary>
    public LeafItemNode() : base()
    {
        Image = CreateDiamondImage(Colors.Red);
    }

    /// <inheritdoc/>
    public override string Kind => "leaf";

    /// <inheritdoc/>
    public override int MaxChildren => 0;

    /// <inheritdoc/>
    public override string ChildRule => "No children";

    /// <inheritdoc/>
    public override bool CanAcceptChildren => false;

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

    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        TreeItemNode copy = Copy(new LeafItemNode());

        return copy;
    }
}

/// <summary>A tree node that accepts at most one child item.</summary>
public sealed class SingleChildItemNode : TreeItemNode
{
    /// <summary>Creates an empty single-child node.</summary>
    public SingleChildItemNode() : base()
    {
        Image = CreateDiamondImage(Colors.Blue);
    }
    /// <inheritdoc/>
    public override string Kind => "singlechild";

    /// <inheritdoc/>
    public override int MaxChildren => 1;

    /// <inheritdoc/>
    public override string ChildRule => "One child maximum";

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

    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        TreeItemNode copy = Copy(new SingleChildItemNode());
        return copy;
    }
}

/// <summary>A tree node that accepts any number of child items.</summary>
public sealed class CollectionItemNode : TreeItemNode
{
    /// <summary>Creates an empty collection node.</summary>
    public CollectionItemNode() : base()
    {
        Image = CreateDiamondImage(Colors.Silver);
    }
    /// <inheritdoc/>
    public override string Kind => "collection";

    /// <inheritdoc/>
    public override int MaxChildren => -1;

    /// <inheritdoc/>
    public override string ChildRule => "Any number of children";

    /// <inheritdoc/>
    public override bool CanAcceptChildren => true;
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

    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        TreeItemNode copy = Copy(new CollectionItemNode());

        return copy;
    }

}

/// <summary>A tree node that accepts any number of child items and represents a grid container.</summary>
public sealed class GridItemNode : TreeItemNode
{
    /// <summary>Creates an empty grid node.</summary>
    public GridItemNode() : base()
    {
        Image = CreateDiamondImage(Colors.Gold);
    }

    /// <inheritdoc/>
    public override string Kind => "grid";

    /// <inheritdoc/>
    public override int MaxChildren => -1;

    /// <inheritdoc/>
    public override string ChildRule => "Any number of children";

    /// <inheritdoc/>
    public override bool CanAcceptChildren => true;
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

    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        TreeItemNode copy = Copy(new GridItemNode());
        return copy;
    }

}

//public sealed partial class VuMeterNode : TreeItemNode
//{
//    public VuMeterNode() : base()
//    {
//        Image = CreateDiamondImage(Colors.DarkBlue);
//    }

//    public override string Kind => "vu meter";

//    public override int MaxChildren => 0;

//    public override string ChildRule => "No children";

//    public override bool CanAcceptChildren => true;

//    private static ImageSource CreateDiamondImage(Color color)
//    {
//        var geometry = Geometry.Parse("M 8,0 L 16,8 L 8,16 L 0,8 Z");
//        geometry.Freeze();

//        var drawing = new GeometryDrawing(new SolidColorBrush(color), null, geometry);
//        drawing.Freeze();

//        var image = new DrawingImage(drawing);
//        image.Freeze();
//        return image;
//    }

//    public override TreeItemNode Copy()
//    {
//        TreeItemNode copy = Copy(new VuMeterNode());
//        return copy;
//    }

//    // Current VU level (range: -20 to +3)
//    [ObservableProperty]
//    private double _currentVuLevel = -20.0;

//    // Needle angle in degrees (mapped from VU level)
//    // The arc spans roughly from -45deg (left, -20 VU) to +45deg (right, +3 VU)
//    [ObservableProperty]
//    private double _needleAngle = -45.0;

//    // Peak LED indicator
//    [ObservableProperty]
//    private bool _isPeakActive = false;

//    // Map VU level to needle angle
//    public void UpdateLevel(double vuLevel)
//    {
//        CurrentVuLevel = vuLevel;

//        // Map -20..+3 VU to -45..+45 degrees
//        // Linear interpolation: angle = ((vuLevel - (-20)) / (3 - (-20))) * 90 - 45
//        NeedleAngle = ((vuLevel + 20.0) / 23.0) * 90.0 - 45.0;

//        // Activate peak LED if level exceeds 0 VU
//        IsPeakActive = vuLevel >= 0.0;
//    }

//}


//public sealed class TreeItemOption(string kind, string name, string group, string description, ImageSource image, Func<TreeItemNode> creator)
//{
//    public string Kind => kind;

//    public string Name => name;

//    public string Group => group;

//    public string Description => description;

//    public ImageSource Image => image;

//    public TreeItemNode Create() => creator();

//    public override string ToString() => Name;


//    public static ImageSource CreateDiamondImage(Color color)
//    {
//        var geometry = Geometry.Parse("M 8,0 L 16,8 L 8,16 L 0,8 Z");
//        geometry.Freeze();

//        var drawing = new GeometryDrawing(new SolidColorBrush(color), null, geometry);
//        drawing.Freeze();

//        var image = new DrawingImage(drawing);
//        image.Freeze();
//        return image;
//    }
//}
