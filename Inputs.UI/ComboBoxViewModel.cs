using PropertyManager.Interfaces;

namespace Inputs.UI;

internal sealed class ComboBoxViewModel : ChoiceInputViewModel
{
    public ComboBoxViewModel(IVariableHandler variables) : base(variables, "Combo box") => InitializeInput();
    public override string Kind => "input-combobox";
    protected override InputViewModelBase CreateCopy() => new ComboBoxViewModel(Variables);
}
