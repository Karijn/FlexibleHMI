namespace PropertyManager.Interfaces;

/// <summary>Parses Boolean values and supported text aliases.</summary>
public static class BooleanValueParser
{
    /// <summary>Parses Boolean text or the case-insensitive aliases On, Off, Yes, and No.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed value, or <see langword="false"/> on failure.</param>
    /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? text, out bool value)
    {
        if (bool.TryParse(text, out value))
        {
            return true;
        }

        if (text is not null)
        {
            if (text.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (text.Equals("Off", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
        }

        value = false;
        return false;
    }
}
