namespace FlexibleHMI.ViewModels;

/// <summary>Specifies where a dragged tree item is inserted relative to a drop target.</summary>
public enum DropPosition
{
    /// <summary>Insert immediately before the target among its siblings.</summary>
    Before,
    /// <summary>Insert as a child of the target, or at the root when no target is specified.</summary>
    Child,
    /// <summary>Insert immediately after the target among its siblings.</summary>
    After
}
