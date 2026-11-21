using FlexibleHMI.Interfaces;

namespace FlexibleHMI.ViewModels;

public sealed class TreeDragData(TreeItemNode? node, TreeItemOption? option)
{
    public TreeItemNode? Node { get; } = node;

    public TreeItemOption? Option { get; } = option;
}
