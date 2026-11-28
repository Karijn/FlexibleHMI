namespace FlexibleHMI.Interfaces;

/// <summary>Describes a tree node or available item being dragged.</summary>
public sealed class TreeDragData(TreeItemNode? node, TreeItemOption? option)
{
    /// <summary>The existing node, if any.</summary>
    public TreeItemNode? Node { get; } = node;
    /// <summary>The available item, if any.</summary>
    public TreeItemOption? Option { get; } = option;
}
