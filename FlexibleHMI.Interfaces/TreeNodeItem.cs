using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;

namespace FlexibleHMI.Interfaces;

/// <summary>Base class for an observable item in the document tree.</summary>
public abstract class TreeItemNode : ObservableObject
{
    /// <summary>Gets the item kind used for document persistence and creation.</summary>
    public abstract string Kind { get; }

    private readonly ObservableCollection<TreeItemNode> _children = new();
    private bool _isCurrent;
    private bool _isExpanded;

    /// <summary>Initializes a document tree item.</summary>
    protected TreeItemNode()
    {
    }

    //    [PropertyGridOptions(SortOrder = 1)]
    /// <summary>Gets or sets the item name.</summary>
    public string Name { get => field; set => SetProperty(ref field, value); } = String.Empty;

    //    [PropertyGridOptions(SortOrder = 2)]
    /// <summary>Gets or sets the item description.</summary>
    public string Description { get => field; set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>Gets the maximum child count, or -1 for no limit.</summary>
    [Browsable(false)]
    public abstract int MaxChildren { get; }

    /// <summary>Gets the child items.</summary>
    [Browsable(false)]
    public ObservableCollection<TreeItemNode> Children { get => _children; }

    /// <summary>Gets or sets the optional item icon.</summary>
    [Browsable(false)]
    public virtual ImageSource? Image { get; set; }

    /// <summary>Gets whether this item is current.</summary>
    [Browsable(false)]
    public bool IsCurrent
    {
        get => _isCurrent;
        private set => SetProperty(ref _isCurrent, value);
    }

    /// <summary>Gets or sets whether the item is expanded in the tree.</summary>
    [Browsable(false)]
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>Gets the parent item, if assigned.</summary>
    [Browsable(false)]
    public TreeItemNode? Parent { get; private set; }

 //   [PropertyGridOptions(SortOrder = 30)]
    /// <summary>Gets a description of the item's child rule.</summary>
    [Category("Base")]
    public abstract string ChildRule { get; }

    /// <summary>Gets whether the item currently has capacity for children.</summary>
    [Browsable(false)]
    public virtual bool CanAcceptChildren => (MaxChildren == -1) || (Children.Count < MaxChildren);

    /// <summary>Sets the current-item state.</summary>
    /// <param name="isCurrent">Whether the item is current.</param>
    public void SetCurrent(bool isCurrent) => IsCurrent = isCurrent;

    /// <summary>Checks whether the given total child count is within the limit.</summary>
    /// <param name="count">The proposed total child count.</param>
    /// <returns>Whether the item can accept the count.</returns>
    [Browsable(false)]
    public bool CanAcceptChildCount(int count) => (this.MaxChildren == -1) ? true : (count <= this.MaxChildren);

    //internal void InsertChild(int index, TreeItemNode child)
    //{
    //    if (!CanAcceptChildCount(_children.Count + 1))
    //    {
    //        throw new InvalidOperationException($"{Name} cannot accept another child.");
    //    }

    //    if (child.Parent is not null)
    //    {
    //        throw new InvalidOperationException($"{child.Name} already belongs to a tree.");
    //    }

    //    _children.Insert(index, child);
    //    child.Parent = this;
    //}

    //internal bool RemoveChild(TreeItemNode child)
    //{
    //    if (!_children.Remove(child))
    //    {
    //        return false;
    //    }

    //    child.Parent = null;
    //    return true;
    //}

    //protected virtual TreeItemNode DeepClone()
    //{
    //    TreeItemNode copy = Copy();
    //    //TreeItemNode clone = this switch
    //    //{
    //    //    LeafItemNode => new LeafItemNode(Name, Description) { Image = this.Image, },
    //    //    SingleChildItemNode => new SingleChildItemNode(Name, Description) { Image = this.Image, },
    //    //    CollectionItemNode => new CollectionItemNode(Name, Description) { Image = this.Image, },
    //    //    GridItemNode => new GridItemNode(Name, Description) { Image = this.Image, },
    //    //    VuMeterNode => new VuMeterNode(Name, Description) { Image = this.Image, },
    //    //    _ => throw new InvalidOperationException($"Unsupported item type: {GetType().Name}.")
    //    //};

    //    foreach (var child in Children)
    //    {
    //        copy.InsertChild(copy.Children.Count, child.DeepClone());
    //    }

    //    return copy;
    //}

    /// <summary>Copies common metadata and recursively copies children into a new item.</summary>
    /// <param name="copy">The destination item.</param>
    /// <returns>The destination item.</returns>
    protected TreeItemNode Copy(TreeItemNode copy)
    {
        copy.Name = Name;
        copy.Description = Description;

        foreach (var child in Children)
        {
            copy.Children.Add(child.Copy());
        }
        return copy;
    }

    /// <summary>Stops runtime activity owned by this item, if any.</summary>
    public virtual void StopRuntimeActivity()
    {
    }

    /// <summary>Creates a copy of the item.</summary>
    /// <returns>The copied item.</returns>
    public abstract TreeItemNode Copy();

}
