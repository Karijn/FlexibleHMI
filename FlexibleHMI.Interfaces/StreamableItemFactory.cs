namespace FlexibleHMI.Interfaces;

/// <summary>Creates streamable items using their parameterless constructor.</summary>
/// <typeparam name="T">The streamable item type.</typeparam>
public class StreamableItemFactory<T> : IStreamableItemFactory
    where T : IStreamableItem, new()
{
    /// <inheritdoc />
    public IStreamableItem Create() => new T();

    /// <inheritdoc />
    public Type ObjectType => typeof(T);
}
