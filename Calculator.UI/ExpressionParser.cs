using PropertyManager.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace Calculator.UI;

internal sealed class ExpressionParser
{
    private readonly string _text;
    private readonly IVariableHandler _variables;
    private int _position;

    private ExpressionParser(string text, IVariableHandler variables)
    {
        _text = text;
        _variables = variables;
    }

    public static ExpressionValue Parse(string text, IVariableHandler variables)
    {
        var parser = new ExpressionParser(text, variables);
        var value = parser.ParseExpression();
        parser.SkipWhitespace();
        if (!parser.AtEnd)
        {
            throw new FormatException($"Unexpected character '{parser.Current}' at position {parser._position + 1}.");
        }

        return value;
    }

    private char Current => _position < _text.Length ? _text[_position] : '\0';

    private bool AtEnd => _position >= _text.Length;

    private ExpressionValue ParseExpression() => ParseOr();

    private ExpressionValue ParseOr()
    {
        var value = ParseAnd();
        while (MatchOperator("||"))
        {
            value = ExpressionValue.FromBoolean(value.AsBoolean() || ParseAnd().AsBoolean());
        }

        return value;
    }

    private ExpressionValue ParseAnd()
    {
        var value = ParseComparison();
        while (MatchOperator("&&"))
        {
            value = ExpressionValue.FromBoolean(value.AsBoolean() && ParseComparison().AsBoolean());
        }

        return value;
    }

    private ExpressionValue ParseComparison()
    {
        var left = ParseAdditive();
        string? operatorText = null;
        SkipWhitespace();

        foreach (var candidate in new[] { "<=", ">=", "==", "!=", "<", ">" })
        {
            if (MatchOperator(candidate))
            {
                operatorText = candidate;
                break;
            }
        }

        if (operatorText is null)
        {
            return left;
        }

        var right = ParseAdditive();
        if (left.Kind == right.Kind)
        {
            var equal = left.Kind switch
            {
                ExpressionValueKind.String => string.Equals(left.Text, right.Text, StringComparison.Ordinal),
                ExpressionValueKind.DateTime => left.DateTime == right.DateTime,
                ExpressionValueKind.TimeSpan => left.TimeSpan == right.TimeSpan,
                ExpressionValueKind.Color => left.Color.ToArgb() == right.Color.ToArgb(),
                ExpressionValueKind.Json => left.Json.GetRawText() == right.Json.GetRawText(),
                ExpressionValueKind.Boolean => left.Boolean == right.Boolean,
                ExpressionValueKind.Number => left.Number == right.Number,
                _ => false
            };

            if (operatorText is "==" or "!=")
            {
                return ExpressionValue.FromBoolean(operatorText == "==" ? equal : !equal);
            }

            return left.Kind switch
            {
                ExpressionValueKind.String => ExpressionValue.FromBoolean(Compare(left.Text, right.Text, operatorText)),
                ExpressionValueKind.DateTime => ExpressionValue.FromBoolean(Compare(left.DateTime, right.DateTime, operatorText)),
                ExpressionValueKind.TimeSpan => ExpressionValue.FromBoolean(Compare(left.TimeSpan, right.TimeSpan, operatorText)),
                ExpressionValueKind.Number => ExpressionValue.FromBoolean(Compare(left.Number, right.Number, operatorText)),
                _ => throw new FormatException($"Operator '{operatorText}' requires numeric, string, date/time, or time span values.")
            };
        }

        if (left.IsBoolean && right.IsBoolean)
        {
            return operatorText switch
            {
                "==" => ExpressionValue.FromBoolean(left.Boolean == right.Boolean),
                "!=" => ExpressionValue.FromBoolean(left.Boolean != right.Boolean),
                _ => throw new FormatException($"Operator '{operatorText}' requires numeric values.")
            };
        }

        var leftNumber = left.AsNumber();
        var rightNumber = right.AsNumber();
        return operatorText switch
        {
            "==" => ExpressionValue.FromBoolean(leftNumber == rightNumber),
            "!=" => ExpressionValue.FromBoolean(leftNumber != rightNumber),
            "<" => ExpressionValue.FromBoolean(leftNumber < rightNumber),
            "<=" => ExpressionValue.FromBoolean(leftNumber <= rightNumber),
            ">" => ExpressionValue.FromBoolean(leftNumber > rightNumber),
            ">=" => ExpressionValue.FromBoolean(leftNumber >= rightNumber),
            _ => throw new InvalidOperationException("Unsupported comparison operator.")
        };
    }

    private ExpressionValue ParseAdditive()
    {
        var value = ParseTerm();
        while (true)
        {
            SkipWhitespace();
            if (Match('+'))
            {
                var right = ParseTerm();
                if (value.IsString)
                {
                    value = ExpressionValue.FromString(value.Text + right.AsString());
                }
                else if (value.IsDateTime && right.IsTimeSpan)
                {
                    value = ExpressionValue.FromDateTime(value.DateTime + right.TimeSpan);
                }
                else if (value.IsTimeSpan && right.IsTimeSpan)
                {
                    value = ExpressionValue.FromTimeSpan(value.TimeSpan + right.TimeSpan);
                }
                else
                {
                    value = ExpressionValue.FromNumber(value.AsNumber() + right.AsNumber());
                }
            }
            else if (Match('-'))
            {
                var right = ParseTerm();
                if (value.IsDateTime && right.IsDateTime)
                {
                    value = ExpressionValue.FromTimeSpan(value.DateTime - right.DateTime);
                }
                else if (value.IsDateTime && right.IsTimeSpan)
                {
                    value = ExpressionValue.FromDateTime(value.DateTime - right.TimeSpan);
                }
                else if (value.IsTimeSpan && right.IsTimeSpan)
                {
                    value = ExpressionValue.FromTimeSpan(value.TimeSpan - right.TimeSpan);
                }
                else
                {
                    value = ExpressionValue.FromNumber(value.AsNumber() - right.AsNumber());
                }
            }
            else
            {
                return value;
            }
        }
    }

    private ExpressionValue ParseTerm()
    {
        var value = ParseUnary();
        while (true)
        {
            SkipWhitespace();
            if (Match('*'))
            {
                value = ExpressionValue.FromNumber(value.AsNumber() * ParseUnary().AsNumber());
            }
            else if (Match('/'))
            {
                value = ExpressionValue.FromNumber(value.AsNumber() / ParseUnary().AsNumber());
            }
            else if (Match('%'))
            {
                value = ExpressionValue.FromNumber(value.AsNumber() % ParseUnary().AsNumber());
            }
            else
            {
                return value;
            }
        }
    }

    private ExpressionValue ParseUnary()
    {
        SkipWhitespace();
        if (Match('+'))
        {
            return ParseUnary();
        }

        if (Match('-'))
        {
            return ExpressionValue.FromNumber(-ParseUnary().AsNumber());
        }

        if (Match('!'))
        {
            return ExpressionValue.FromBoolean(!ParseUnary().AsBoolean());
        }

        return ParsePower();
    }

    private ExpressionValue ParsePower()
    {
        var value = ParsePrimary();
        SkipWhitespace();
        if (Match('^'))
        {
            value = ExpressionValue.FromNumber(Math.Pow(value.AsNumber(), ParseUnary().AsNumber()));
        }

        return value;
    }

    private ExpressionValue ParsePrimary()
    {
        SkipWhitespace();
        if (Match('('))
        {
            var value = ParseExpression();
            SkipWhitespace();
            if (!Match(')'))
            {
                throw new FormatException("Missing closing parenthesis.");
            }

            return value;
        }

        if (Match('{'))
        {
            var start = _position;
            while (!AtEnd && Current != '}')
            {
                _position++;
            }

            if (AtEnd)
            {
                throw new FormatException("Missing closing brace for property name.");
            }

            var name = _text[start.._position].Trim();
            _position++;
            return ResolveProperty(name);
        }

        if (Current is '"' or '\'')
        {
            var quote = Current;
            _position++;
            var start = _position;
            var text = new System.Text.StringBuilder();
            while (!AtEnd)
            {
                if (Current == quote)
                {
                    _position++;
                    return ExpressionValue.FromString(text.ToString());
                }

                if (Current == '\\' && _position + 1 < _text.Length)
                {
                    _position++;
                    text.Append(Current);
                    _position++;
                }
                else
                {
                    text.Append(Current);
                    _position++;
                }
            }

            throw new FormatException("Missing closing quote.");
        }

        if (char.IsDigit(Current) || Current == '.')
        {
            var start = _position;
            while (char.IsDigit(Current) || Current == '.')
            {
                _position++;
            }

            if (Current == 'e' || Current == 'E')
            {
                _position++;
                if (Current == '+' || Current == '-')
                {
                    _position++;
                }

                var exponentStart = _position;
                while (char.IsDigit(Current))
                {
                    _position++;
                }

                if (_position == exponentStart)
                {
                    throw new FormatException("Invalid scientific notation.");
                }
            }

            if (!double.TryParse(_text[start.._position], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                throw new FormatException("Invalid number.");
            }

            return ExpressionValue.FromNumber(number);
        }

        if (char.IsLetter(Current) || Current == '_')
        {
            var start = _position;
            while (char.IsLetterOrDigit(Current) || Current is '_' or '.' or '[' or ']')
            {
                _position++;
            }

            var name = _text[start.._position];
            SkipWhitespace();
            if (Match('('))
            {
                return ParseFunction(name);
            }

            if (name.Equals("pi", StringComparison.OrdinalIgnoreCase))
            {
                return ExpressionValue.FromNumber(Math.PI);
            }

            if (name.Equals("e", StringComparison.OrdinalIgnoreCase))
            {
                return ExpressionValue.FromNumber(Math.E);
            }

            if (BooleanValueParser.TryParse(name, out var booleanLiteral))
            {
                return ExpressionValue.FromBoolean(booleanLiteral);
            }

            return ResolveProperty(name);
        }

        throw new FormatException($"Unexpected character '{Current}' at position {_position + 1}.");
    }

    private ExpressionValue ParseFunction(string name)
    {
        var arguments = new List<ExpressionValue>();
        SkipWhitespace();
        if (!Match(')'))
        {
            while (true)
            {
                arguments.Add(ParseExpression());
                SkipWhitespace();
                if (Match(')')) break;
                if (!Match(',')) throw new FormatException("Function arguments must be separated by commas.");
            }
        }

        var lowerName = name.ToLowerInvariant();

        if (lowerName is "color" or "colorfromstring" or "colorfromarray" or "colorfromstruct" or "colorfromjson" or "rgb" or "argb")
        {
            if (lowerName is "rgb" or "argb")
            {
                var expected = lowerName == "rgb" ? 3 : 4;
                if (arguments.Count != expected) throw new FormatException($"{name} requires {expected} components.");
                var components = arguments.Select(value => ColorComponent(value.AsNumber())).ToArray();
                var offset = expected == 4 ? 1 : 0;
                return ExpressionValue.FromColor(System.Drawing.Color.FromArgb(expected == 4 ? components[0] : 255, components[offset], components[offset + 1], components[offset + 2]));
            }
            if (arguments.Count != 1) throw new FormatException($"{name} requires one argument.");
            var argument = arguments[0];
            if (lowerName == "color" && argument.IsColor) return argument;
            if (lowerName is "color" or "colorfromstring")
            {
                if (!argument.IsString || !ColorValue.TryParse(argument.Text, out var color))
                    throw new FormatException("Invalid color string. Use a color name or #AARRGGBB.");
                return ExpressionValue.FromColor(color);
            }
            if (lowerName == "colorfromjson" && argument.IsString && ColorValue.TryParse(argument.Text, out var extractedColor))
                return ExpressionValue.FromColor(extractedColor);
            var json = argument.IsJson ? argument.Json : ParseJson(argument.AsString());
            return ExpressionValue.FromColor(ParseColorJson(json, lowerName));
        }

        if (lowerName is "colortostring" or "colortoarray" or "colortostruct" or "colortojson")
        {
            if (arguments.Count != 1 || !arguments[0].IsColor) throw new FormatException($"{name} requires one Color argument.");
            var color = arguments[0].Color;
            return lowerName switch
            {
                "colortostring" => ExpressionValue.FromString(ColorValue.Format(color)),
                "colortojson" => ExpressionValue.FromString(JsonSerializer.Serialize(ColorValue.Format(color))),
                "colortoarray" => ExpressionValue.FromJson(ToJsonElement(new[] { (int)color.A, color.R, color.G, color.B })),
                _ => ExpressionValue.FromJson(ToJsonElement(new { A = color.A, R = color.R, G = color.G, B = color.B }))
            };
        }

        if (lowerName is "string")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromString(arguments[0].AsString());
        }

        if (lowerName is "len" or "length")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromNumber(arguments[0].AsString().Length);
        }

        if (lowerName is "lower" or "lowercase" or "toupper" or "upper")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var text = arguments[0].AsString();
            return ExpressionValue.FromString(lowerName is "upper" or "toupper"
                ? text.ToUpperInvariant()
                : text.ToLowerInvariant());
        }

        if (lowerName is "trim" or "trimstart" or "trimend")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var text = arguments[0].AsString();
            var trimmed = lowerName switch
            {
                "trimstart" => text.TrimStart(),
                "trimend" => text.TrimEnd(),
                _ => text.Trim()
            };
            return ExpressionValue.FromString(trimmed);
        }

        if (lowerName is "substring")
        {
            if (arguments.Count is < 2 or > 3) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var text = arguments[0].AsString();
            var start = checked((int)arguments[1].AsNumber());
            if (start < 0 || start > text.Length) throw new ArgumentOutOfRangeException("startIndex");
            var result = arguments.Count == 3
                ? text.Substring(start, checked((int)arguments[2].AsNumber()))
                : text[start..];
            return ExpressionValue.FromString(result);
        }

        if (lowerName is "replace")
        {
            if (arguments.Count != 3) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromString(arguments[0].AsString().Replace(arguments[1].AsString(), arguments[2].AsString(), StringComparison.Ordinal));
        }

        if (lowerName is "concat")
        {
            if (arguments.Count < 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromString(string.Concat(arguments.Select(argument => argument.AsString())));
        }

        if (lowerName is "contains" or "startswith" or "endswith" or "indexof")
        {
            if (arguments.Count != 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var text = arguments[0].AsString();
            var search = arguments[1].AsString();
            if (lowerName == "contains") return ExpressionValue.FromBoolean(text.Contains(search, StringComparison.Ordinal));
            if (lowerName == "startswith") return ExpressionValue.FromBoolean(text.StartsWith(search, StringComparison.Ordinal));
            if (lowerName == "endswith") return ExpressionValue.FromBoolean(text.EndsWith(search, StringComparison.Ordinal));
            return ExpressionValue.FromNumber(text.IndexOf(search, StringComparison.Ordinal));
        }

        if (lowerName is "datetime")
        {
            if (arguments.Count == 1)
            {
                if (!DateTime.TryParse(arguments[0].AsString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var dateTime))
                    throw new FormatException("Invalid DateTime value.");
                return ExpressionValue.FromDateTime(dateTime);
            }

            if (arguments.Count is 3 or 6)
            {
                var year = checked((int)arguments[0].AsNumber());
                var month = checked((int)arguments[1].AsNumber());
                var day = checked((int)arguments[2].AsNumber());
                var hour = arguments.Count == 6 ? checked((int)arguments[3].AsNumber()) : 0;
                var minute = arguments.Count == 6 ? checked((int)arguments[4].AsNumber()) : 0;
                var second = arguments.Count == 6 ? checked((int)arguments[5].AsNumber()) : 0;
                return ExpressionValue.FromDateTime(new DateTime(year, month, day, hour, minute, second));
            }

            throw new FormatException($"Unknown function or argument count: '{name}'.");
        }

        if (lowerName is "now" or "utcnow")
        {
            if (arguments.Count != 0) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromDateTime(lowerName == "utcnow" ? DateTime.UtcNow : DateTime.Now);
        }

        if (lowerName == "today")
        {
            if (arguments.Count != 0) throw new FormatException($"Unknown function or argument count: '{name}'.");
            return ExpressionValue.FromDateTime(DateTime.Today);
        }

        if (lowerName == "date")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var dateTime = arguments[0].IsDateTime ? arguments[0].DateTime : ParseDateTime(arguments[0].AsString());
            return ExpressionValue.FromDateTime(dateTime.Date);
        }

        if (lowerName is "year" or "month" or "day" or "hour" or "minute" or "second" or "millisecond" or "dayofweek")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var dateTime = arguments[0].IsDateTime
                ? arguments[0].DateTime
                : ParseDateTime(arguments[0].AsString());
            var value = lowerName switch
            {
                "year" => dateTime.Year,
                "month" => dateTime.Month,
                "day" => dateTime.Day,
                "hour" => dateTime.Hour,
                "minute" => dateTime.Minute,
                "second" => dateTime.Second,
                "millisecond" => dateTime.Millisecond,
                "dayofweek" => (int)dateTime.DayOfWeek,
                _ => 0
            };
            return ExpressionValue.FromNumber(value);
        }

        if (lowerName is "adddays" or "addhours" or "addminutes" or "addseconds" or "addmilliseconds")
        {
            if (arguments.Count != 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var dateTime = arguments[0].IsDateTime ? arguments[0].DateTime : ParseDateTime(arguments[0].AsString());
            var amount = arguments[1].AsNumber();
            var result = lowerName switch
            {
                "adddays" => dateTime.AddDays(amount),
                "addhours" => dateTime.AddHours(amount),
                "addminutes" => dateTime.AddMinutes(amount),
                "addseconds" => dateTime.AddSeconds(amount),
                "addmilliseconds" => dateTime.AddMilliseconds(amount),
                _ => dateTime
            };
            return ExpressionValue.FromDateTime(result);
        }

        if (lowerName is "formatdatetime")
        {
            if (arguments.Count is < 1 or > 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var dateTime = arguments[0].IsDateTime ? arguments[0].DateTime : ParseDateTime(arguments[0].AsString());
            var format = arguments.Count == 2 ? arguments[1].AsString() : "O";
            return ExpressionValue.FromString(dateTime.ToString(format, CultureInfo.InvariantCulture));
        }

        if (lowerName is "timespan")
        {
            if (arguments.Count == 1)
            {
                if (arguments[0].IsTimeSpan) return arguments[0];
                if (!TimeSpan.TryParse(arguments[0].AsString(), CultureInfo.InvariantCulture, out var timeSpan))
                    throw new FormatException("Invalid TimeSpan value.");
                return ExpressionValue.FromTimeSpan(timeSpan);
            }

            if (arguments.Count == 3)
            {
                var hours = checked((int)arguments[0].AsNumber());
                var minutes = checked((int)arguments[1].AsNumber());
                var seconds = checked((int)arguments[2].AsNumber());
                return ExpressionValue.FromTimeSpan(new TimeSpan(hours, minutes, seconds));
            }

            if (arguments.Count is 4 or 5)
            {
                var days = checked((int)arguments[0].AsNumber());
                var hours = checked((int)arguments[1].AsNumber());
                var minutes = checked((int)arguments[2].AsNumber());
                var seconds = checked((int)arguments[3].AsNumber());
                var milliseconds = arguments.Count == 5 ? checked((int)arguments[4].AsNumber()) : 0;
                return ExpressionValue.FromTimeSpan(new TimeSpan(days, hours, minutes, seconds, milliseconds));
            }

            throw new FormatException($"Unknown function or argument count: '{name}'.");
        }

        if (lowerName is "days" or "hours" or "minutes" or "seconds" or "milliseconds"
            or "totaldays" or "totalhours" or "totalminutes" or "totalseconds" or "totalmilliseconds")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var timeSpan = arguments[0].IsTimeSpan ? arguments[0].TimeSpan : ParseTimeSpan(arguments[0].AsString());
            var value = lowerName switch
            {
                "days" => (double)timeSpan.Days,
                "hours" => (double)timeSpan.Hours,
                "minutes" => (double)timeSpan.Minutes,
                "seconds" => (double)timeSpan.Seconds,
                "milliseconds" => (double)timeSpan.Milliseconds,
                "totaldays" => timeSpan.TotalDays,
                "totalhours" => timeSpan.TotalHours,
                "totalminutes" => timeSpan.TotalMinutes,
                "totalseconds" => timeSpan.TotalSeconds,
                "totalmilliseconds" => timeSpan.TotalMilliseconds,
                _ => 0d
            };
            return ExpressionValue.FromNumber(value);
        }

        if (lowerName is "addtimespan" or "subtracttimespan")
        {
            if (arguments.Count != 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var left = arguments[0].IsTimeSpan ? arguments[0].TimeSpan : ParseTimeSpan(arguments[0].AsString());
            var right = arguments[1].IsTimeSpan ? arguments[1].TimeSpan : ParseTimeSpan(arguments[1].AsString());
            return ExpressionValue.FromTimeSpan(lowerName == "addtimespan" ? left + right : left - right);
        }

        if (lowerName == "json")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var value = ToJsonSerializable(arguments[0]);
            var json = JsonSerializer.SerializeToElement(value);
            return ExpressionValue.FromJson(json);
        }

        if (lowerName is "jsonparse" or "parsejson")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var json = ParseJson(arguments[0].AsString());
            return ExpressionValue.FromJson(json);
        }

        if (lowerName is "jsonobject" or "jsoncreate")
        {
            if (arguments.Count == 0 || arguments.Count % 2 != 0)
                throw new FormatException($"Unknown function or argument count: '{name}'.");
            var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var index = 0; index < arguments.Count; index += 2)
            {
                var key = arguments[index].AsString();
                if (string.IsNullOrEmpty(key)) throw new FormatException("JSON object property names cannot be empty.");
                if (!fields.TryAdd(key, ToJsonSerializable(arguments[index + 1])))
                    throw new FormatException($"Duplicate JSON object property '{key}'.");
            }

            return ExpressionValue.FromJson(JsonSerializer.SerializeToElement(fields));
        }

        if (lowerName is "jsonarray" or "arrayjson")
        {
            var items = arguments.Select(ToJsonSerializable).ToArray();
            return ExpressionValue.FromJson(JsonSerializer.SerializeToElement(items));
        }

        if (lowerName is "jsonget" or "jsonextract" or "jsonvalue")
        {
            if (arguments.Count is < 1 or > 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var root = arguments[0].IsJson ? arguments[0].Json : ParseJson(arguments[0].AsString());
            if (arguments.Count == 1) return ExpressionValue.FromJson(root);
            if (!TryResolveJsonPath(root, arguments[1].AsString(), out var value))
                throw new KeyNotFoundException($"JSON path '{arguments[1].AsString()}' was not found.");
            return FromJsonElement(value);
        }

        if (lowerName is "jsonstring" or "jsonserialize")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var json = arguments[0].IsJson ? arguments[0].Json : ParseJson(arguments[0].AsString());
            return ExpressionValue.FromString(json.GetRawText());
        }

        if (lowerName is "jsonhas")
        {
            if (arguments.Count != 2) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var root = arguments[0].IsJson ? arguments[0].Json : ParseJson(arguments[0].AsString());
            return ExpressionValue.FromBoolean(TryResolveJsonPath(root, arguments[1].AsString(), out _));
        }

        if (lowerName is "jsonlength")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            var root = arguments[0].IsJson ? arguments[0].Json : ParseJson(arguments[0].AsString());
            var length = root.ValueKind switch
            {
                JsonValueKind.Array => root.GetArrayLength(),
                JsonValueKind.Object => root.EnumerateObject().Count(),
                JsonValueKind.String => root.GetString()?.Length ?? 0,
                _ => throw new FormatException("JSON length requires an object, array, or string.")
            };
            return ExpressionValue.FromNumber(length);
        }

        if (lowerName is "jsonvalid")
        {
            if (arguments.Count != 1) throw new FormatException($"Unknown function or argument count: '{name}'.");
            try
            {
                _ = ParseJson(arguments[0].AsString());
                return ExpressionValue.FromBoolean(true);
            }
            catch (JsonException)
            {
                return ExpressionValue.FromBoolean(false);
            }
        }

        var number = lowerName switch
        {
            "abs" when arguments.Count == 1 => Math.Abs(arguments[0].AsNumber()),
            "sqrt" when arguments.Count == 1 => Math.Sqrt(arguments[0].AsNumber()),
            "sin" when arguments.Count == 1 => Math.Sin(arguments[0].AsNumber()),
            "cos" when arguments.Count == 1 => Math.Cos(arguments[0].AsNumber()),
            "tan" when arguments.Count == 1 => Math.Tan(arguments[0].AsNumber()),
            "asin" when arguments.Count == 1 => Math.Asin(arguments[0].AsNumber()),
            "acos" when arguments.Count == 1 => Math.Acos(arguments[0].AsNumber()),
            "atan" when arguments.Count == 1 => Math.Atan(arguments[0].AsNumber()),
            "exp" when arguments.Count == 1 => Math.Exp(arguments[0].AsNumber()),
            "ln" or "log" when arguments.Count == 1 => name.Equals("log", StringComparison.OrdinalIgnoreCase)
                ? Math.Log10(arguments[0].AsNumber()) : Math.Log(arguments[0].AsNumber()),
            "log10" when arguments.Count == 1 => Math.Log10(arguments[0].AsNumber()),
            "floor" when arguments.Count == 1 => Math.Floor(arguments[0].AsNumber()),
            "ceil" when arguments.Count == 1 => Math.Ceiling(arguments[0].AsNumber()),
            "round" when arguments.Count == 1 => Math.Round(arguments[0].AsNumber()),
            "min" when arguments.Count == 2 => Math.Min(arguments[0].AsNumber(), arguments[1].AsNumber()),
            "max" when arguments.Count == 2 => Math.Max(arguments[0].AsNumber(), arguments[1].AsNumber()),
            "pow" when arguments.Count == 2 => Math.Pow(arguments[0].AsNumber(), arguments[1].AsNumber()),
            "clamp" when arguments.Count == 3 => Math.Clamp(arguments[0].AsNumber(), arguments[1].AsNumber(), arguments[2].AsNumber()),
            _ => throw new FormatException($"Unknown function or argument count: '{name}'.")
        };

        return ExpressionValue.FromNumber(number);
    }

    private ExpressionValue ResolveProperty(string name)
    {
        if (_variables.TryGet<double>(name, out var number))
        {
            return ExpressionValue.FromNumber(number);
        }

        if (_variables.TryGet<int>(name, out var integer))
        {
            return ExpressionValue.FromNumber(integer);
        }

        if (_variables.TryGet<bool>(name, out var boolean))
        {
            return ExpressionValue.FromBoolean(boolean);
        }

        if (_variables.TryGet<string>(name, out var text))
        {
            return ExpressionValue.FromString(text);
        }

        if (_variables.TryGet<DateTime>(name, out var dateTime))
        {
            return ExpressionValue.FromDateTime(dateTime);
        }

        if (_variables.TryGet<TimeSpan>(name, out var timeSpan))
        {
            return ExpressionValue.FromTimeSpan(timeSpan);
        }

        if (_variables.TryGet<System.Drawing.Color>(name, out var color))
        {
            return ExpressionValue.FromColor(color);
        }

        if (_variables.TryGet<StructValue>(name, out var topLevelStructure) && topLevelStructure is not null)
        {
            return ExpressionValue.FromJson(ToJsonElement(ToSerializableProperty(topLevelStructure)));
        }

        if (_variables.TryGet<ArrayValue>(name, out var topLevelArray) && topLevelArray is not null)
        {
            return ExpressionValue.FromJson(ToJsonElement(ToSerializableProperty(topLevelArray)));
        }

        for (var separatorIndex = Math.Max(name.LastIndexOf('.'), name.LastIndexOf('[')); separatorIndex > 0;
             separatorIndex = Math.Max(name.LastIndexOf('.', separatorIndex - 1), name.LastIndexOf('[', separatorIndex - 1)))
        {
            var variableName = name[..separatorIndex];
            object? rootValue = null;
            if (_variables.TryGet<StructValue>(variableName, out var structure) && structure is not null)
            {
                rootValue = structure;
            }
            else if (_variables.TryGet<ArrayValue>(variableName, out var array) && array is not null)
            {
                rootValue = array;
            }

            var propertyPath = name[separatorIndex..];
            if (propertyPath.Length > 0 && propertyPath[0] == '.')
            {
                propertyPath = propertyPath[1..];
            }

            if (rootValue is null || !TryResolveValuePath(rootValue, propertyPath, out var value))
            {
                continue;
            }

            return value switch
            {
                double fieldNumber => ExpressionValue.FromNumber(fieldNumber),
                int fieldInteger => ExpressionValue.FromNumber(fieldInteger),
                bool fieldBoolean => ExpressionValue.FromBoolean(fieldBoolean),
                string fieldText => ExpressionValue.FromString(fieldText),
                DateTime fieldDateTime => ExpressionValue.FromDateTime(fieldDateTime),
                TimeSpan fieldTimeSpan => ExpressionValue.FromTimeSpan(fieldTimeSpan),
                System.Drawing.Color fieldColor => ExpressionValue.FromColor(fieldColor),
                StructValue fieldStructure => ExpressionValue.FromJson(ToJsonElement(ToSerializableProperty(fieldStructure))),
                ArrayValue fieldArray => ExpressionValue.FromJson(ToJsonElement(ToSerializableProperty(fieldArray))),
                _ => throw new FormatException($"Property path '{name}' has an unsupported value.")
            };
        }

        throw new KeyNotFoundException($"Property '{name}' was not found or has an unsupported type.");
    }

    private static bool TryResolveValuePath(object current, string path, out object? value)
    {
        if (path.Length == 0)
        {
            value = current;
            return true;
        }

        if (current is ArrayValue array && path[0] == '[')
        {
            var closingBracket = path.IndexOf(']');
            if (closingBracket > 1 &&
                int.TryParse(path.AsSpan(1, closingBracket - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
                index >= 0 && index < array.Items.Count)
            {
                var remainingPath = path[(closingBracket + 1)..];
                if (remainingPath.Length > 0 && remainingPath[0] == '.')
                {
                    remainingPath = remainingPath[1..];
                }

                if (array.Items[index] is { } item)
                {
                    return TryResolveValuePath(item, remainingPath, out value);
                }
            }
        }

        if (current is StructValue structure)
        {
            if (structure.Fields.TryGetValue(path, out value))
            {
                return true;
            }

            for (var separatorIndex = path.Length - 1; separatorIndex > 0;
                 separatorIndex = path.LastIndexOf('.', separatorIndex - 1))
            {
                if (structure.Fields.TryGetValue(path[..separatorIndex], out var fieldValue) && fieldValue is not null &&
                    TryResolveValuePath(fieldValue, path[(separatorIndex + 1)..], out value))
                {
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static JsonElement ParseJson(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static int ColorComponent(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 255 || value != Math.Truncate(value))
            throw new FormatException("Color components must be integers between 0 and 255.");
        return (int)value;
    }

    private static int ColorComponent(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
            throw new FormatException("Color components must be integers between 0 and 255.");
        return ColorComponent(number);
    }

    private static System.Drawing.Color ParseColorJson(JsonElement json, string function)
    {
        if (function == "colorfromjson" && json.ValueKind == JsonValueKind.String &&
            ColorValue.TryParse(json.GetString(), out var color)) return color;

        if (function != "colorfromstruct" && json.ValueKind == JsonValueKind.Array)
        {
            var values = json.EnumerateArray().Select(ColorComponent).ToArray();
            if (values.Length is not (3 or 4)) throw new FormatException("A color array must contain [R,G,B] or [A,R,G,B].");
            var offset = values.Length == 4 ? 1 : 0;
            return System.Drawing.Color.FromArgb(values.Length == 4 ? values[0] : 255, values[offset], values[offset + 1], values[offset + 2]);
        }

        if (function != "colorfromarray" && json.ValueKind == JsonValueKind.Object)
        {
            var components = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in json.EnumerateObject())
            {
                if (field.Name.Equals("A", StringComparison.OrdinalIgnoreCase) || field.Name.Equals("R", StringComparison.OrdinalIgnoreCase) ||
                    field.Name.Equals("G", StringComparison.OrdinalIgnoreCase) || field.Name.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    if (!components.TryAdd(field.Name, ColorComponent(field.Value))) throw new FormatException($"Duplicate color component '{field.Name}'.");
                }
            }
            if (!components.TryGetValue("R", out var red) || !components.TryGetValue("G", out var green) || !components.TryGetValue("B", out var blue))
                throw new FormatException("A color object requires R, G, and B components.");
            return System.Drawing.Color.FromArgb(components.TryGetValue("A", out var alpha) ? alpha : 255, red, green, blue);
        }

        throw new FormatException("Invalid color representation.");
    }

    private static JsonElement ToJsonElement(object? value) => JsonSerializer.SerializeToElement(value);

    private static object? ToJsonSerializable(ExpressionValue value) => value.Kind switch
    {
        ExpressionValueKind.Number => value.Number,
        ExpressionValueKind.Boolean => value.Boolean,
        ExpressionValueKind.String => value.Text,
        ExpressionValueKind.DateTime => value.DateTime.ToString("O", CultureInfo.InvariantCulture),
        ExpressionValueKind.TimeSpan => value.TimeSpan.ToString("c", CultureInfo.InvariantCulture),
        ExpressionValueKind.Color => ColorValue.Format(value.Color),
        ExpressionValueKind.Json => value.Json.Clone(),
        _ => null
    };

    private static object? ToSerializableProperty(object? value) => value switch
    {
        StructValue structure => structure.Fields.ToDictionary(
            field => field.Key,
            field => ToSerializableProperty(field.Value),
            StringComparer.Ordinal),
        ArrayValue array => array.Items.Select(ToSerializableProperty).ToArray(),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
        System.Drawing.Color color => ColorValue.Format(color),
        _ => value
    };

    private static ExpressionValue FromJsonElement(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => ExpressionValue.FromString(value.GetString() ?? string.Empty),
        JsonValueKind.Number when value.TryGetDouble(out var number) => ExpressionValue.FromNumber(number),
        JsonValueKind.True => ExpressionValue.FromBoolean(true),
        JsonValueKind.False => ExpressionValue.FromBoolean(false),
        JsonValueKind.Null => ExpressionValue.FromString(string.Empty),
        JsonValueKind.Object or JsonValueKind.Array => ExpressionValue.FromJson(value),
        _ => ExpressionValue.FromJson(value)
    };

    private static bool TryResolveJsonPath(JsonElement root, string path, out JsonElement value)
    {
        var current = root;
        var position = 0;
        SkipJsonPathSeparators(path, ref position);

        if (position == path.Length)
        {
            value = root;
            return true;
        }

        while (position < path.Length)
        {
            if (path[position] == '[')
            {
                var closing = path.IndexOf(']', position + 1);
                if (closing < 0 || !int.TryParse(path.AsSpan(position + 1, closing - position - 1),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    current.ValueKind != JsonValueKind.Array || index < 0 || index >= current.GetArrayLength())
                {
                    value = default;
                    return false;
                }

                current = current[index];
                position = closing + 1;
            }
            else
            {
                var end = position;
                while (end < path.Length && path[end] is not '.' and not '[')
                {
                    end++;
                }

                var propertyName = path[position..end];
                if (current.ValueKind != JsonValueKind.Object ||
                    !current.TryGetProperty(propertyName, out current))
                {
                    value = default;
                    return false;
                }

                position = end;
            }

            SkipJsonPathSeparators(path, ref position);
        }

        value = current;
        return true;
    }

    private static void SkipJsonPathSeparators(string path, ref int position)
    {
        while (position < path.Length && path[position] is '.' or '[')
        {
            if (path[position] == '.') position++;
            else break;
        }
    }

    private static DateTime ParseDateTime(string text)
    {
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var value))
        {
            return value;
        }

        throw new FormatException("Invalid DateTime value.");
    }

    private static TimeSpan ParseTimeSpan(string text)
    {
        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException("Invalid TimeSpan value.");
    }

    private static bool Compare<T>(T left, T right, string operatorText) where T : IComparable<T>
    {
        var comparison = left.CompareTo(right);
        return operatorText switch
        {
            "<" => comparison < 0,
            "<=" => comparison <= 0,
            ">" => comparison > 0,
            ">=" => comparison >= 0,
            _ => throw new InvalidOperationException("Unsupported comparison operator.")
        };
    }

    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }

    private bool MatchOperator(string expected)
    {
        SkipWhitespace();
        if (_position + expected.Length > _text.Length ||
            !string.Equals(_text.Substring(_position, expected.Length), expected, StringComparison.Ordinal))
        {
            return false;
        }

        _position += expected.Length;
        return true;
    }

    private bool Match(char expected)
    {
        SkipWhitespace();
        if (AtEnd || Current != expected)
        {
            return false;
        }

        _position++;
        return true;
    }
}
