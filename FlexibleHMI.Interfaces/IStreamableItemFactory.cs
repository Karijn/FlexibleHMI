namespace FlexibleHMI.Interfaces;

public interface IStreamableItemFactory
{
    IStreamableItem Create();
    Type ObjectType { get; }
}
