using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;

namespace Inputs.UI;

internal enum InputKind { ComboBox, CheckBox, RadioButtonSet, OnOffButton, Joystick, Potentiometer, Slider }

internal sealed partial class InputViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly IVariableHandler _variables;
    private readonly Dispatcher _dispatcher;
    private readonly InputKind _kind;
    private bool _detached = true;
    private int _version;
    private string _propertyName = "";
    private string _yPropertyName = "";
    private string _optionsText = "Option 1\nOption 2";
    private double _minimum = 0;
    private double _maximum = 100;
    private string _status = "";
    private bool _isAvailable;
    private bool _booleanValue;
    private double _numericValue;
    private double _yValue;
    private string? _selectedOption;

    public InputViewModel(IVariableHandler variables, InputKind kind)
    {
        _variables = variables;
        _kind = kind;
        _dispatcher = Dispatcher.CurrentDispatcher;
        if (kind == InputKind.Joystick) { _minimum = -1; _maximum = 1; }
        Name = Title(kind);
        Description = "Property-bound input control";
        Image = TreeItemOption.CreateDiamondImage(Colors.SteelBlue);
        UpdateOptions();
        RefreshNames();
        RefreshValue();
    }

    public static string Key(InputKind kind) => kind switch
    {
        InputKind.ComboBox => "input-combobox", InputKind.CheckBox => "input-checkbox",
        InputKind.RadioButtonSet => "input-radiobuttonset", InputKind.OnOffButton => "input-onoff",
        InputKind.Joystick => "input-joystick", InputKind.Potentiometer => "input-potentiometer",
        InputKind.Slider => "input-slider", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static string Title(InputKind kind) => kind switch
    {
        InputKind.ComboBox => "Combo box", InputKind.CheckBox => "Check box", InputKind.RadioButtonSet => "Radio button set",
        InputKind.OnOffButton => "On-off button", InputKind.Joystick => "Joystick", InputKind.Potentiometer => "Potentiometer",
        InputKind.Slider => "Slider", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public override string Kind => Key(_kind);
    public override int MaxChildren => 0;
    public override string ChildRule => "No children";
    public override bool CanAcceptChildren => false;
    public event EventHandler? DocumentDataChanged;
    public ObservableCollection<string> PropertyNames { get; } = [];
    public ObservableCollection<string> Options { get; } = [];
    public bool IsComboBox => _kind == InputKind.ComboBox;
    public bool IsCheckBox => _kind == InputKind.CheckBox;
    public bool IsRadioButtonSet => _kind == InputKind.RadioButtonSet;
    public bool IsOnOffButton => _kind == InputKind.OnOffButton;
    public bool IsJoystick => _kind == InputKind.Joystick;
    public bool IsPotentiometer => _kind == InputKind.Potentiometer;
    public bool IsSlider => _kind == InputKind.Slider;
    public bool IsChoice => IsComboBox || IsRadioButtonSet;
    public bool IsBoolean => IsCheckBox || IsOnOffButton;
    public bool IsNumeric => IsJoystick || IsPotentiometer || IsSlider;
    public string PropertyName { get => _propertyName; set { if (SetProperty(ref _propertyName, value?.Trim() ?? "")) SettingsChanged(); } }
    public string YPropertyName { get => _yPropertyName; set { if (SetProperty(ref _yPropertyName, value?.Trim() ?? "")) SettingsChanged(); } }
    public string OptionsText { get => _optionsText; set { if (SetProperty(ref _optionsText, value ?? "")) { UpdateOptions(); SettingsChanged(); OnPropertyChanged(nameof(SelectedOption)); } } }
    public double Minimum { get => _minimum; set { ValidateRange(value, Maximum); if (SetProperty(ref _minimum, value)) SettingsChanged(); } }
    public double Maximum { get => _maximum; set { ValidateRange(Minimum, value); if (SetProperty(ref _maximum, value)) SettingsChanged(); } }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool IsAvailable { get => _isAvailable; private set => SetProperty(ref _isAvailable, value); }
    public bool BooleanValue
    {
        get => _booleanValue;
        set
        {
            if (!IsAvailable || !IsBoolean) return;
            if (_variables.TryGet<bool>(PropertyName, out var current) && current != value) _variables.Set(PropertyName, value);
            RefreshValue();
            OnPropertyChanged(nameof(BooleanValue));
        }
    }
    public string? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (!IsAvailable || !IsChoice || value is null || !Options.Contains(value)) return;
            if (_variables.TryGet<string>(PropertyName, out var current) && current is not null && current != value) _variables.Set(PropertyName, value);
            RefreshValue();
            OnPropertyChanged(nameof(SelectedOption));
        }
    }
    public double NumericValue { get => _numericValue; set { if (IsAvailable && IsNumeric) WriteNumber(PropertyName, value); } }
    public double YValue { get => _yValue; set { if (IsAvailable && IsJoystick) WriteNumber(YPropertyName, value); } }
    public double NumericStep => StepFor(PropertyName);
    public double YStep => StepFor(YPropertyName);
    public string OnOffLabel => BooleanValue ? "ON" : "OFF";
    private double StepFor(string name) => _variables.TryGet<int>(name, out _) ? Math.Max(1, (Maximum - Minimum) / 100) : (Maximum - Minimum) / 100;
    private bool HasIntegerInRange(string name) => !_variables.TryGet<int>(name, out _) ||
        Math.Max(Math.Ceiling(Minimum), int.MinValue) <= Math.Min(Math.Floor(Maximum), int.MaxValue);

    [RelayCommand]
    private void SelectOption(string? option) => SelectedOption = option;

    private static void ValidateRange(double min, double max)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min >= max || !double.IsFinite(max - min))
            throw new ArgumentOutOfRangeException(nameof(min), "Range endpoints must be finite and minimum must be less than maximum.");
    }
    private void UpdateOptions()
    {
        var values = OptionsText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Options.Clear();
        foreach (var value in values.Distinct(StringComparer.Ordinal)) Options.Add(value);
    }
    private void SettingsChanged() { RefreshValue(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); }
    private void WriteNumber(string name, double value)
    {
        if (!double.IsFinite(value)) { Status = "Input value must be finite."; return; }
        value = Math.Clamp(value, Minimum, Maximum);
        if (_variables.TryGet<int>(name, out _))
        {
            if (!HasIntegerInRange(name)) { RefreshValue(); return; }
            var rounded = Math.Clamp(Math.Round(value), Math.Max(Math.Ceiling(Minimum), int.MinValue), Math.Min(Math.Floor(Maximum), int.MaxValue));
            _variables.Set(name, (int)rounded);
        }
        else if (_variables.TryGet<double>(name, out var current) && double.IsFinite(current)) _variables.Set(name, value);
        RefreshValue();
        OnPropertyChanged(name == PropertyName ? nameof(NumericValue) : nameof(YValue));
    }
    private bool ReadNumber(string name, out double value)
    {
        if (_variables.TryGet<double>(name, out value)) return double.IsFinite(value);
        if (_variables.TryGet<int>(name, out var integer)) { value = integer; return true; }
        value = 0; return false;
    }
    private void RefreshValue()
    {
        var errors = new List<string>();
        var available = false;
        if (IsBoolean)
        {
            available = !string.IsNullOrEmpty(PropertyName) && _variables.TryGet<bool>(PropertyName, out var value);
            if (available) { _variables.TryGet<bool>(PropertyName, out var current); SetProperty(ref _booleanValue, current, nameof(BooleanValue)); OnPropertyChanged(nameof(OnOffLabel)); }
            else errors.Add("Select an available Boolean property.");
        }
        else if (IsChoice)
        {
            available = !string.IsNullOrEmpty(PropertyName) && _variables.TryGet<string>(PropertyName, out var value) && value is not null;
            if (available)
            {
                _variables.TryGet<string>(PropertyName, out var current);
                SetProperty(ref _selectedOption, Options.Contains(current!) ? current : null, nameof(SelectedOption));
                if (!Options.Contains(current!)) errors.Add("The property value is not in the options; select an option to change it.");
            }
            else { SetProperty(ref _selectedOption, null, nameof(SelectedOption)); errors.Add("Select an available String property."); }
            if (Options.Count == 0) { available = false; errors.Add("Enter at least one option."); }
        }
        else
        {
            available = !string.IsNullOrEmpty(PropertyName) && ReadNumber(PropertyName, out var value);
            if (available) { ReadNumber(PropertyName, out var current); SetProperty(ref _numericValue, Math.Clamp(current, Minimum, Maximum), nameof(NumericValue)); if (current < Minimum || current > Maximum) errors.Add("Property value is outside the configured range."); }
            else errors.Add("Select an available finite numeric property.");
            if (available && !HasIntegerInRange(PropertyName)) { available = false; errors.Add("The range contains no valid integer for the property."); }
            if (IsJoystick)
            {
                var yAvailable = !string.IsNullOrEmpty(YPropertyName) && ReadNumber(YPropertyName, out var y);
                if (yAvailable) { ReadNumber(YPropertyName, out var current); SetProperty(ref _yValue, Math.Clamp(current, Minimum, Maximum), nameof(YValue)); if (current < Minimum || current > Maximum) errors.Add("Y property value is outside the configured range."); }
                else errors.Add("Select an available finite numeric Y property.");
                if (yAvailable && !HasIntegerInRange(YPropertyName)) { yAvailable = false; errors.Add("The range contains no valid integer for the Y property."); }
                if (PropertyName == YPropertyName && !string.IsNullOrEmpty(PropertyName)) { yAvailable = false; errors.Add("Joystick X and Y properties must be different."); }
                available &= yAvailable;
            }
            OnPropertyChanged(nameof(NumericStep));
            OnPropertyChanged(nameof(YStep));
        }
        IsAvailable = available;
        Status = string.Join(" ", errors);
    }
    private void RefreshNames()
    {
        IEnumerable<string> names = IsBoolean ? _variables.GetVariableNames<bool>() : IsChoice ? _variables.GetVariableNames<string>() :
            _variables.GetVariableNames<double>().Concat(_variables.GetVariableNames<int>());
        var ordered = names.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        for (var i = PropertyNames.Count - 1; i >= 0; i--) if (!ordered.Contains(PropertyNames[i], StringComparer.Ordinal)) PropertyNames.RemoveAt(i);
        foreach (var name in ordered) if (!PropertyNames.Contains(name, StringComparer.Ordinal)) PropertyNames.Add(name);
    }
    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var namesChanged = change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType();
        var version = Volatile.Read(ref _version);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _version)) return;
            if (namesChanged) RefreshNames();
            if (change.Name == PropertyName || (IsJoystick && change.Name == YPropertyName)) RefreshValue();
        }
        if (_dispatcher.CheckAccess()) Apply(); else _dispatcher.BeginInvoke(new Action(Apply));
    }
    public void OnDocumentItemDetached() { _detached = true; Interlocked.Increment(ref _version); _variables.VariableChanged -= OnVariableChanged; }
    public void OnDocumentItemReattached()
    {
        if (!_detached) return;
        _detached = false; Interlocked.Increment(ref _version); _variables.VariableChanged += OnVariableChanged;
        RefreshNames(); RefreshValue();
    }
    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(PropertyName, YPropertyName, OptionsText, Minimum, Maximum));
    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid input settings.");
        if (settings.PropertyName is null || settings.YPropertyName is null || settings.OptionsText is null ||
            !double.IsFinite(settings.Minimum) || !double.IsFinite(settings.Maximum) || settings.Minimum >= settings.Maximum || !double.IsFinite(settings.Maximum - settings.Minimum))
            throw new InvalidDataException("Invalid input settings or range.");
        _minimum = settings.Minimum; _maximum = settings.Maximum;
        OnPropertyChanged(nameof(Minimum)); OnPropertyChanged(nameof(Maximum));
        PropertyName = settings.PropertyName; YPropertyName = settings.YPropertyName; OptionsText = settings.OptionsText;
        RefreshValue();
    }
    public override TreeItemNode Copy() { var copy = new InputViewModel(_variables, _kind); Copy(copy); copy.LoadDocumentData(SaveDocumentData()); return copy; }
    private sealed record Settings(string PropertyName, string YPropertyName, string OptionsText, double Minimum, double Maximum);
}
