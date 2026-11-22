namespace PropertyManager.Interfaces;

public static class BooleanValueParser
{
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
