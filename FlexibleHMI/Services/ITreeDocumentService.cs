using FlexibleHMI.Interfaces;

namespace FlexibleHMI.Services;

/// <summary>Contains the data loaded from an HMI tree document.</summary>
/// <param name="FilePath">The path from which the document was loaded.</param>
/// <param name="Items">The document's root tree items.</param>
/// <param name="Properties">The document-level properties.</param>
public sealed record LoadedTreeDocument(
    string FilePath,
    IReadOnlyList<TreeItemNode> Items,
    IReadOnlyList<TreeDocumentProperty> Properties);

/// <summary>Loads and saves HMI tree documents.</summary>
public interface ITreeDocumentService
{
    /// <summary>Saves the supplied tree and document properties.</summary>
    /// <param name="items">The root items to save.</param>
    /// <param name="currentFilePath">The current document path, if one is assigned.</param>
    /// <param name="saveAs"><see langword="true"/> to prompt for a destination; otherwise, reuse the current path when available.</param>
    /// <returns>The saved file path, or <see langword="null"/> if the save dialog was canceled.</returns>
    string? Save(IReadOnlyCollection<TreeItemNode> items, string? currentFilePath, bool saveAs);

    /// <summary>Prompts for an HMI document and loads it if a file is selected.</summary>
    /// <returns>The loaded document, or <see langword="null"/> if the dialog was canceled.</returns>
    LoadedTreeDocument? Load();

    /// <summary>Loads an HMI document from the specified path.</summary>
    /// <param name="filePath">The path of the document to load.</param>
    /// <returns>The loaded document and its tree and document properties.</returns>
    LoadedTreeDocument Load(string filePath);
}
