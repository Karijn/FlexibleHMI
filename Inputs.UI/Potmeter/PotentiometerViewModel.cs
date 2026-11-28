using PropertyManager.Interfaces;

namespace Inputs.UI.Potmeter;

internal sealed class PotentiometerViewModel : NumericInputViewModel
{
    public PotentiometerViewModel(IVariableHandler variables) : base(variables, "Potentiometer") => InitializeInput();
    public override string Kind => "input-potentiometer";
    protected override InputViewModelBase CreateCopy() => new PotentiometerViewModel(Variables);
}
