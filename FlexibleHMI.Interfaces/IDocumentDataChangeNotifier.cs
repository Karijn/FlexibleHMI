namespace FlexibleHMI.Interfaces;

/// <summary>Notifies the document when an item's persisted data changes.</summary>
public interface IDocumentDataChangeNotifier
{
    /// <summary>Occurs when document data has changed.</summary>
    event EventHandler? DocumentDataChanged;
}
