using PropertyManager.Interfaces;

namespace Inputs.UI;

internal abstract class BooleanInputViewModel(IVariableHandler variables, string title) : InputViewModelBase(variables, title)
{
    private bool _booleanValue;
    public bool BooleanValue
    {
        get => _booleanValue;
        set
        {
            if (!IsAvailable)
                return;
            if (Variables.TryGet<bool>(PropertyName, out var current) && current != value)
                Variables.Set(PropertyName, value);
            RefreshValue();
            OnPropertyChanged(nameof(BooleanValue));
        }
    }

    public string OnOffLabel => BooleanValue ? "ON" : "OFF";
    protected override IEnumerable<string> GetNames() => Variables.GetVariableNames<bool>();
    protected override void RefreshValue()
    {
        var available = !string.IsNullOrEmpty(PropertyName) && Variables.TryGet<bool>(PropertyName, out _);
        if (available)
        {
            Variables.TryGet<bool>(PropertyName, out var current);
            SetProperty(ref _booleanValue, current, nameof(BooleanValue));
            OnPropertyChanged(nameof(OnOffLabel));
        }
        IsAvailable = available;
        Status = available ? "" : "Select an available Boolean property.";
    }
}
