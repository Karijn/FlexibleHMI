using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace FunctionGenerator.UI;

public partial class FunctionGeneratorViewModel : TreeItemNode, ITreeItemDocumentData
{
    private readonly IVariableHandler _variableHandler;
    private int _nextOutputNumber = 1;

    public FunctionGeneratorViewModel(IVariableHandler variableHandler)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        Name = "Function Generator";
        Image = TreeItemOption.CreateDiamondImage(Colors.Olive);
        Outputs.Add(CreateOutput());
    }

    public ObservableCollection<FunctionOutputViewModel> Outputs { get; } = [];

    public override string Kind => "functionGenerator";

    public override int MaxChildren => -1;

    public override string ChildRule => "Any number of children";

    public override bool CanAcceptChildren => true;

    [RelayCommand]
    private void AddOutput() => Outputs.Add(CreateOutput());

    [RelayCommand]
    private void RemoveOutput(FunctionOutputViewModel? output)
    {
        if (output is not null && Outputs.Remove(output))
        {
            output.StopGeneration();
        }
    }

    public override TreeItemNode Copy()
    {
        var copy = new FunctionGeneratorViewModel(_variableHandler);
        Copy(copy);
        copy.Outputs.Clear();
        foreach (var output in Outputs)
        {
            copy.Outputs.Add(output.Copy(_variableHandler));
        }

        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        Outputs.Select(output => new FunctionOutputSettings(
            output.PropertyName,
            output.Waveform,
            output.Frequency,
            output.Amplitude,
            output.Offset,
            output.UpdateIntervalMilliseconds)).ToList());

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<List<FunctionOutputSettings>>()
            ?? throw new InvalidDataException("Function generator data is invalid.");

        foreach (var output in settings)
        {
            if (string.IsNullOrWhiteSpace(output.PropertyName) ||
                !Enum.IsDefined(output.Waveform) ||
                !double.IsFinite(output.Frequency) || output.Frequency is <= 0 or > 1000 ||
                !double.IsFinite(output.Amplitude) || !double.IsFinite(output.Offset) ||
                output.UpdateIntervalMilliseconds is < 1 or > 10000)
            {
                throw new InvalidDataException("The document contains invalid function generator settings.");
            }
        }

        foreach (var output in Outputs)
        {
            output.StopGeneration();
        }

        Outputs.Clear();
        foreach (var output in settings)
        {
            Outputs.Add(new FunctionOutputViewModel(_variableHandler, output.PropertyName)
            {
                Waveform = output.Waveform,
                Frequency = output.Frequency,
                Amplitude = output.Amplitude,
                Offset = output.Offset,
                UpdateIntervalMilliseconds = output.UpdateIntervalMilliseconds
            });
        }

        _nextOutputNumber = Outputs.Count + 1;
    }

    private FunctionOutputViewModel CreateOutput()
    {
        string name;
        do
        {
            name = $"Signal{_nextOutputNumber++}";
        }
        while (Outputs.Any(output => string.Equals(output.PropertyName, name, StringComparison.Ordinal)));

        return new FunctionOutputViewModel(_variableHandler, name);
    }

    public sealed record FunctionOutputSettings(
        string PropertyName,
        SignalWaveform Waveform,
        double Frequency,
        double Amplitude,
        double Offset,
        int UpdateIntervalMilliseconds);
}

public class FunctionGeneratorCreator : TreeItemOption
{
    public FunctionGeneratorCreator(IServiceProvider serviceProvider)
        : base("functionGenerator", "FunctionGenerator", "Function Generation", "Generate waveforms into properties", TreeItemOption.CreateDiamondImage(Colors.Red), serviceProvider)
    {
    }
}
