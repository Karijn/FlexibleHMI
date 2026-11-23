using System.Drawing;
using System.Globalization;

namespace PropertyManager.Interfaces;

/// <summary>Converts property colors between text and <see cref="Color"/> values.</summary>
public static class ColorValue
{
    /// <summary>Parses a non-system color name or RGB/ARGB hexadecimal text.</summary>
    /// <param name="text">A color name or a value in #RGB, #ARGB, #RRGGBB, or #AARRGGBB form.</param>
    /// <param name="color">The parsed color, or the default color on failure.</param>
    /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text[0] == '#')
        {
            var hex = text.AsSpan(1);
            if (hex.Length is 3 or 4)
            {
                uint value = 0;
                foreach (var character in hex)
                {
                    if (!byte.TryParse(character.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var digit)) return false;
                    value = (value << 8) | (uint)(digit * 17);
                }
                if (hex.Length == 3) value |= 0xFF000000;
                color = Color.FromArgb(unchecked((int)value));
                return true;
            }
            if (hex.Length is 6 or 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                if (hex.Length == 6) argb |= 0xFF000000;
                color = Color.FromArgb(unchecked((int)argb));
                return true;
            }
            return false;
        }

        if (char.IsLetter(text[0]) && Enum.TryParse<KnownColor>(text, true, out var known) && Enum.IsDefined(known))
        {
            var named = Color.FromKnownColor(known);
            if (!named.IsSystemColor)
            {
                color = Color.FromArgb(named.ToArgb());
                return true;
            }
        }
        return false;
    }

    /// <summary>Formats a color as an eight-digit ARGB hexadecimal string.</summary>
    /// <param name="color">The color to format.</param>
    /// <returns>The color in #AARRGGBB form.</returns>
    public static string Format(Color color) => $"#{color.ToArgb():X8}";
}
