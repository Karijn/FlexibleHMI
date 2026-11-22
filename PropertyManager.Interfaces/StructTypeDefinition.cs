using System.Collections.ObjectModel;

namespace PropertyManager.Interfaces;

public sealed record StructFieldDefinition(string Name, string TypeName);

public sealed record StructTypeDefinition
{
    public StructTypeDefinition(string name, IReadOnlyList<StructFieldDefinition> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Name = name;
        Fields = Array.AsReadOnly(fields.ToArray());
    }

    public string Name { get; }

    public IReadOnlyList<StructFieldDefinition> Fields { get; }
}

public sealed record StructValue
{
    public StructValue(string typeName, IReadOnlyDictionary<string, object?> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        TypeName = typeName;
        Fields = new ReadOnlyDictionary<string, object?>(
            fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal));
    }

    public string TypeName { get; }

    public IReadOnlyDictionary<string, object?> Fields { get; }

    public override string ToString() => TypeName;
}
