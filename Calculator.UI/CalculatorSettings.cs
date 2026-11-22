namespace Calculator.UI;

public partial class CalculatorViewModel
{
    public sealed record CalculatorSettings(
        string OutputPropertyName,
        string Expression,
        string OutputType,
        bool Enabled);
}
