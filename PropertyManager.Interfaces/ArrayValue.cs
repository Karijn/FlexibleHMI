namespace PropertyManager.Interfaces;

public sealed record ArrayValue
{
    public ArrayValue(string elementTypeName, IReadOnlyList<object?> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementTypeName);
        ArgumentNullException.ThrowIfNull(items);
        ElementTypeName = elementTypeName;
        Items = Array.AsReadOnly(items.ToArray());
    }

    public string ElementTypeName { get; }

    public IReadOnlyList<object?> Items { get; }

    public override string ToString() => $"{ElementTypeName}[] ({Items.Count})";
}
