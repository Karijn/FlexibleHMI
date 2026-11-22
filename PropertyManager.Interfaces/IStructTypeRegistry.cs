namespace PropertyManager.Interfaces;

public interface IStructTypeRegistry
{
    event EventHandler? Changed;

    IReadOnlyList<StructTypeDefinition> GetDefinitions();

    void ValidateDefinitions(IEnumerable<StructTypeDefinition> definitions);

    void ReplaceDefinitions(IEnumerable<StructTypeDefinition> definitions);

    void SaveDefinition(StructTypeDefinition definition, string? originalName = null);

    void RemoveDefinition(string name);

    StructValue CreateDefaultValue(string typeName);

    StructValue ParseValue(string typeName, string json);

    StructValue ParseValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions);

    string SerializeValue(StructValue value);

    StructValue UpdateMemberValue(StructValue value, string memberPath, object? memberValue);

    IReadOnlyList<string> FindMemberPaths(StructValue value, string memberName);

    bool IsArrayType(string typeName);

    object CreateDefaultTypedValue(string typeName);

    object ParseTypedValue(string typeName, string json);

    object ParseTypedValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions);

    string SerializeTypedValue(string typeName, object value);
}
