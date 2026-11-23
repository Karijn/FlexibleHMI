namespace Calculator.UI;

public partial class CalculatorViewModel
{
    /// <summary>Stores the persisted settings for a single calculator expression.</summary>
    /// <param name="OutputPropertyName">Name of the destination property.</param>
    /// <param name="Expression">Expression text to evaluate.</param>
    /// <param name="OutputType">Destination type name supported by the calculator.</param>
    /// <param name="Enabled">Whether this calculation is enabled.</param>
    public sealed record CalculatorSettings(
        string OutputPropertyName,
        string Expression,
        string OutputType,
        bool Enabled);
}
