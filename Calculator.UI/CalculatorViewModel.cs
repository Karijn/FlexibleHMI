using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Calculator.UI;

/// <summary>
/// Represents a document item that evaluates expressions against live properties and writes each result to a destination property.
/// </summary>
public partial class CalculatorViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentItemDetached
{
    private readonly IVariableHandler _variableHandler;
    private readonly IStructTypeRegistry _structTypeRegistry;
    private bool _isDetached;

    /// <summary>
    /// Creates a calculator item connected to the specified property handler and structure registry.
    /// </summary>
    /// <param name="variableHandler">Provides the live properties read by expressions and written by calculations.</param>
    /// <param name="structTypeRegistry">Parses and serializes typed structure and array results.</param>
    /// <exception cref="ArgumentNullException">Either dependency is <see langword="null"/>.</exception>
    public CalculatorViewModel(IVariableHandler variableHandler, IStructTypeRegistry structTypeRegistry)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _structTypeRegistry = structTypeRegistry ?? throw new ArgumentNullException(nameof(structTypeRegistry));
        Name = "Calculator";
        Description = "Calculate properties from live property values";
        Image = CalculatorCreator.GetIcon(); // TreeItemOption.CreateDiamondImage(Colors.DarkOrange);
        _variableHandler.VariableChanged += OnVariableChanged;
        RefreshAvailableProperties();
        Calculations.Add(new CalculatorCalculationViewModel(_variableHandler, _structTypeRegistry));
    }

    /// <summary>Gets the editable calculations displayed by the calculator view.</summary>
    public ObservableCollection<CalculatorCalculationViewModel> Calculations { get; } = [];

    /// <inheritdoc/>
    public void OnDocumentItemDetached()
    {
        _isDetached = true;
        _variableHandler.VariableChanged -= OnVariableChanged;
    }

    /// <inheritdoc/>
    public void OnDocumentItemReattached()
    {
        if (_isDetached)
        {
            _isDetached = false;
            _variableHandler.VariableChanged += OnVariableChanged;
        }
    }

    /// <summary>Gets the current property names and scalar structure/array paths available to expressions.</summary>
    public ObservableCollection<string> AvailableProperties { get; } = [];

    /// <summary>Gets or sets whether the calculator section is expanded in the view.</summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <inheritdoc/>
    public override string Kind => CalculatorCreator.jsonkey;

    /// <inheritdoc/>
    public override int MaxChildren => 0;

    /// <inheritdoc/>
    public override string ChildRule => "No Children here";

    /// <inheritdoc/>
    public override bool CanAcceptChildren => false;

    [RelayCommand]
    private void AddCalculation()
    {
        var number = 1;
        string name;
        do
        {
            name = $"CalculatedProperty{number++}";
        }
        while (Calculations.Any(item => string.Equals(item.OutputPropertyName, name, StringComparison.Ordinal)));

        Calculations.Add(new CalculatorCalculationViewModel(_variableHandler, _structTypeRegistry, name));
    }

    [RelayCommand]
    private void RemoveCalculation(CalculatorCalculationViewModel? calculation)
    {
        if (calculation is not null)
        {
            Calculations.Remove(calculation);
        }
    }

    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        var copy = new CalculatorViewModel(_variableHandler, _structTypeRegistry)
        {
            Name = Name,
            Description = Description,
            IsExpanded = IsExpanded
        };
        copy.Calculations.Clear();
        foreach (var calculation in Calculations)
        {
            copy.Calculations.Add(calculation.Copy(_variableHandler, _structTypeRegistry));
        }

        return copy;
    }

    /// <inheritdoc/>
    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        new CalculatorDocumentSettings(
            Calculations.Select(calculation => new CalculatorSettings(
                calculation.OutputPropertyName,
                calculation.Expression,
                calculation.OutputType,
                calculation.Enabled)).ToList(),
            IsExpanded));

    /// <inheritdoc/>
    public void LoadDocumentData(JsonElement data)
    {
        List<CalculatorSettings> settings;
        if (data.ValueKind == JsonValueKind.Array)
        {
            settings = data.Deserialize<List<CalculatorSettings>>()
                ?? throw new InvalidDataException("Calculator data is invalid.");
        }
        else
        {
            var documentSettings = data.Deserialize<CalculatorDocumentSettings>()
                ?? throw new InvalidDataException("Calculator data is invalid.");
            settings = documentSettings.Calculations
                ?? throw new InvalidDataException("Calculator data is missing calculations.");
            IsExpanded = documentSettings.IsExpanded;
        }

        Calculations.Clear();
        foreach (var setting in settings)
        {
            if (setting.OutputType is not ("Integer" or "Double" or "Boolean" or "String" or "DateTime" or "TimeSpan" or "Color" or "Struct" or "Array") ||
                !double.TryParse("0", NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                throw new InvalidDataException("The document contains invalid calculator settings.");
            }

            Calculations.Add(new CalculatorCalculationViewModel(_variableHandler, _structTypeRegistry, setting.OutputPropertyName)
            {
                Expression = setting.Expression,
                OutputType = setting.OutputType,
                Enabled = setting.Enabled
            });
        }

        if (Calculations.Count == 0)
        {
            Calculations.Add(new CalculatorCalculationViewModel(_variableHandler, _structTypeRegistry));
        }
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyVariableChange();
            return;
        }

        dispatcher.BeginInvoke(new Action(ApplyVariableChange));
    }

    private void ApplyVariableChange()
    {
        if (_isDetached)
        {
            return;
        }

        RefreshAvailableProperties();
        foreach (var calculation in Calculations)
        {
            if (calculation.Enabled)
            {
                calculation.Recalculate();
            }
        }
    }

    private void RefreshAvailableProperties()
    {
        var names = new List<string>();
        foreach (var name in _variableHandler.GetVariableNames())
        {
            if (_variableHandler.TryGet<StructValue>(name, out var structure) && structure is not null)
            {
                names.Add(FormatExpressionReference(name));
                names.AddRange(GetScalarFieldReferences(structure, name));
            }
            else if (_variableHandler.TryGet<ArrayValue>(name, out var array) && array is not null)
            {
                names.Add(FormatExpressionReference(name));
                names.AddRange(GetScalarArrayReferences(array, name));
            }
            else
            {
                names.Add(FormatExpressionReference(name));
            }
        }

        names = names.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList();
        foreach (var name in names.Where(name => !AvailableProperties.Contains(name, StringComparer.Ordinal)))
        {
            AvailableProperties.Add(name);
        }

        foreach (var name in AvailableProperties.Where(name => !names.Contains(name, StringComparer.Ordinal)).ToList())
        {
            AvailableProperties.Remove(name);
        }
    }

    private static IEnumerable<string> GetScalarFieldReferences(StructValue structure, string path)
    {
        foreach (var (fieldName, value) in structure.Fields)
        {
            var fieldPath = $"{path}.{fieldName}";
            if (value is StructValue nested)
            {
                foreach (var reference in GetScalarFieldReferences(nested, fieldPath))
                {
                    yield return reference;
                }
            }
            else if (value is ArrayValue array)
            {
                foreach (var reference in GetScalarArrayReferences(array, fieldPath))
                {
                    yield return reference;
                }
            }
            else if (value is int or double or bool or string or DateTime or TimeSpan or System.Drawing.Color)
            {
                yield return FormatExpressionReference(fieldPath);
            }
        }
    }

    private static IEnumerable<string> GetScalarArrayReferences(ArrayValue array, string path)
    {
        for (var index = 0; index < array.Items.Count; index++)
        {
            var itemPath = $"{path}[{index}]";
            var item = array.Items[index];
            if (item is StructValue structure)
            {
                foreach (var reference in GetScalarFieldReferences(structure, itemPath))
                {
                    yield return reference;
                }
            }
            else if (item is ArrayValue nestedArray)
            {
                foreach (var reference in GetScalarArrayReferences(nestedArray, itemPath))
                {
                    yield return reference;
                }
            }
            else if (item is int or double or bool or string or DateTime or TimeSpan or System.Drawing.Color)
            {
                yield return FormatExpressionReference(itemPath);
            }
        }
    }

    private sealed record CalculatorDocumentSettings(List<CalculatorSettings>? Calculations, bool IsExpanded = true);

    private static string FormatExpressionReference(string name)
    {
        var validUnbracedName = name.Length > 0 &&
            (char.IsLetter(name[0]) || name[0] == '_') &&
            name.All(character => char.IsLetterOrDigit(character) || character is '_' or '.' or '[' or ']');
        var reservedLiteral = BooleanValueParser.TryParse(name, out _) ||
            name.Equals("pi", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("e", StringComparison.OrdinalIgnoreCase);
        return validUnbracedName && !reservedLiteral ? name : $"{{{name}}}";
    }
}
