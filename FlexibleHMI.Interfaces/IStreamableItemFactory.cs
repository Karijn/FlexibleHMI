namespace FlexibleHMI.Interfaces;

/// <summary>Creates instances of a streamable item type.</summary>
public interface IStreamableItemFactory
{
    /// <summary>Creates an item.</summary>
    /// <returns>The new item.</returns>
    IStreamableItem Create();

    /// <summary>Gets the type created by this factory.</summary>
    Type ObjectType { get; }
}
