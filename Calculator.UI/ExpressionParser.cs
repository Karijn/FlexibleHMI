using PropertyManager.Interfaces;
using System.Globalization;

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
                value = ExpressionValue.FromNumber(value.AsNumber() + ParseTerm().AsNumber());
            }
            else if (Match('-'))
            {
                value = ExpressionValue.FromNumber(value.AsNumber() - ParseTerm().AsNumber());
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

        var number = name.ToLowerInvariant() switch
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
                StructValue => throw new FormatException($"Property path '{name}' must end at a numeric or boolean field."),
                ArrayValue => throw new FormatException($"Property path '{name}' must index an array to a numeric or boolean value."),
                _ => throw new FormatException($"Property path '{name}' has an unsupported value.")
            };
        }

        throw new KeyNotFoundException($"Property '{name}' was not found or is not numeric or boolean.");
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
