using FlexibleHMI.Interfaces;
using FlexibleHMI.Views;
using System.IO;
using System.Text.Json;

namespace FlexibleHMI.Services;

public sealed class TreeDocumentService : ITreeDocumentService
{
    private const int CurrentDocumentVersion = 1;
    private const int MaximumTreeDepth = 256;
    private const int MaximumItemCount = 100_000;

    public TreeDocumentService(IDictionary<string, TreeItemOption> creators)
    {
        _creators = creators;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private static IDictionary<string, TreeItemOption> _creators;

    public string? Save(IReadOnlyCollection<TreeItemNode> items, string? currentFilePath, bool saveAs)
    {
        var path = !saveAs && !string.IsNullOrWhiteSpace(currentFilePath)
            ? currentFilePath
            : ThemedFileDialog.ShowSave(currentFilePath);

        if (path is null)
        {
            return null;
        }

        var document = new HmiTreeDocument
        {
            Version = CurrentDocumentVersion,
            Items = items.Select(ToDocumentItem).ToList()
        };

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        File.WriteAllText(path, json);
        return path;
    }

    public LoadedTreeDocument? Load()
    {
        var path = ThemedFileDialog.ShowOpen();
        return path is null ? null : Load(path);
    }

    public LoadedTreeDocument Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var document = JsonSerializer.Deserialize<HmiTreeDocument>(json, SerializerOptions)
            ?? throw new InvalidDataException("The selected file does not contain a tree document.");

        if (document.Version != CurrentDocumentVersion)
        {
            throw new InvalidDataException($"Unsupported HMI document version: {document.Version}.");
        }

        if (document.Items is null)
        {
            throw new InvalidDataException("The HMI document is missing its root items.");
        }

        var itemCount = 0;
        var items = new List<TreeItemNode>(document.Items.Count);
        foreach (var item in document.Items)
        {
            items.Add(FromDocumentItem(item, 1, ref itemCount));
        }

        return new LoadedTreeDocument(filePath, items);
    }

    private static HmiTreeItem ToDocumentItem(TreeItemNode item) => new()
    {
        Kind = item.Kind,
        Name = item.Name,
        Description = item.Description,
        Children = item.Children.Select(ToDocumentItem).ToList()
    };

    private static TreeItemNode FromDocumentItem(HmiTreeItem? item, int depth, ref int itemCount)
    {
        if (item is null)
        {
            throw new InvalidDataException("The HMI document contains a missing tree item.");
        }

        if (depth > MaximumTreeDepth)
        {
            throw new InvalidDataException($"Tree depth exceeds the maximum of {MaximumTreeDepth}.");
        }

        if (++itemCount > MaximumItemCount)
        {
            throw new InvalidDataException($"The document exceeds the maximum of {MaximumItemCount} items.");
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            item.Name = "No Name";
            //throw new InvalidDataException("Tree item names cannot be empty.");
        }

        if (item.Children is null)
        {
            throw new InvalidDataException($"Tree item '{item.Name}' is missing its children collection.");
        }

        string kind = item.Kind;
        TreeItemNode node = null;
        if (_creators.TryGetValue(kind, out TreeItemOption creator))
        {
            node = creator.Create();
            node.Name = creator.Name;

            //node = item.Kind switch
            //{
            //    "leaf" => new LeafItemNode() { Name = item.Name, Description = item.Description },
            //    "singleChild" => new SingleChildItemNode() { Name = item.Name, Description = item.Description },
            //    "collection" => new CollectionItemNode() { Name = item.Name, Description = item.Description },
            //    "grid" => new GridItemNode() { Name = item.Name, Description = item.Description },
            //    "Vu meter" => new VuMeterNode() { Name = item.Name, Description = item.Description },
            //    _ => throw new InvalidDataException($"Unsupported tree item kind '{item.Kind}'.")
            //};

            foreach (var child in item.Children)
            {
                var childNode = FromDocumentItem(child, depth + 1, ref itemCount);
                try
                {
                    node.Children.Add(childNode);
                }
                catch (InvalidOperationException exception)
                {
                    throw new InvalidDataException($"Tree item '{item.Name}' has too many children.", exception);
                }
            }
        }
        return node;
    }

    private sealed class HmiTreeDocument
    {
        public HmiTreeDocument()
        {
        }

        public int Version { get; set; }

        public List<HmiTreeItem>? Items { get; set; }
    }

    private sealed class HmiTreeItem
    {
        public HmiTreeItem()
        {
        }

        public string Kind { get; set; } = "";

        public string Name { get; set; }

        public string Description { get; set; } = "";

        public List<HmiTreeItem>? Children { get; set; }

        
    }
}
