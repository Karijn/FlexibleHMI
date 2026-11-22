using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyManager.Interfaces;
using System.Globalization;

namespace Calculator.UI;

public partial class CalculatorCalculationViewModel : ObservableObject
{
    private readonly IVariableHandler _variableHandler;

    public CalculatorCalculationViewModel(IVariableHandler variableHandler, string outputPropertyName = "CalculatedProperty")
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        OutputPropertyName = outputPropertyName;
    }

    public IReadOnlyList<string> OutputTypes { get; } = ["Double", "Integer", "Boolean"];

    [ObservableProperty]
    private string _outputPropertyName = string.Empty;

    [ObservableProperty]
    private string _expression = string.Empty;

    [ObservableProperty]
    private string _outputType = "Double";

    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _result = "-";

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
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or OverflowException or KeyNotFoundException)
        {
            Result = "-";
            Status = exception.Message;
        }
    }

    public CalculatorCalculationViewModel Copy(IVariableHandler variableHandler) => new(variableHandler, OutputPropertyName)
    {
        Expression = Expression,
        OutputType = OutputType,
        Enabled = Enabled
    };
}
