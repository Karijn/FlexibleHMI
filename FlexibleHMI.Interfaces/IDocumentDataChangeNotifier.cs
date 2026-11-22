namespace FlexibleHMI.Interfaces;

public interface IDocumentDataChangeNotifier
{
    event EventHandler? DocumentDataChanged;
}
