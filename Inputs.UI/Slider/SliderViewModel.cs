using PropertyManager.Interfaces;

namespace Inputs.UI.Slider;

internal sealed class SliderViewModel : NumericInputViewModel
{
    public SliderViewModel(IVariableHandler variables) : base(variables, "Slider") => InitializeInput();
    public override string Kind => "input-slider";
    protected override InputViewModelBase CreateCopy() => new SliderViewModel(Variables);
}
