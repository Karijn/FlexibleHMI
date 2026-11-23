using System.Collections.ObjectModel;

namespace PropertyManager.Interfaces;

/// <summary>Describes a named field in a struct type.</summary>
/// <param name="Name">The case-sensitive field name.</param>
/// <param name="TypeName">The primitive, struct, or array type name.</param>
public sealed record StructFieldDefinition(string Name, string TypeName);

/// <summary>Describes a named struct type and its fields.</summary>
public sealed record StructTypeDefinition
{
    /// <summary>Creates a definition with a snapshot of the supplied fields.</summary>
    /// <param name="name">The type name.</param>
    /// <param name="fields">The field definitions.</param>
    public StructTypeDefinition(string name, IReadOnlyList<StructFieldDefinition> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Name = name;
        Fields = Array.AsReadOnly(fields.ToArray());
    }

    /// <summary>Gets the type name.</summary>
    public string Name { get; }

    /// <summary>Gets the read-only snapshot of field definitions.</summary>
    public IReadOnlyList<StructFieldDefinition> Fields { get; }
}

/// <summary>Represents a document-backed struct property value.</summary>
public sealed record StructValue
{
    /// <summary>Creates a struct value with a snapshot of its field mappings.</summary>
    /// <param name="typeName">The struct type name.</param>
    /// <param name="fields">The field names and values.</param>
    public StructValue(string typeName, IReadOnlyDictionary<string, object?> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        TypeName = typeName;
        Fields = new ReadOnlyDictionary<string, object?>(
            fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal));
    }

    /// <summary>Gets the struct type name.</summary>
    public string TypeName { get; }

    /// <summary>Gets the read-only, case-sensitive field mappings.</summary>
    public IReadOnlyDictionary<string, object?> Fields { get; }

    /// <summary>Returns the type name for display.</summary>
    /// <returns>The struct type name.</returns>
    public override string ToString() => TypeName;
}
