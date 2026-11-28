using PropertyManager.Interfaces;

namespace Inputs.UI;

internal sealed class RadioButtonSetViewModel : ChoiceInputViewModel
{
    public RadioButtonSetViewModel(IVariableHandler variables) : base(variables, "Radio button set") => InitializeInput();
    public override string Kind => "input-radiobuttonset";
    protected override InputViewModelBase CreateCopy() => new RadioButtonSetViewModel(Variables);
}
