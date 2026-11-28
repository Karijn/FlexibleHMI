using PropertyManager.Interfaces;

namespace Inputs.UI;

internal sealed class OnOffButtonViewModel : BooleanInputViewModel
{
    public OnOffButtonViewModel(IVariableHandler variables) : base(variables, "On-off button") => InitializeInput();
    public override string Kind => "input-onoff";
    protected override InputViewModelBase CreateCopy() => new OnOffButtonViewModel(Variables);
}
