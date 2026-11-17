using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media;
using WpfPropertyGrid;



namespace FlexibleHMI;

public abstract class TreeItemNode : ObservableObject
{
    private readonly ObservableCollection<TreeItemNode> _children = new();
    private bool _isCurrent;
    private bool _isExpanded;

    protected TreeItemNode(string name, string description)
    {
        Name = name;
        Description = description;
        Children = new ReadOnlyObservableCollection<TreeItemNode>(_children);
    }

    [PropertyGridOptions(SortOrder = 1)]
    public string Name { get => field; set => SetProperty(ref field, value); }

    [PropertyGridOptions(SortOrder = 2)]
    public string Description { get => field; set => SetProperty(ref field, value); }

    [Browsable(false)]
    public abstract int MaxChildren { get; }
    
    [Browsable(false)]
    public ReadOnlyObservableCollection<TreeItemNode> Children { get; }

    [Browsable(false)]
    public virtual ImageSource? Image { get; set; }

    [Browsable(false)]
    public bool IsCurrent
    {
        get => _isCurrent;
        private set => SetProperty(ref _isCurrent, value);
    }

    [Browsable(false)]
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    [Browsable(false)]
    public TreeItemNode? Parent { get; private set; }

    [PropertyGridOptions(SortOrder = 30)]
    [Category("Base")]
    public abstract string ChildRule { get; }

    [Browsable(false)]
    public abstract bool CanAcceptChildren { get; }

    internal void SetCurrent(bool isCurrent) => IsCurrent = isCurrent;

    [Browsable(false)]
    internal bool CanAcceptChildCount(int count) => (this.MaxChildren == -1) ? true : (count <= this.MaxChildren);

    internal void InsertChild(int index, TreeItemNode child)
    {
        if (!CanAcceptChildCount(_children.Count + 1))
        {
            throw new InvalidOperationException($"{Name} cannot accept another child.");
        }

        if (child.Parent is not null)
        {
            throw new InvalidOperationException($"{child.Name} already belongs to a tree.");
        }

        _children.Insert(index, child);
        child.Parent = this;
    }

    internal bool RemoveChild(TreeItemNode child)
    {
        if (!_children.Remove(child))
        {
            return false;
        }

        child.Parent = null;
        return true;
    }

    internal TreeItemNode DeepClone()
    {
        TreeItemNode clone = this switch
        {
            LeafItemNode => new LeafItemNode(Name, Description) { Image = this.Image, },
            SingleChildItemNode => new SingleChildItemNode(Name, Description) { Image = this.Image, },
            CollectionItemNode => new CollectionItemNode(Name, Description) { Image = this.Image, },
            GridItemNode => new GridItemNode(Name, Description) { Image = this.Image, },
            _ => throw new InvalidOperationException($"Unsupported item type: {GetType().Name}.")
        };

        foreach (var child in Children)
        {
            clone.InsertChild(clone.Children.Count, child.DeepClone());
        }

        return clone;
    }
}

public sealed class LeafItemNode : TreeItemNode 
{
    public LeafItemNode(string name, string description) : base(name, description)
    {
        Image = CreateDiamondImage(Colors.Red);
    }

    public override int MaxChildren => 0;

    public override string ChildRule => "No children";

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
}

public sealed class SingleChildItemNode : TreeItemNode
{
    public SingleChildItemNode(string name, string description) : base(name, description)
    {
        Image = CreateDiamondImage(Colors.Blue);
    }

    public override int MaxChildren => 1;

    public override string ChildRule => "One child maximum";

    public override bool CanAcceptChildren => Children.Count == 0;

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

public sealed class CollectionItemNode : TreeItemNode
{
    public CollectionItemNode(string name, string description) : base(name, description)
    {
        Image = CreateDiamondImage(Colors.Silver);
    }
    public override int MaxChildren => -1;

    public override string ChildRule => "Any number of children";

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
}

public sealed class GridItemNode : TreeItemNode
{
    public GridItemNode(string name, string description) : base(name, description)
    {
        Image = CreateDiamondImage(Colors.Gold);
    }

    public override int MaxChildren => -1;

    public override string ChildRule => "Any number of children";

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
}

public enum TreeItemKind
{
    Leaf,
    SingleChild,
    Collection,
    Grid
}

public sealed class TreeItemOption(TreeItemKind kind, string name, string group, string description, ImageSource image)
{
    public TreeItemKind Kind { get; } = kind;

    public string Name { get; } = name;

    public string Group { get; } = group;

    public string Description { get; } = description;

    public ImageSource Image { get; set; } = image;
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
