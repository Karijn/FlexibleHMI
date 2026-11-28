using PropertyManager.Interfaces;

namespace Inputs.UI;

internal sealed class CheckBoxViewModel : BooleanInputViewModel
{
    public CheckBoxViewModel(IVariableHandler variables) : base(variables, "Check box") => InitializeInput();
    public override string Kind => "input-checkbox";
    protected override InputViewModelBase CreateCopy() => new CheckBoxViewModel(Variables);
}
