using FlexibleHMI.Interfaces;

namespace FlexibleHMI.ViewModels;

/// <summary>Describes a tree node or available-item option being dragged in the main window.</summary>
/// <param name="node">The existing node being dragged, or <see langword="null"/> when dragging an available item.</param>
/// <param name="option">The available item option being dragged, or <see langword="null"/> when dragging an existing node.</param>
public sealed class TreeDragData(TreeItemNode? node, TreeItemOption? option)
{
    /// <summary>Gets the existing node being dragged, if any.</summary>
    public TreeItemNode? Node { get; } = node;

    /// <summary>Gets the available-item option being dragged, if any.</summary>
    public TreeItemOption? Option { get; } = option;
}
