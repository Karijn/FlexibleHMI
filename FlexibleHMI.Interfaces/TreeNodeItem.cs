using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;

namespace FlexibleHMI.Interfaces;

public abstract class TreeItemNode : ObservableObject
{
    public abstract string Kind { get; }

    private readonly ObservableCollection<TreeItemNode> _children = new();
    private bool _isCurrent;
    private bool _isExpanded;

    protected TreeItemNode()
    {
    }

    //    [PropertyGridOptions(SortOrder = 1)]
    public string Name { get => field; set => SetProperty(ref field, value); } = String.Empty;

    //    [PropertyGridOptions(SortOrder = 2)]
    public string Description { get => field; set => SetProperty(ref field, value); } = string.Empty;

    [Browsable(false)]
    public abstract int MaxChildren { get; }

    [Browsable(false)]
    public ObservableCollection<TreeItemNode> Children { get => _children; }

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

 //   [PropertyGridOptions(SortOrder = 30)]
    [Category("Base")]
    public abstract string ChildRule { get; }

    [Browsable(false)]
    public virtual bool CanAcceptChildren => (MaxChildren == -1) || (Children.Count < MaxChildren);

    public void SetCurrent(bool isCurrent) => IsCurrent = isCurrent;

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

    public virtual void StopRuntimeActivity()
    {
    }

    public abstract TreeItemNode Copy();

}
