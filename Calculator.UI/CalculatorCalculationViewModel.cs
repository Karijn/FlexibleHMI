using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyManager.Interfaces;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Calculator.UI;

/// <summary>
/// Holds one expression, its destination property and type, and the latest evaluation result and status.
/// </summary>
public partial class CalculatorCalculationViewModel : ObservableObject
{
    private readonly IVariableHandler _variableHandler;
    private readonly IStructTypeRegistry _structTypeRegistry;

    /// <summary>Creates a calculation bound to the specified property services.</summary>
    /// <param name="variableHandler">Provides source and destination properties.</param>
    /// <param name="structTypeRegistry">Handles typed structure and array values.</param>
    /// <param name="outputPropertyName">Initial destination property name; defaults to <c>CalculatedProperty</c>.</param>
    /// <exception cref="ArgumentNullException">A required service is <see langword="null"/>.</exception>
    public CalculatorCalculationViewModel(IVariableHandler variableHandler, IStructTypeRegistry structTypeRegistry, string outputPropertyName = "CalculatedProperty")
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _structTypeRegistry = structTypeRegistry ?? throw new ArgumentNullException(nameof(structTypeRegistry));
        OutputPropertyName = outputPropertyName;
    }

    /// <summary>Gets the output type names supported by the calculator.</summary>
    public IReadOnlyList<string> OutputTypes { get; } = ["Double", "Integer", "Boolean", "String", "DateTime", "TimeSpan", "Color", "Struct", "Array"];

    /// <summary>Gets or sets the property receiving the calculated value.</summary>
    [ObservableProperty]
    private string _outputPropertyName = string.Empty;

    /// <summary>Gets or sets the expression to evaluate.</summary>
    [ObservableProperty]
    private string _expression = string.Empty;

    /// <summary>Gets or sets the destination type: a scalar type, <c>Struct</c>, or <c>Array</c>.</summary>
    [ObservableProperty]
    private string _outputType = "Double";

    /// <summary>Gets or sets whether evaluation is enabled, including live recalculation on property changes.</summary>
    [ObservableProperty]
    private bool _enabled = true;

    /// <summary>Gets the latest formatted result, or <c>-</c> when no result is available.</summary>
    [ObservableProperty]
    private string _result = "-";

    /// <summary>Gets the latest evaluation status or validation error message.</summary>
    [ObservableProperty]
    private string _status = "Enter an expression and set the output property.";

    partial void OnExpressionChanged(string value) => Recalculate();

    partial void OnOutputPropertyNameChanged(string value) => Recalculate();

    partial void OnOutputTypeChanged(string value) => Recalculate();

    partial void OnEnabledChanged(bool value)
    {
        if (value)
        {
            Recalculate();
        }
        else
        {
            Status = "Calculation disabled.";
        }
    }

    /// <summary>Evaluates the current expression and updates the destination property when the inputs are valid.</summary>
    /// <remarks>Evaluation errors are reported in <see cref="Status"/> and clear <see cref="Result"/> rather than being propagated.</remarks>
    [RelayCommand]
    public void Recalculate()
    {
        if (!Enabled)
        {
            return;
        }

        var outputName = OutputPropertyName.Trim();
        if (string.IsNullOrWhiteSpace(outputName))
        {
            Result = "-";
            Status = "Enter an output property name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Expression))
        {
            Result = "-";
            Status = "Enter an expression.";
            return;
        }

        try
        {
            var value = ExpressionParser.Parse(Expression, _variableHandler);

            if (OutputType == "Boolean")
            {
                var booleanValue = value.IsBoolean
                    ? value.Boolean
                    : !double.IsNaN(value.Number) && !double.IsInfinity(value.Number) && value.Number != 0d;
                _variableHandler.Set(outputName, booleanValue);
                Result = booleanValue.ToString(CultureInfo.InvariantCulture);
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (OutputType == "String")
            {
                var textValue = value.AsString();
                _variableHandler.Set(outputName, textValue);
                Result = textValue;
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (OutputType == "DateTime")
            {
                if (!value.IsDateTime)
                {
                    throw new InvalidOperationException("The expression result must be a DateTime for this output type.");
                }

                _variableHandler.Set(outputName, value.DateTime);
                Result = value.DateTime.ToString("O", CultureInfo.InvariantCulture);
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (OutputType == "TimeSpan")
            {
                if (!value.IsTimeSpan)
                {
                    throw new InvalidOperationException("The expression result must be a TimeSpan for this output type.");
                }

                _variableHandler.Set(outputName, value.TimeSpan);
                Result = value.TimeSpan.ToString("c", CultureInfo.InvariantCulture);
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (OutputType == "Color")
            {
                if (!value.IsColor)
                {
                    throw new InvalidOperationException("The expression result must be a Color for this output type.");
                }

                _variableHandler.Set(outputName, value.Color);
                Result = ColorValue.Format(value.Color);
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (OutputType is "Struct" or "Array")
            {
                if (!value.IsJson) throw new InvalidOperationException("The expression result must be a JSON object or array.");
                string typeName;
                object previous;
                if (OutputType == "Struct" && _variableHandler.TryGet<StructValue>(outputName, out var structure) && structure is not null)
                {
                    typeName = structure.TypeName;
                    previous = structure;
                }
                else if (OutputType == "Array" && _variableHandler.TryGet<ArrayValue>(outputName, out var array) && array is not null)
                {
                    typeName = $"{array.ElementTypeName}[]";
                    previous = array;
                }
                else
                {
                    throw new InvalidOperationException($"Create the destination {OutputType} property before calculating its value.");
                }

                var parsed = _structTypeRegistry.ParseTypedValue(typeName, value.Json.GetRawText());
                var result = _structTypeRegistry.SerializeTypedValue(typeName, parsed);
                if (result != _structTypeRegistry.SerializeTypedValue(typeName, previous)) _variableHandler.Set(outputName, parsed);
                Result = result;
                Status = $"Live result in '{outputName}'.";
                return;
            }

            if (!value.IsNumber)
            {
                throw new InvalidOperationException("The expression result must be numeric for this output type.");
            }

            var number = value.Number;
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw new InvalidOperationException("The result is not a finite number.");
            }

            if (OutputType == "Integer")
            {
                if (number is < int.MinValue or > int.MaxValue)
                {
                    throw new OverflowException("The result is outside the integer range.");
                }

                var integerValue = (int)Math.Round(number, MidpointRounding.ToEven);
                _variableHandler.Set(outputName, integerValue);
                Result = integerValue.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                _variableHandler.Set(outputName, number);
                Result = number.ToString("G15", CultureInfo.InvariantCulture);
            }

            Status = $"Live result in '{outputName}'.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or OverflowException or KeyNotFoundException or JsonException or InvalidDataException)
        {
            Result = "-";
            Status = exception.Message;
        }
    }

    /// <summary>Creates a calculation with the same expression settings and the supplied property services.</summary>
    /// <param name="variableHandler">Property handler for the copied calculation.</param>
    /// <param name="structTypeRegistry">Structure registry for the copied calculation.</param>
    /// <returns>A new calculation carrying this calculation's expression, output type, enabled state, and destination name.</returns>
    public CalculatorCalculationViewModel Copy(IVariableHandler variableHandler, IStructTypeRegistry structTypeRegistry) => new(variableHandler, structTypeRegistry, OutputPropertyName)
    {
        Expression = Expression,
        OutputType = OutputType,
        Enabled = Enabled
    };
}
