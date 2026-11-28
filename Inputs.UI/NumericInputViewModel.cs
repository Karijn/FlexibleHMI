using PropertyManager.Interfaces;
using System.Windows.Media;

namespace Inputs.UI;

internal abstract class NumericInputViewModel(IVariableHandler variables, string title) : InputViewModelBase(variables, title)
{
    private double _numericValue;
    public double Minimum
    {
        get => SavedMinimum;
        set
        {
            ValidateRange(value, Maximum);
            if (SetProperty(ref SavedMinimum, value))
                SettingsChanged();
        }
    }
    public double Maximum
    {
        get => SavedMaximum;
        set
        {
            ValidateRange(Minimum, value);
            if (SetProperty(ref SavedMaximum, value))
                SettingsChanged();
        }
    }
    public double NumericValue
    {
        get => _numericValue;
        set
        {
            if (IsAvailable)
                WriteNumber(PropertyName, value);
        }
    }


    public double NumericStep => StepFor(PropertyName);
    protected double StepFor(string name) => Variables.TryGet<int>(name, out _) ? Math.Max(1, (Maximum - Minimum) / 100) : (Maximum - Minimum) / 100;
    protected bool HasIntegerInRange(string name) => !Variables.TryGet<int>(name, out _) ||
        Math.Max(Math.Ceiling(Minimum), int.MinValue) <= Math.Min(Math.Floor(Maximum), int.MaxValue);
    private static void ValidateRange(double min, double max)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min >= max || !double.IsFinite(max - min))
            throw new ArgumentOutOfRangeException(nameof(min), "Range endpoints must be finite and minimum must be less than maximum.");
    }
    protected void WriteNumber(string name, double value, string propertyName = nameof(NumericValue))
    {
        if (!double.IsFinite(value))
        {
            Status = "Input value must be finite.";
            return;
        }
        value = Math.Clamp(value, Minimum, Maximum);
        if (Variables.TryGet<int>(name, out _))
        {
            if (!HasIntegerInRange(name))
            {
                RefreshValue();
                return;
            }
            var rounded = Math.Clamp(Math.Round(value), Math.Max(Math.Ceiling(Minimum), int.MinValue), Math.Min(Math.Floor(Maximum), int.MaxValue));
            Variables.Set(name, (int)rounded);
        }
        else if (Variables.TryGet<double>(name, out var current) && double.IsFinite(current))
            Variables.Set(name, value);
        RefreshValue();
        OnPropertyChanged(propertyName);
    }

    protected bool ReadNumber(string name, out double value)
    {
        if (Variables.TryGet<double>(name, out value))
            return double.IsFinite(value);
        if (Variables.TryGet<int>(name, out var integer))
        {
            value = integer;
            return true;
        }
        value = 0;
        return false;
    }
    protected override IEnumerable<string> GetNames() => Variables.GetVariableNames<double>().Concat(Variables.GetVariableNames<int>());
    protected override void SettingsLoaded()
    {
        OnPropertyChanged(nameof(Minimum));
        OnPropertyChanged(nameof(Maximum));
    }
    protected virtual bool RefreshAdditionalValues(List<string> errors) => true;
    protected override void RefreshValue()
    {
        var errors = new List<string>();
        var available = !string.IsNullOrEmpty(PropertyName) && ReadNumber(PropertyName, out var value);
        if (available)
        {
            ReadNumber(PropertyName, out var current);
            SetProperty(ref _numericValue, Math.Clamp(current, Minimum, Maximum), nameof(NumericValue));
            if (current < Minimum || current > Maximum)
                errors.Add("Property value is outside the configured range.");
        }
        else
            errors.Add("Select an available finite numeric property.");
        if (available && !HasIntegerInRange(PropertyName))
        {
            available = false;
            errors.Add("The range contains no valid integer for the property.");
        }
        available &= RefreshAdditionalValues(errors);
        OnPropertyChanged(nameof(NumericStep));
        IsAvailable = available;
        Status = string.Join(" ", errors);
    }
}
