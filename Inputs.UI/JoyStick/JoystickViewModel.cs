using PropertyManager.Interfaces;
using System.Windows.Media;

namespace Inputs.UI.JoyStick;

internal sealed class JoystickViewModel : NumericInputViewModel
{
    private double _yValue;
    public JoystickViewModel(IVariableHandler variables) : base(variables, "Joystick")
    {
        SavedMinimum = -1;
        SavedMaximum = 1;
        BackColor = Colors.SteelBlue;
        BackColorDisabled = Colors.LightGray;
        BorderColor = Colors.Black;

        InitializeInput();
    }
    public override string Kind => "input-joystick";
    public string YPropertyName
    {
        get => SavedYPropertyName;
        set
        {
            if (SetProperty(ref SavedYPropertyName, value?.Trim() ?? ""))
                SettingsChanged();
        }
    }
    public double YValue
    {
        get => _yValue;
        set
        {
            if (IsAvailable)
                WriteNumber(YPropertyName, value, nameof(YValue));
        }
    }
    public double YStep => StepFor(YPropertyName);
    protected override InputViewModelBase CreateCopy() => new JoystickViewModel(Variables);
    protected override bool IsRelevantVariable(string name) => base.IsRelevantVariable(name) || name == YPropertyName;
    protected override void SettingsLoaded()
    {
        base.SettingsLoaded();
        OnPropertyChanged(nameof(YPropertyName));
    }
    protected override bool RefreshAdditionalValues(List<string> errors)
    {
        var available = !string.IsNullOrEmpty(YPropertyName) && ReadNumber(YPropertyName, out var value);
        if (available)
        {
            ReadNumber(YPropertyName, out var current);
            SetProperty(ref _yValue, Math.Clamp(current, Minimum, Maximum), nameof(YValue));
            if (current < Minimum || current > Maximum)
                errors.Add("Y property value is outside the configured range.");
        }
        else
            errors.Add("Select an available finite numeric Y property.");

        if (available && !HasIntegerInRange(YPropertyName))
        {
            available = false;
            errors.Add("The range contains no valid integer for the Y property.");
        }
        if (PropertyName == YPropertyName && !string.IsNullOrEmpty(PropertyName))
        {
            available = false;
            errors.Add("Joystick X and Y properties must be different.");
        }
        OnPropertyChanged(nameof(YStep));
        return available;
    }
}
