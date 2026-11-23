using PropertyManager.Interfaces;
using System.Drawing;
using System.Globalization;
using System.Text.Json;

namespace Calculator.UI;

internal readonly record struct ExpressionValue
{
    private ExpressionValue(double number)
    {
        Number = number;
        Kind = ExpressionValueKind.Number;
    }

    private ExpressionValue(bool boolean)
    {
        Boolean = boolean;
        Kind = ExpressionValueKind.Boolean;
    }

    private ExpressionValue(string text)
    {
        Text = text;
        Kind = ExpressionValueKind.String;
    }

    private ExpressionValue(DateTime dateTime)
    {
        DateTime = dateTime;
        Kind = ExpressionValueKind.DateTime;
    }

    private ExpressionValue(TimeSpan timeSpan)
    {
        TimeSpan = timeSpan;
        Kind = ExpressionValueKind.TimeSpan;
    }

    private ExpressionValue(Color color)
    {
        Color = color;
        Kind = ExpressionValueKind.Color;
    }

    private ExpressionValue(JsonElement json)
    {
        Json = json.Clone();
        Kind = ExpressionValueKind.Json;
    }

    public double Number { get; }
    public bool Boolean { get; }
    public string Text { get; } = string.Empty;
    public DateTime DateTime { get; }
    public TimeSpan TimeSpan { get; }
    public Color Color { get; }
    public JsonElement Json { get; }
    public ExpressionValueKind Kind { get; }

    public bool IsNumber => Kind == ExpressionValueKind.Number;
    public bool IsBoolean => Kind == ExpressionValueKind.Boolean;
    public bool IsString => Kind == ExpressionValueKind.String;
    public bool IsDateTime => Kind == ExpressionValueKind.DateTime;
    public bool IsTimeSpan => Kind == ExpressionValueKind.TimeSpan;
    public bool IsColor => Kind == ExpressionValueKind.Color;
    public bool IsJson => Kind == ExpressionValueKind.Json;

    public static ExpressionValue FromNumber(double number) => new(number);
    public static ExpressionValue FromBoolean(bool boolean) => new(boolean);
    public static ExpressionValue FromString(string text) => new(text);
    public static ExpressionValue FromDateTime(DateTime dateTime) => new(dateTime);
    public static ExpressionValue FromTimeSpan(TimeSpan timeSpan) => new(timeSpan);
    public static ExpressionValue FromColor(Color color) => new(color);
    public static ExpressionValue FromJson(JsonElement json) => new(json);

    public double AsNumber()
    {
        if (IsNumber) return Number;
        if (IsBoolean) return Boolean ? 1d : 0d;
        if (IsTimeSpan) return TimeSpan.TotalSeconds;
        if (IsJson && Json.ValueKind == JsonValueKind.Number && Json.TryGetDouble(out var jsonNumber)) return jsonNumber;
        throw new FormatException("The expression value must be numeric.");
    }

    public bool AsBoolean()
    {
        if (IsBoolean) return Boolean;
        if (IsNumber) return !double.IsNaN(Number) && !double.IsInfinity(Number) && Number != 0d;
        if (IsString) return !string.IsNullOrEmpty(Text);
        if (IsJson)
        {
            return Json.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False or JsonValueKind.Null => false,
                JsonValueKind.Number => Json.TryGetDouble(out var number) && number != 0d,
                JsonValueKind.String => Json.GetString()?.Length > 0,
                JsonValueKind.Array or JsonValueKind.Object => true,
                _ => false
            };
        }
        return true;
    }

    public string AsString() => Kind switch
    {
        ExpressionValueKind.String => Text,
        ExpressionValueKind.Number => Number.ToString("G15", CultureInfo.InvariantCulture),
        ExpressionValueKind.Boolean => Boolean.ToString(CultureInfo.InvariantCulture),
        ExpressionValueKind.DateTime => DateTime.ToString("O", CultureInfo.InvariantCulture),
        ExpressionValueKind.TimeSpan => TimeSpan.ToString("c", CultureInfo.InvariantCulture),
        ExpressionValueKind.Color => ColorValue.Format(Color),
        ExpressionValueKind.Json => Json.ValueKind switch
        {
            JsonValueKind.String => Json.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Number => Json.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => Json.GetRawText()
        },
        _ => string.Empty
    };
}

internal enum ExpressionValueKind
{
    Number,
    Boolean,
    String,
    DateTime,
    TimeSpan,
    Color,
    Json
}
