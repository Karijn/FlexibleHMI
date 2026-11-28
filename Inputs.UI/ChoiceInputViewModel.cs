using CommunityToolkit.Mvvm.Input;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;

namespace Inputs.UI;

internal abstract partial class ChoiceInputViewModel : InputViewModelBase
{
    private string? _selectedOption;
    protected ChoiceInputViewModel(IVariableHandler variables, string title) : base(variables, title) => UpdateOptions();
    public ObservableCollection<string> Options { get; } = [];
    public string OptionsText
    {
        get => SavedOptionsText;
        set
        {
            if (SetProperty(ref SavedOptionsText, value ?? ""))
            {
                UpdateOptions();
                SettingsChanged();
                OnPropertyChanged(nameof(SelectedOption));
            }
        }
    }
    public string? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (!IsAvailable || value is null || !Options.Contains(value))
                return;
            if (Variables.TryGet<string>(PropertyName, out var current) && current is not null && current != value)
                Variables.Set(PropertyName, value);
            RefreshValue();
            OnPropertyChanged(nameof(SelectedOption));
        }
    }
    [RelayCommand]
    private void SelectOption(string? option) => SelectedOption = option;
    private void UpdateOptions()
    {
        var values = OptionsText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Options.Clear();
        foreach (var value in values.Distinct(StringComparer.Ordinal))
            Options.Add(value);
    }
    protected override IEnumerable<string> GetNames() => Variables.GetVariableNames<string>();
    protected override void SettingsLoaded()
    {
        OnPropertyChanged(nameof(OptionsText));
        UpdateOptions();
        OnPropertyChanged(nameof(SelectedOption));
    }

    protected override void RefreshValue()
    {
        var errors = new List<string>();
        var available = !string.IsNullOrEmpty(PropertyName) && Variables.TryGet<string>(PropertyName, out var value) && value is not null;
        if (available)
        {
            Variables.TryGet<string>(PropertyName, out var current);
            SetProperty(ref _selectedOption, Options.Contains(current!) ? current : null, nameof(SelectedOption));
            if (!Options.Contains(current!))
                errors.Add("The property value is not in the options; select an option to change it.");
        }
        else
        {
            SetProperty(ref _selectedOption, null, nameof(SelectedOption));
            errors.Add("Select an available String property.");
        }
        if (Options.Count == 0)
        {
            available = false;
            errors.Add("Enter at least one option.");
        }
        IsAvailable = available;
        Status = string.Join(" ", errors);
    }
}
