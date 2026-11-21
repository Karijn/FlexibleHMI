namespace FlexibleHMI.Interfaces;

public class StreamableItemFactory<T> : IStreamableItemFactory
    where T : IStreamableItem, new()
{
    public IStreamableItem Create() => new T();

    public Type ObjectType => typeof(T);
}
