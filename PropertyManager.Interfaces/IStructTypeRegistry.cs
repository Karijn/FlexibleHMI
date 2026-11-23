namespace PropertyManager.Interfaces;

/// <summary>Manages named struct definitions and converts typed property values to and from JSON.</summary>
public interface IStructTypeRegistry
{
    /// <summary>Occurs after definitions are replaced, saved, or removed.</summary>
    event EventHandler? Changed;

    /// <summary>Gets a snapshot of the registered definitions.</summary>
    /// <returns>The registered struct definitions.</returns>
    IReadOnlyList<StructTypeDefinition> GetDefinitions();

    /// <summary>Validates a complete set of definitions without changing the registry.</summary>
    /// <param name="definitions">The definitions to validate, including referenced types.</param>
    /// <exception cref="System.IO.InvalidDataException">The definitions are invalid or contain an unsupported recursive cycle.</exception>
    void ValidateDefinitions(IEnumerable<StructTypeDefinition> definitions);

    /// <summary>Validates and replaces all registered definitions.</summary>
    /// <param name="definitions">The complete replacement set.</param>
    /// <remarks>This operation does not migrate existing property values.</remarks>
    void ReplaceDefinitions(IEnumerable<StructTypeDefinition> definitions);

    /// <summary>Adds, updates, or renames a definition and migrates existing struct and array values.</summary>
    /// <param name="definition">The definition to save.</param>
    /// <param name="originalName">The previous name when editing or renaming; otherwise, <see langword="null"/>.</param>
    void SaveDefinition(StructTypeDefinition definition, string? originalName = null);

    /// <summary>Removes a definition if it is not used by another definition or property.</summary>
    /// <param name="name">The case-sensitive type name.</param>
    /// <exception cref="InvalidOperationException">The type is in use.</exception>
    void RemoveDefinition(string name);

    /// <summary>Creates a struct whose fields contain their type's default values.</summary>
    /// <param name="typeName">The registered struct type name.</param>
    /// <returns>The default struct value.</returns>
    StructValue CreateDefaultValue(string typeName);

    /// <summary>Parses a JSON object using the registered struct definitions.</summary>
    /// <param name="typeName">The registered struct type name.</param>
    /// <param name="json">A JSON object matching the definition.</param>
    /// <returns>The parsed struct value.</returns>
    StructValue ParseValue(string typeName, string json);

    /// <summary>Parses a JSON object using a supplied set of definitions without replacing the registry.</summary>
    /// <param name="typeName">The struct type name.</param>
    /// <param name="json">A JSON object matching the definition.</param>
    /// <param name="definitions">The complete set of definitions to use.</param>
    /// <returns>The parsed struct value.</returns>
    StructValue ParseValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions);

    /// <summary>Validates and serializes a struct value as a JSON object.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON object.</returns>
    string SerializeValue(StructValue value);

    /// <summary>Creates a validated struct value with one member replaced.</summary>
    /// <param name="value">The original struct value.</param>
    /// <param name="memberPath">The field path, optionally including nested fields and zero-based array indices.</param>
    /// <param name="memberValue">The replacement value.</param>
    /// <returns>A new struct value containing the replacement.</returns>
    StructValue UpdateMemberValue(StructValue value, string memberPath, object? memberValue);

    /// <summary>Finds member paths in a struct value, including members of populated arrays.</summary>
    /// <param name="value">The struct value to search.</param>
    /// <param name="memberName">An exact path or a field name.</param>
    /// <returns>Exact path matches if present; otherwise, matching field-name paths.</returns>
    IReadOnlyList<string> FindMemberPaths(StructValue value, string memberName);

    /// <summary>Checks whether a type name ends with the array suffix <c>[]</c>.</summary>
    /// <param name="typeName">The type name to inspect.</param>
    /// <returns><see langword="true"/> if the suffix is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This check does not validate the element type.</remarks>
    bool IsArrayType(string typeName);

    /// <summary>Creates a default value for a registered primitive, struct, or array type.</summary>
    /// <param name="typeName">The type name, such as <c>Double</c> or <c>Point[]</c>.</param>
    /// <returns>The default typed value.</returns>
    object CreateDefaultTypedValue(string typeName);

    /// <summary>Parses a JSON value using the registered definitions.</summary>
    /// <param name="typeName">The expected primitive, struct, or array type.</param>
    /// <param name="json">The JSON value to parse.</param>
    /// <returns>The parsed typed value.</returns>
    object ParseTypedValue(string typeName, string json);

    /// <summary>Parses a JSON value using supplied definitions without replacing the registry.</summary>
    /// <param name="typeName">The expected primitive, struct, or array type.</param>
    /// <param name="json">The JSON value to parse.</param>
    /// <param name="definitions">The complete set of definitions to use.</param>
    /// <returns>The parsed typed value.</returns>
    object ParseTypedValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions);

    /// <summary>Validates and serializes a typed value as JSON.</summary>
    /// <param name="typeName">The expected primitive, struct, or array type.</param>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON representation.</returns>
    string SerializeTypedValue(string typeName, object value);
}
