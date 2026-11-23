using PropertyManager.Interfaces;

namespace VUMeter.UI;

internal static class LegendVisibility
{
    /// <summary>
    /// Evaluates the effective legend visibility and returns any binding status.
    /// </summary>
    public static (bool Visible, string Status) Evaluate(IVariableHandler variables, string propertyName, bool manualValue)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return (manualValue, string.Empty);
        if (variables.TryGet<bool>(propertyName, out var value)) return (value, string.Empty);
        return (false, $"Legend property '{propertyName}' must be an available Boolean.");
    }
}
