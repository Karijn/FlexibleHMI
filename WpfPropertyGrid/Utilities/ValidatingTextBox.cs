namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides text-box input validation and formatted text handling.
/// </summary>
public class ValidatingTextBox : TextBox
{
    private bool _inEvents;
    private string? _textBefore;
    private string? _initalText;
    private int _selectionStart;
    private int _selectionLength;

    // validates entered text at char level, but globally
    /// <summary>
    /// Occurs when pre validate text.
    /// </summary>
    public event EventHandler<ValidateTextEventArgs>? PreValidateText;

    // validates before losing focus
    /// <summary>
    /// Occurs when validate text.
    /// </summary>
    public event EventHandler<ValidateTextEventArgs>? ValidateText;
    /// <summary>
    /// Gets or sets the validate.
    /// </summary>
    public virtual bool Validate { get; set; } = true;

    /// <summary>
    /// Raises the pre-validation event before the text is validated.
    /// </summary>
    protected virtual void OnPreValidateText(object sender, ValidateTextEventArgs e) => PreValidateText?.Invoke(this, e);
    /// <summary>
    /// Validates the current text and reports any validation error.
    /// </summary>
    protected virtual void OnValidateText(object sender, ValidateTextEventArgs e) => ValidateText?.Invoke(this, e);

    /// <inheritdoc/>
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _initalText = Text;
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (!Validate)
            return;

        var ev = new ValidateTextEventArgs(Text);
        OnValidateText(this, ev);
        if (ev.Cancel && Text != _initalText)
        {
            Text = _initalText;
        }
        else if (ev.ReplacementText != null && Text != ev.ReplacementText)
        {
            Text = ev.ReplacementText;
        }
    }

    /// <inheritdoc/>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!Validate)
            return;

        if (_inEvents)
            return;

        _selectionStart = SelectionStart;
        _selectionLength = SelectionLength;
        _textBefore = Text;
    }

    /// <inheritdoc/>
    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        if (!Validate)
            return;

        if (_inEvents)
            return;

        _inEvents = true;
        try
        {
            var ev = new ValidateTextEventArgs(Text);
            OnPreValidateText(this, ev);
            if (ev.Cancel)
            {
                Text = _textBefore;
                SelectionStart = _selectionStart;
                SelectionLength = _selectionLength;
            }
            else if (ev.ReplacementText != null && ev.ReplacementText != Text)
            {
                Text = ev.ReplacementText;
            }
            base.OnTextChanged(e);
        }
        finally
        {
            _inEvents = false;
        }
    }
}
/// <summary>
/// Provides numeric text input with parsing and range validation.
/// </summary>
public abstract class NumberTextBox<T> : ValidatingTextBox where T : struct, INumberBase<T>, IMinMaxValue<T>, IComparisonOperators<T, T, bool>
{
    /// <summary>
    /// Gets or sets is floating point for derived controls.
    /// </summary>
    protected virtual bool IsFloatingPoint => false;
    /// <summary>
    /// Gets or sets is empty valid for derived controls.
    /// </summary>
    protected virtual bool IsEmptyValid => true;
    /// <summary>
    /// Gets or sets is signed for derived controls.
    /// </summary>
    protected virtual bool IsSigned
    {
        get
        {
            if (MinValue.HasValue && MinValue.Value >= T.Zero)
                return false;

            return true;
        }
    }
    /// <summary>
    /// Gets or sets the min value.
    /// </summary>
    public T? MinValue { get; set; }
    /// <summary>
    /// Gets or sets the max value.
    /// </summary>
    public T? MaxValue { get; set; }
    /// <summary>
    /// Gets the value.
    /// </summary>
    public T Value
    {
        get
        {
            var norm = Text.Replace(',', '.');
            T.TryParse(norm, CultureInfo.InvariantCulture, out var value);
            return value;
        }
    }

    /// <summary>
    /// Provides the is valid digit extension point for derived controls.
    /// </summary>
    protected virtual bool IsValidDigit(char c) => char.IsAsciiDigit(c);
    /// <inheritdoc/>
    protected override void OnValidateText(object sender, ValidateTextEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Text))
        {
            var norm = e.Text.Replace(',', '.');
            if (T.TryParse(norm, CultureInfo.InvariantCulture, out var value))
            {
                if (MinValue.HasValue && value < MinValue.Value)
                {
                    value = MinValue.Value;
                }

                if (MaxValue.HasValue && value >= MaxValue.Value)
                {
                    value = MaxValue.Value;
                }

                e.ReplacementText = value.ToString();
                return;
            }

            if (IsFloatingPoint)
            {
                if (typeof(T) != typeof(double) && double.TryParse(norm, CultureInfo.InvariantCulture, out var dbl))
                {
                    e.ReplacementText = dbl > 0 ? (MaxValue ?? T.MaxValue).ToString() : (MinValue ?? T.MinValue).ToString();
                    return;
                }
            }
            else
            {
                if (IsSigned)
                {
                    if (typeof(T) != typeof(long) && long.TryParse(norm, CultureInfo.InvariantCulture, out var lg))
                    {
                        e.ReplacementText = lg > 0 ? (MaxValue ?? T.MaxValue).ToString() : (MinValue ?? T.MinValue).ToString();
                        return;
                    }
                }
                else
                {
                    if (typeof(T) != typeof(ulong) && ulong.TryParse(norm, CultureInfo.InvariantCulture, out var ulg))
                    {
                        e.ReplacementText = ulg > 0 ? (MaxValue ?? T.MaxValue).ToString() : (MinValue ?? T.MinValue).ToString();
                        return;
                    }
                }
            }
        }
        e.Cancel = true;
    }

    /// <inheritdoc/>
    protected override void OnPreValidateText(object sender, ValidateTextEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text))
        {
            e.Cancel = !IsEmptyValid;
            return;
        }

        var dashes = 0;
        var floatingPoints = 0;
        for (var i = 0; i < e.Text.Length; i++)
        {
            var c = e.Text[i];
            if (c == '-' && IsSigned)
            {
                dashes++;
                if (dashes > 1)
                {
                    e.Cancel = true;
                    return;
                }
                continue;
            }

            // note: we don't support scientific notation
            if ((c == '.' || c == ',') && IsFloatingPoint)
            {
                floatingPoints++;
                if (floatingPoints > 1)
                {
                    e.Cancel = true;
                    return;
                }
                continue;
            }

            if (!IsValidDigit(c))
            {
                e.Cancel = true;
                return;
            }
        }
    }
}
/// <summary>
/// Provides signed-integer text input with range validation.
/// </summary>
public abstract class SignedIntegerTextBox<T> : NumberTextBox<T> where T : struct, ISignedNumber<T>, IMinMaxValue<T>, IComparisonOperators<T, T, bool>
{
}
/// <summary>
/// Provides unsigned-integer text input with range validation.
/// </summary>
public abstract class UnsignedIntegerTextBox<T> : NumberTextBox<T> where T : struct, IUnsignedNumber<T>, IMinMaxValue<T>, IComparisonOperators<T, T, bool>
{
    /// <inheritdoc/>
    protected override bool IsSigned => false;
}
/// <summary>
/// Provides floating-point text input with range validation.
/// </summary>
public abstract class FloatingPointNumberTextBox<T> : NumberTextBox<T> where T : struct, IFloatingPoint<T>, IMinMaxValue<T>, IComparisonOperators<T, T, bool>
{
    /// <inheritdoc/>
    protected override bool IsFloatingPoint => true;
}
/// <summary>
/// Provides the sbyte text box functionality.
/// </summary>
public class SByteTextBox : SignedIntegerTextBox<sbyte>
{
}
/// <summary>
/// Provides the byte text box functionality.
/// </summary>
public class ByteTextBox : UnsignedIntegerTextBox<byte>
{
}
/// <summary>
/// Provides the int16 text box functionality.
/// </summary>
public class Int16TextBox : SignedIntegerTextBox<short>
{
}
/// <summary>
/// Provides the int32 text box functionality.
/// </summary>
public class Int32TextBox : SignedIntegerTextBox<int>
{
}
/// <summary>
/// Provides the int64 text box functionality.
/// </summary>
public class Int64TextBox : SignedIntegerTextBox<long>
{
}
/// <summary>
/// Provides the uint16 text box functionality.
/// </summary>
public class UInt16TextBox : UnsignedIntegerTextBox<ushort>
{
}
/// <summary>
/// Provides the uint32 text box functionality.
/// </summary>
public class UInt32TextBox : UnsignedIntegerTextBox<uint>
{
}
/// <summary>
/// Provides the uint64 text box functionality.
/// </summary>
public class UInt64TextBox : UnsignedIntegerTextBox<ulong>
{
}
/// <summary>
/// Provides the double text box functionality.
/// </summary>
public class DoubleTextBox : FloatingPointNumberTextBox<double>
{
}
/// <summary>
/// Provides the single text box functionality.
/// </summary>
public class SingleTextBox : FloatingPointNumberTextBox<float>
{
}
/// <summary>
/// Provides the decimal text box functionality.
/// </summary>
public class DecimalTextBox : FloatingPointNumberTextBox<decimal>
{
}
/// <summary>
/// Provides text input for a color value.
/// </summary>
public class ColorTextBox : ValidatingTextBox
{
    /// <summary>
    /// Formats the current value according to the supplied format and culture.
    /// </summary>
    public static string Format(Color color) => $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    /// <summary>
    /// Gets the value.
    /// </summary>
    public Color Value
    {
        get
        {
            ConversionService.TryConvertType<Color>(Text, out var value);
            return value;
        }
    }

    /// <inheritdoc/>
    protected override void OnValidateText(object sender, ValidateTextEventArgs e)
    {
        base.OnValidateText(sender, e);
        e.Cancel = !ConversionService.TryConvertType<Color>(Text, out var value);
        e.ReplacementText = Format(value);
    }

    /// <inheritdoc/>
    protected override void OnPreValidateText(object sender, ValidateTextEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text))
            return;

        for (var i = 0; i < e.Text.Length; i++)
        {
            var c = e.Text[i];

            if (!char.IsAsciiHexDigit(c))
            {
                e.Cancel = true;
                return;
            }
        }
    }
}