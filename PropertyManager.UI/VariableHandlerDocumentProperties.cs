using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Globalization;
using System.IO;
using System.Windows;

namespace PropertyManager.UI;

public sealed class VariableHandlerDocumentProperties : ITreeDocumentProperties
{
    private readonly IVariableHandler _variableHandler;

    public VariableHandlerDocumentProperties(IVariableHandler variableHandler)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _variableHandler.VariableChanged += OnVariableChanged;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<TreeDocumentProperty> GetProperties()
    {
        var properties = new List<TreeDocumentProperty>();
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

        var parsedProperties = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (property is null || string.IsNullOrWhiteSpace(property.Name))
            {
                throw new InvalidDataException("The document contains a missing or empty property name.");
            }

            if (!parsedProperties.TryAdd(property.Name, ParseValue(property)))
            {
                throw new InvalidDataException($"The document contains duplicate property '{property.Name}'.");
            }
        }

        foreach (var name in _variableHandler.GetVariableNames())
        {
            _variableHandler.Remove(name);
        }

        foreach (var (name, value) in parsedProperties)
        {
            _variableHandler.Set(name, value);
        }
    }

    private static object ParseValue(TreeDocumentProperty property) => property.Type switch
    {
        "Integer" when int.TryParse(property.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
        "Double" when double.TryParse(property.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) => value,
        "Boolean" when bool.TryParse(property.Value, out var value) => value,
        _ => throw new InvalidDataException($"Property '{property.Name}' has an invalid or unsupported value.")
    };

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
