using PropertyManager.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace PropertyManager;

public sealed class StructTypeRegistry : IStructTypeRegistry
{
    private static readonly HashSet<string> PrimitiveTypes = ["Integer", "Double", "Boolean"];
    private readonly IVariableHandler _variableHandler;
    private readonly object _syncRoot = new();
    private Dictionary<string, StructTypeDefinition> _definitions = new(StringComparer.Ordinal);

    public StructTypeRegistry(IVariableHandler variableHandler)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
    }

    public event EventHandler? Changed;

    public IReadOnlyList<StructTypeDefinition> GetDefinitions()
    {
        lock (_syncRoot)
        {
            return _definitions.Values.ToArray();
        }
    }

    public void ValidateDefinitions(IEnumerable<StructTypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _ = CreateDefinitionMap(definitions);
    }

    public void ReplaceDefinitions(IEnumerable<StructTypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var replacement = CreateDefinitionMap(definitions);
        lock (_syncRoot)
        {
            _definitions = replacement;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SaveDefinition(StructTypeDefinition definition, string? originalName = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException("A struct type must have a name.");
        }

        if (definition.Fields is null || definition.Fields.Any(field =>
                field is null || string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.TypeName)))
        {
            throw new InvalidDataException("A struct type contains an invalid field.");
        }

        var normalizedName = definition.Name.Trim();
        var normalizedDefinition = new StructTypeDefinition(
            normalizedName,
            definition.Fields.Select(field => new StructFieldDefinition(field.Name.Trim(), field.TypeName.Trim())).ToArray());
        var oldName = originalName?.Trim();
        var wasRenamed = oldName is not null && !string.Equals(oldName, normalizedName, StringComparison.Ordinal);
        Dictionary<string, StructTypeDefinition> updatedDefinitions;
        lock (_syncRoot)
        {
            updatedDefinitions = new Dictionary<string, StructTypeDefinition>(_definitions, StringComparer.Ordinal);
        }

        if (wasRenamed)
        {
            if (!updatedDefinitions.Remove(oldName!, out var oldDefinition))
            {
                throw new InvalidOperationException($"Struct type '{oldName}' no longer exists.");
            }

            if (updatedDefinitions.ContainsKey(normalizedName))
            {
                throw new InvalidOperationException($"Struct type '{normalizedName}' already exists.");
            }

            foreach (var existing in updatedDefinitions.Values.ToArray())
            {
                updatedDefinitions[existing.Name] = new StructTypeDefinition(
                    existing.Name,
                    existing.Fields.Select(field => RewriteReferencedType(field, oldName!, normalizedName)).ToArray());
            }

            normalizedDefinition = new StructTypeDefinition(
                normalizedDefinition.Name,
                normalizedDefinition.Fields.Select(field => RewriteReferencedType(field, oldName!, normalizedName)).ToArray());
        }

        updatedDefinitions[normalizedName] = normalizedDefinition;
        updatedDefinitions = CreateDefinitionMap(updatedDefinitions.Values);
        lock (_syncRoot)
        {
            _definitions = updatedDefinitions;
        }

        foreach (var name in _variableHandler.GetVariableNames())
        {
            if (_variableHandler.TryGet<StructValue>(name, out var value) && value is not null)
            {
                var renamedValue = wasRenamed ? RenameType(value, oldName!, normalizedName) : value;
                _variableHandler.Set(name, MigrateValue(renamedValue, updatedDefinitions));
            }
            else if (_variableHandler.TryGet<ArrayValue>(name, out var array) && array is not null)
            {
                var renamedArray = wasRenamed ? RenameArrayTypes(array, oldName!, normalizedName) : array;
                if (IsArrayTypeName($"{renamedArray.ElementTypeName}[]"))
                {
                    _variableHandler.Set(name, MigrateArray(renamedArray, $"{renamedArray.ElementTypeName}[]", updatedDefinitions));
                }
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Dictionary<string, StructTypeDefinition> updatedDefinitions;
        lock (_syncRoot)
        {
            if (!_definitions.ContainsKey(name))
            {
                return;
            }

            if (_definitions.Values.Any(definition =>
                    !string.Equals(definition.Name, name, StringComparison.Ordinal) &&
                    definition.Fields.Any(field => string.Equals(GetBaseTypeName(field.TypeName), name, StringComparison.Ordinal))))
            {
                throw new InvalidOperationException($"Struct type '{name}' is used by another struct type.");
            }

            updatedDefinitions = new Dictionary<string, StructTypeDefinition>(_definitions, StringComparer.Ordinal);
            updatedDefinitions.Remove(name);
        }

        foreach (var variableName in _variableHandler.GetVariableNames())
        {
            var usesType = _variableHandler.TryGet<StructValue>(variableName, out var value) &&
                value is not null && ContainsType(value, name);
            usesType |= _variableHandler.TryGet<ArrayValue>(variableName, out var array) &&
                array is not null && ContainsType(array, name);
            if (usesType)
            {
                throw new InvalidOperationException($"Struct type '{name}' is used by property '{variableName}'.");
            }
        }

        lock (_syncRoot)
        {
            _definitions = updatedDefinitions;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public StructValue CreateDefaultValue(string typeName)
    {
        var definitions = GetDefinitionMap();
        return CreateDefaultValue(typeName, definitions, new HashSet<string>(StringComparer.Ordinal));
    }

    public StructValue ParseValue(string typeName, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        return ParseValue(typeName, document.RootElement, GetDefinitionMap());
    }

    public StructValue ParseValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var definitionMap = CreateDefinitionMap(definitions);
        using var document = JsonDocument.Parse(json);
        return ParseValue(typeName, document.RootElement, definitionMap);
    }

    public string SerializeValue(StructValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var definitions = GetDefinitionMap();
        ValidateValue(value, definitions);
        return JsonSerializer.Serialize(ToSerializableFields(value, definitions));
    }

    public StructValue UpdateMemberValue(StructValue value, string memberPath, object? memberValue)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberPath);
        var definitions = GetDefinitionMap();
        var updated = UpdateTypedMember(value.TypeName, value, memberPath, memberValue, definitions);
        if (updated is not StructValue updatedStruct)
        {
            throw new InvalidDataException($"Member path '{memberPath}' does not resolve to a struct.");
        }

        ValidateValue(updatedStruct, definitions);
        return updatedStruct;
    }

    public IReadOnlyList<string> FindMemberPaths(StructValue value, string memberName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        var definitions = GetDefinitionMap();
        var exactPaths = new List<string>();
        var matchingFieldNames = new List<string>();
        FindMemberPaths(value.TypeName, value, string.Empty, memberName, definitions, exactPaths, matchingFieldNames);
        return exactPaths.Count > 0 ? exactPaths.Distinct(StringComparer.Ordinal).ToArray() :
            matchingFieldNames.Distinct(StringComparer.Ordinal).ToArray();
    }

    public bool IsArrayType(string typeName) => IsArrayTypeName(typeName);

    public object CreateDefaultTypedValue(string typeName)
    {
        var definitions = GetDefinitionMap();
        ValidateTypeName(typeName, definitions);
        return CreateDefaultTypedValue(typeName, definitions, new HashSet<string>(StringComparer.Ordinal));
    }

    public object ParseTypedValue(string typeName, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        var definitions = GetDefinitionMap();
        ValidateTypeName(typeName, definitions);
        return ParseTypedValue(typeName, document.RootElement, definitions);
    }

    public object ParseTypedValue(string typeName, string json, IEnumerable<StructTypeDefinition> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(definitions);
        var definitionMap = CreateDefinitionMap(definitions);
        ValidateTypeName(typeName, definitionMap);
        using var document = JsonDocument.Parse(json);
        return ParseTypedValue(typeName, document.RootElement, definitionMap);
    }

    public string SerializeTypedValue(string typeName, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var definitions = GetDefinitionMap();
        ValidateTypeName(typeName, definitions);
        var serializableValue = ToSerializableTypedValue(typeName, value, definitions);
        _ = ParseTypedValue(typeName, JsonSerializer.SerializeToElement(serializableValue), definitions);
        return JsonSerializer.Serialize(serializableValue);
    }

    private static Dictionary<string, StructTypeDefinition> CreateDefinitionMap(IEnumerable<StructTypeDefinition> definitions)
    {
        var result = new Dictionary<string, StructTypeDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (definition is null || string.IsNullOrWhiteSpace(definition.Name) || definition.Fields is null)
            {
                throw new InvalidDataException("A struct type must have a name and a fields collection.");
            }

            if (definition.Fields.Any(field =>
                    field is null || string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.TypeName)))
            {
                throw new InvalidDataException($"Struct type '{definition.Name}' contains an invalid field.");
            }

            var typeName = definition.Name.Trim();
            if (PrimitiveTypes.Contains(typeName) || typeName.IndexOfAny(['[', ']']) >= 0)
            {
                throw new InvalidDataException($"'{typeName}' is a reserved or invalid struct type name.");
            }

            var normalizedFields = definition.Fields
                .Select(field => new StructFieldDefinition(field.Name.Trim(), field.TypeName.Trim()))
                .ToArray();
            if (!result.TryAdd(typeName, new StructTypeDefinition(typeName, normalizedFields)))
            {
                throw new InvalidDataException($"Struct type '{typeName}' is defined more than once.");
            }
        }

        foreach (var definition in result.Values)
        {
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in definition.Fields)
            {
                if (field is null || string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.TypeName))
                {
                    throw new InvalidDataException($"Struct type '{definition.Name}' contains an invalid field.");
                }

                if (!fieldNames.Add(field.Name.Trim()))
                {
                    throw new InvalidDataException($"Struct type '{definition.Name}' contains duplicate field '{field.Name}'.");
                }

                var baseTypeName = GetBaseTypeName(field.TypeName);
                if (!IsValidTypeName(field.TypeName) ||
                    !PrimitiveTypes.Contains(baseTypeName) && !result.ContainsKey(baseTypeName))
                {
                    throw new InvalidDataException($"Field '{field.Name}' references unknown type '{field.TypeName}'.");
                }
            }
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var currentPath = new HashSet<string>(StringComparer.Ordinal);
        foreach (var typeName in result.Keys)
        {
            ValidateAcyclic(typeName, result, visited, currentPath);
        }

        return result;
    }

    private static void ValidateAcyclic(
        string typeName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions,
        HashSet<string> visited,
        HashSet<string> currentPath)
    {
        if (visited.Contains(typeName))
        {
            return;
        }

        if (!currentPath.Add(typeName))
        {
            throw new InvalidDataException($"Struct type '{typeName}' creates a recursive type definition.");
        }

        foreach (var field in definitions[typeName].Fields.Where(field =>
                     !IsArrayTypeName(field.TypeName) && definitions.ContainsKey(field.TypeName)))
        {
            ValidateAcyclic(field.TypeName, definitions, visited, currentPath);
        }

        currentPath.Remove(typeName);
        visited.Add(typeName);
    }

    private Dictionary<string, StructTypeDefinition> GetDefinitionMap()
    {
        lock (_syncRoot)
        {
            return new Dictionary<string, StructTypeDefinition>(_definitions, StringComparer.Ordinal);
        }
    }

    private static object CreateDefaultTypedValue(
        string typeName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions,
        HashSet<string> currentPath)
    {
        if (IsArrayTypeName(typeName))
        {
            return new ArrayValue(typeName[..^2], []);
        }

        return typeName switch
        {
            "Integer" => 0,
            "Double" => 0d,
            "Boolean" => false,
            _ => CreateDefaultValue(typeName, definitions, currentPath)
        };
    }

    private static StructValue CreateDefaultValue(
        string typeName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions,
        HashSet<string> currentPath)
    {
        if (!definitions.TryGetValue(typeName, out var definition))
        {
            throw new InvalidOperationException($"Struct type '{typeName}' is not defined.");
        }

        if (!currentPath.Add(typeName))
        {
            throw new InvalidOperationException($"Struct type '{typeName}' is recursive.");
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            fields.Add(field.Name, CreateDefaultTypedValue(field.TypeName, definitions, currentPath));
        }

        currentPath.Remove(typeName);
        return new StructValue(typeName, fields);
    }

    private static StructValue ParseValue(
        string typeName,
        JsonElement element,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (!definitions.TryGetValue(typeName, out var definition) || element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Value for struct type '{typeName}' must be an object with a defined type.");
        }

        var expectedNames = definition.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var jsonField in element.EnumerateObject())
        {
            if (!expectedNames.Contains(jsonField.Name))
            {
                throw new InvalidDataException($"Struct type '{typeName}' does not define field '{jsonField.Name}'.");
            }
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            if (!element.TryGetProperty(field.Name, out var value))
            {
                throw new InvalidDataException($"Struct value of type '{typeName}' is missing field '{field.Name}'.");
            }

            fields.Add(field.Name, ParseTypedValue(field.TypeName, value, definitions));
        }

        return new StructValue(typeName, fields);
    }

    private static object ParseTypedValue(
        string typeName,
        JsonElement value,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (IsArrayTypeName(typeName))
        {
            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException($"Value for array type '{typeName}' must be a JSON array.");
            }

            var elementTypeName = typeName[..^2];
            return new ArrayValue(elementTypeName,
                value.EnumerateArray().Select(item => ParseTypedValue(elementTypeName, item, definitions)).ToArray());
        }

        if (typeName == "Boolean")
        {
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (BooleanValueParser.TryParse(text, out var booleanAlias))
                {
                    return booleanAlias;
                }
            }

            throw new InvalidDataException("Value is invalid for type 'Boolean'.");
        }

        return typeName switch
        {
            "Integer" when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer) => integer,
            "Double" when value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number) => number,
            _ when definitions.ContainsKey(typeName) => ParseValue(typeName, value, definitions),
            _ => throw new InvalidDataException($"Value is invalid for type '{typeName}'.")
        };
    }

    private static object ToSerializableFields(
        StructValue value,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (!definitions.TryGetValue(value.TypeName, out var definition))
        {
            throw new InvalidDataException($"Struct type '{value.TypeName}' is not defined.");
        }

        var expectedNames = definition.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        if (value.Fields.Keys.Any(fieldName => !expectedNames.Contains(fieldName)))
        {
            throw new InvalidDataException($"Struct value of type '{value.TypeName}' contains an undefined field.");
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            if (!value.Fields.TryGetValue(field.Name, out var fieldValue))
            {
                throw new InvalidDataException($"Struct value of type '{value.TypeName}' is missing field '{field.Name}'.");
            }

            fields.Add(field.Name, ToSerializableTypedValue(field.TypeName, fieldValue, definitions));
        }

        return fields;
    }

    private static void ValidateValue(StructValue value, IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        var serializedFields = ToSerializableFields(value, definitions);
        _ = ParseValue(value.TypeName, JsonSerializer.SerializeToElement(serializedFields), definitions);
    }

    private static StructValue MigrateValue(
        StructValue oldValue,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (!definitions.TryGetValue(oldValue.TypeName, out var definition))
        {
            return oldValue;
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            if (oldValue.Fields.TryGetValue(field.Name, out var oldFieldValue))
            {
                if (field.TypeName is "Integer" or "Double" or "Boolean")
                {
                    if (oldFieldValue is not null && MatchesPrimitiveType(field.TypeName, oldFieldValue))
                    {
                        fields[field.Name] = oldFieldValue;
                        continue;
                    }
                }
                else if (IsArrayTypeName(field.TypeName) && oldFieldValue is ArrayValue array &&
                         array.ElementTypeName == field.TypeName[..^2])
                {
                    fields[field.Name] = MigrateArray(array, field.TypeName, definitions);
                    continue;
                }
                else if (oldFieldValue is StructValue nested && nested.TypeName == field.TypeName)
                {
                    fields[field.Name] = MigrateValue(nested, definitions);
                    continue;
                }
            }

            fields[field.Name] = CreateDefaultTypedValue(
                field.TypeName, definitions, new HashSet<string>(StringComparer.Ordinal));
        }

        return new StructValue(oldValue.TypeName, fields);
    }

    private static StructValue RenameType(StructValue value, string oldName, string newName)
    {
        var fields = value.Fields.ToDictionary(
            field => field.Key,
            field => field.Value switch
            {
                StructValue nested => RenameType(nested, oldName, newName),
                ArrayValue array => RenameArrayTypes(array, oldName, newName),
                _ => field.Value
            },
            StringComparer.Ordinal);
        return new StructValue(value.TypeName == oldName ? newName : value.TypeName, fields);
    }

    private static void FindMemberPaths(
        string typeName,
        object? currentValue,
        string prefix,
        string memberName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions,
        ICollection<string> exactPaths,
        ICollection<string> matchingFieldNames)
    {
        if (IsArrayTypeName(typeName) && currentValue is ArrayValue array)
        {
            for (var index = 0; index < array.Items.Count; index++)
            {
                FindMemberPaths(array.ElementTypeName, array.Items[index], $"{prefix}[{index}]", memberName,
                    definitions, exactPaths, matchingFieldNames);
            }

            return;
        }

        if (!definitions.TryGetValue(typeName, out var definition) || currentValue is not StructValue structure)
        {
            return;
        }

        foreach (var field in definition.Fields)
        {
            var fieldPath = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
            if (string.Equals(fieldPath, memberName, StringComparison.Ordinal))
            {
                exactPaths.Add(fieldPath);
            }
            else if (!memberName.Contains('.') && !memberName.Contains('[') &&
                     string.Equals(field.Name, memberName, StringComparison.Ordinal))
            {
                matchingFieldNames.Add(fieldPath);
            }

            if (structure.Fields.TryGetValue(field.Name, out var fieldValue))
            {
                FindMemberPaths(field.TypeName, fieldValue, fieldPath, memberName,
                    definitions, exactPaths, matchingFieldNames);
            }
        }
    }

    private static object UpdateTypedMember(
        string typeName,
        object? currentValue,
        string memberPath,
        object? replacementValue,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (memberPath.Length == 0)
        {
            var serialized = ToSerializableTypedValue(typeName, replacementValue, definitions);
            return ParseTypedValue(typeName, JsonSerializer.SerializeToElement(serialized), definitions);
        }

        if (IsArrayTypeName(typeName) && currentValue is ArrayValue array)
        {
            var closingBracket = memberPath[0] == '[' ? memberPath.IndexOf(']') : -1;
            if (closingBracket <= 1 ||
                !int.TryParse(memberPath.AsSpan(1, closingBracket - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                index < 0 || index >= array.Items.Count)
            {
                throw new InvalidDataException($"Member path '{memberPath}' contains an invalid array index.");
            }

            var remainingPath = memberPath[(closingBracket + 1)..];
            if (remainingPath.Length > 0 && remainingPath[0] == '.')
            {
                remainingPath = remainingPath[1..];
            }

            var items = array.Items.ToArray();
            items[index] = UpdateTypedMember(array.ElementTypeName, items[index], remainingPath, replacementValue, definitions);
            return new ArrayValue(array.ElementTypeName, items);
        }

        if (!definitions.TryGetValue(typeName, out var definition) || currentValue is not StructValue structure)
        {
            throw new InvalidDataException($"Member path '{memberPath}' does not resolve through struct type '{typeName}'.");
        }

        if (structure.Fields.TryGetValue(memberPath, out var existingField))
        {
            var fields = structure.Fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
            var fieldDefinition = definition.Fields.First(field => field.Name == memberPath);
            fields[memberPath] = UpdateTypedMember(fieldDefinition.TypeName, existingField, string.Empty, replacementValue, definitions);
            return new StructValue(typeName, fields);
        }

        for (var separatorIndex = memberPath.LastIndexOf('.'); separatorIndex > 0;
             separatorIndex = memberPath.LastIndexOf('.', separatorIndex - 1))
        {
            var fieldName = memberPath[..separatorIndex];
            var fieldDefinition = definition.Fields.FirstOrDefault(field => field.Name == fieldName);
            if (fieldDefinition is null || !structure.Fields.TryGetValue(fieldName, out var fieldValue))
            {
                continue;
            }

            var fields = structure.Fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
            fields[fieldName] = UpdateTypedMember(
                fieldDefinition.TypeName, fieldValue, memberPath[(separatorIndex + 1)..], replacementValue, definitions);
            return new StructValue(typeName, fields);
        }

        throw new InvalidDataException($"Struct type '{typeName}' does not contain member path '{memberPath}'.");
    }

    private static object ToSerializableTypedValue(
        string typeName,
        object? value,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (IsArrayTypeName(typeName))
        {
            if (value is not ArrayValue array || array.ElementTypeName != typeName[..^2])
            {
                throw new InvalidDataException($"Value is invalid for array type '{typeName}'.");
            }

            return array.Items.Select(item => ToSerializableTypedValue(array.ElementTypeName, item, definitions)).ToArray();
        }

        return typeName switch
        {
            "Integer" when value is int integer => integer,
            "Double" when value is double number && double.IsFinite(number) => number,
            "Boolean" when value is bool boolean => boolean,
            _ when definitions.ContainsKey(typeName) && value is StructValue structure && structure.TypeName == typeName =>
                ToSerializableFields(structure, definitions),
            _ => throw new InvalidDataException($"Value is invalid for type '{typeName}'.")
        };
    }

    private static ArrayValue MigrateArray(
        ArrayValue value,
        string typeName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        var elementTypeName = typeName[..^2];
        return new ArrayValue(elementTypeName, value.Items.Select(item => MigrateTypedValue(item, elementTypeName, definitions)).ToArray());
    }

    private static object MigrateTypedValue(
        object? value,
        string typeName,
        IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        if (typeName is "Integer" or "Double" or "Boolean")
        {
            return value is not null && MatchesPrimitiveType(typeName, value)
                ? value
                : CreateDefaultTypedValue(typeName, definitions, new HashSet<string>(StringComparer.Ordinal));
        }

        if (IsArrayTypeName(typeName) && value is ArrayValue array && array.ElementTypeName == typeName[..^2])
        {
            return MigrateArray(array, typeName, definitions);
        }

        if (value is StructValue structure && structure.TypeName == typeName)
        {
            return MigrateValue(structure, definitions);
        }

        return CreateDefaultTypedValue(typeName, definitions, new HashSet<string>(StringComparer.Ordinal));
    }

    private static ArrayValue RenameArrayTypes(ArrayValue array, string oldName, string newName)
    {
        var elementTypeName = RewriteTypeName(array.ElementTypeName, oldName, newName);
        return new ArrayValue(elementTypeName, array.Items.Select(item => item switch
        {
            StructValue nested => RenameType(nested, oldName, newName),
            ArrayValue nestedArray => RenameArrayTypes(nestedArray, oldName, newName),
            _ => item
        }).ToArray());
    }

    private static StructFieldDefinition RewriteReferencedType(
        StructFieldDefinition field,
        string oldName,
        string newName) => field with
        {
            TypeName = RewriteTypeName(field.TypeName, oldName, newName)
        };

    private static string RewriteTypeName(string typeName, string oldName, string newName)
    {
        var baseTypeName = GetBaseTypeName(typeName);
        return string.Equals(baseTypeName, oldName, StringComparison.Ordinal)
            ? newName + typeName[baseTypeName.Length..]
            : typeName;
    }

    private static string GetBaseTypeName(string typeName)
    {
        while (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            typeName = typeName[..^2];
        }

        return typeName;
    }

    private static bool IsArrayTypeName(string typeName) => typeName.EndsWith("[]", StringComparison.Ordinal);

    private static bool IsValidTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName) || typeName != typeName.Trim())
        {
            return false;
        }

        var baseTypeName = GetBaseTypeName(typeName);
        return !string.IsNullOrWhiteSpace(baseTypeName) &&
            !baseTypeName.Contains('[') && !baseTypeName.Contains(']');
    }

    private static void ValidateTypeName(string typeName, IReadOnlyDictionary<string, StructTypeDefinition> definitions)
    {
        var baseTypeName = GetBaseTypeName(typeName);
        if (!IsValidTypeName(typeName) ||
            !PrimitiveTypes.Contains(baseTypeName) && !definitions.ContainsKey(baseTypeName))
        {
            throw new InvalidDataException($"Type '{typeName}' is not defined.");
        }
    }

    private static bool MatchesPrimitiveType(string typeName, object value) => typeName switch
    {
        "Integer" => value is int,
        "Double" => value is double,
        "Boolean" => value is bool,
        _ => false
    };

    private static bool ContainsType(StructValue value, string typeName)
    {
        if (string.Equals(value.TypeName, typeName, StringComparison.Ordinal))
        {
            return true;
        }

        return value.Fields.Values.Any(fieldValue => ContainsType(fieldValue, typeName));
    }

    private static bool ContainsType(object? value, string typeName) => value switch
    {
        StructValue structure => ContainsType(structure, typeName),
        ArrayValue array => string.Equals(GetBaseTypeName(array.ElementTypeName), typeName, StringComparison.Ordinal) ||
            array.Items.Any(item => ContainsType(item, typeName)),
        _ => false
    };
}
