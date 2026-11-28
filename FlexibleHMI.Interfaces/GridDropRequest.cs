using System.Windows;

namespace FlexibleHMI.Interfaces;

/// <summary>A grid container's request to the document host to move or copy an item.</summary>
public sealed class GridDropRequest : RoutedEventArgs
{
    /// <summary>The routed event used to request a document operation.</summary>
    public static readonly RoutedEvent Event = EventManager.RegisterRoutedEvent("GridDrop", RoutingStrategy.Bubble, typeof(EventHandler<GridDropRequest>), typeof(GridDropRequest));

    /// <summary>Creates a grid drop request.</summary>
    public GridDropRequest(TreeDragData data, TreeItemNode target, int row, int column, bool copy) : base(Event)
    {
        Data = data;
        Target = target;
        Row = row;
        Column = column;
        Copy = copy;
    }

    /// <summary>The dragged item.</summary>
    public TreeDragData Data { get; }
    /// <summary>The destination grid.</summary>
    public TreeItemNode Target { get; }
    /// <summary>The destination row.</summary>
    public int Row { get; }
    /// <summary>The destination column.</summary>
    public int Column { get; }
    /// <summary>Whether to copy the item.</summary>
    public bool Copy { get; }
    /// <summary>Whether the host completed the operation.</summary>
    public bool Succeeded { get; set; }
}

/// <summary>Allows the host to position an item after it has been added to a grid.</summary>
public interface IGridDropTarget
{
    /// <summary>Checks whether a cell exists in the grid.</summary>
    bool ContainsCell(int row, int column);
    /// <summary>Gets children covering a cell, excluding the moving child.</summary>
    IReadOnlyList<TreeItemNode> GetChildrenAt(int row, int column, TreeItemNode? exclude);
    /// <summary>Moves a child to a cell and moves its occupant to the child's former position.</summary>
    void SwapChildren(TreeItemNode child, TreeItemNode other, int row, int column);
    /// <summary>Gets the row and column spans of a child.</summary>
    (int RowSpan, int ColumnSpan) GetChildSpan(TreeItemNode child);
    /// <summary>Moves a child to the requested row and column, optionally retaining source spans.</summary>
    void PositionChild(TreeItemNode child, int row, int column, (int RowSpan, int ColumnSpan)? span = null);
}
