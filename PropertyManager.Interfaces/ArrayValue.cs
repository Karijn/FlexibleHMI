namespace PropertyManager.Interfaces;

/// <summary>Represents a document-backed typed array property.</summary>
public sealed record ArrayValue
{
    /// <summary>Creates an array value with a snapshot of the supplied items.</summary>
    /// <param name="elementTypeName">The element type name, which may itself be an array type.</param>
    /// <param name="items">The array items.</param>
    public ArrayValue(string elementTypeName, IReadOnlyList<object?> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementTypeName);
        ArgumentNullException.ThrowIfNull(items);
        ElementTypeName = elementTypeName;
        Items = Array.AsReadOnly(items.ToArray());
    }

    /// <summary>Gets the element type name.</summary>
    public string ElementTypeName { get; }

    /// <summary>Gets the read-only snapshot of items.</summary>
    public IReadOnlyList<object?> Items { get; }

    /// <summary>Returns the element type and item count for display.</summary>
    /// <returns>A display description of the array.</returns>
    public override string ToString() => $"{ElementTypeName}[] ({Items.Count})";
}
