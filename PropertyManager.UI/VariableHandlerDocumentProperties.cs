using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace PropertyManager.UI;

public sealed class VariableHandlerDocumentProperties : ITreeDocumentProperties
{
    private readonly IVariableHandler _variableHandler;
    private readonly IStructTypeRegistry _structTypeRegistry;

    public VariableHandlerDocumentProperties(IVariableHandler variableHandler, IStructTypeRegistry structTypeRegistry)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _structTypeRegistry = structTypeRegistry ?? throw new ArgumentNullException(nameof(structTypeRegistry));
        _variableHandler.VariableChanged += OnVariableChanged;
        _structTypeRegistry.Changed += OnStructTypesChanged;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<TreeDocumentProperty> GetProperties()
    {
        var properties = _structTypeRegistry.GetDefinitions()
            .Select(definition => new TreeDocumentProperty(
                definition.Name,
                "StructDefinition",
                JsonSerializer.Serialize(definition)))
            .ToList();
        foreach (var name in _variableHandler.GetVariableNames())
        {
            if (_variableHandler.TryGet<int>(name, out var integer))
            {
                properties.Add(new TreeDocumentProperty(name, "Integer", integer.ToString(CultureInfo.InvariantCulture)));
            }
            else if (_variableHandler.TryGet<double>(name, out var number) && double.IsFinite(number))
            {
                properties.Add(new TreeDocumentProperty(name, "Double", number.ToString("R", CultureInfo.InvariantCulture)));
            }
            else if (_variableHandler.TryGet<bool>(name, out var boolean))
            {
                properties.Add(new TreeDocumentProperty(name, "Boolean", boolean.ToString()));
            }
            else if (_variableHandler.TryGet<StructValue>(name, out var structure) && structure is not null)
            {
                properties.Add(new TreeDocumentProperty(
                    name,
                    $"Struct:{structure.TypeName}",
                    _structTypeRegistry.SerializeValue(structure)));
            }
            else if (_variableHandler.TryGet<ArrayValue>(name, out var array) && array is not null)
            {
                var arrayType = $"{array.ElementTypeName}[]";
                properties.Add(new TreeDocumentProperty(
                    name,
                    $"Array:{array.ElementTypeName}",
                    _structTypeRegistry.SerializeTypedValue(arrayType, array)));
            }
            else
            {
                throw new InvalidDataException($"Property '{name}' has an unsupported value type or invalid value.");
            }
        }

        return properties;
    }

    public void LoadProperties(IReadOnlyList<TreeDocumentProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var structDefinitions = new List<StructTypeDefinition>();
        foreach (var property in properties.Where(property => property?.Type == "StructDefinition"))
        {
            var definition = JsonSerializer.Deserialize<StructTypeDefinition>(property.Value)
                ?? throw new InvalidDataException($"Struct definition '{property.Name}' is invalid.");
            if (!string.Equals(definition.Name, property.Name, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Struct definition '{property.Name}' has a mismatched name.");
            }

            structDefinitions.Add(definition);
        }

        _structTypeRegistry.ValidateDefinitions(structDefinitions);
        var parsedProperties = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in properties.Where(property => property?.Type != "StructDefinition"))
        {
            if (property is null || string.IsNullOrWhiteSpace(property.Name))
            {
                throw new InvalidDataException("The document contains a missing or empty property name.");
            }

            var value = ParseValue(property, structDefinitions);
            if (!parsedProperties.TryAdd(property.Name, value))
            {
                throw new InvalidDataException($"The document contains duplicate property '{property.Name}'.");
            }
        }

        foreach (var name in _variableHandler.GetVariableNames().ToArray())
        {
            _variableHandler.Remove(name);
        }

        _structTypeRegistry.ReplaceDefinitions(structDefinitions);
        foreach (var (name, value) in parsedProperties)
        {
            _variableHandler.Set(name, value);
        }
    }

    private object ParseValue(TreeDocumentProperty property, IReadOnlyList<StructTypeDefinition> definitions)
    {
        if (property.Type.StartsWith("Struct:", StringComparison.Ordinal))
        {
            var typeName = property.Type["Struct:".Length..];
            return _structTypeRegistry.ParseValue(typeName, property.Value, definitions);
        }

        if (property.Type.StartsWith("Array:", StringComparison.Ordinal))
        {
            var elementTypeName = property.Type["Array:".Length..];
            return _structTypeRegistry.ParseTypedValue($"{elementTypeName}[]", property.Value, definitions);
        }

        return property.Type switch
        {
            "Integer" when int.TryParse(property.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            "Double" when double.TryParse(property.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) => value,
            "Boolean" when BooleanValueParser.TryParse(property.Value, out var value) => value,
            _ => throw new InvalidDataException($"Property '{property.Name}' has an invalid or unsupported value.")
        };
    }

    private void OnStructTypesChanged(object? sender, EventArgs eventArgs) => OnVariableChanged(sender, new VariableChanged(string.Empty, null, null));

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => Changed?.Invoke(this, EventArgs.Empty)));
    }
}
