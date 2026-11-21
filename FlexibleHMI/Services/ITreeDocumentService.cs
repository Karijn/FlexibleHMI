using FlexibleHMI.Interfaces;

namespace FlexibleHMI.Services;

public sealed record LoadedTreeDocument(
    string FilePath,
    IReadOnlyList<TreeItemNode> Items,
    IReadOnlyList<TreeDocumentProperty> Properties);

public interface ITreeDocumentService
{
    string? Save(IReadOnlyCollection<TreeItemNode> items, string? currentFilePath, bool saveAs);

    LoadedTreeDocument? Load();

    LoadedTreeDocument Load(string filePath);
}
