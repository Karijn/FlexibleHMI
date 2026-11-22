namespace Calculator.UI;

internal readonly record struct ExpressionValue
{
    private ExpressionValue(double number)
    {
        Number = number;
        Boolean = false;
        IsNumber = true;
    }

    private ExpressionValue(bool boolean)
    {
        Number = 0d;
        Boolean = boolean;
        IsNumber = false;
    }

    public double Number { get; }
    public bool Boolean { get; }
    public bool IsNumber { get; }
    public bool IsBoolean => !IsNumber;

    public static ExpressionValue FromNumber(double number) => new(number);
    public static ExpressionValue FromBoolean(bool boolean) => new(boolean);

    public double AsNumber()
    {
        if (IsNumber)
        {
            return Number;
        }

        return Boolean ? 1d : 0d;
    }

    public bool AsBoolean()
    {
        if (IsBoolean)
        {
            return Boolean;
        }

        return !double.IsNaN(Number) && !double.IsInfinity(Number) && Number != 0d;
    }
}
