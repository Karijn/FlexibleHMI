using System.Text.Json;

namespace FlexibleHMI.Interfaces;

/// <summary>Persists item-specific data within a tree document.</summary>
public interface ITreeItemDocumentData
{
    /// <summary>Serializes the item's document data.</summary>
    /// <returns>The serialized JSON value.</returns>
    JsonElement SaveDocumentData();

    /// <summary>Restores the item's document data.</summary>
    /// <param name="data">The serialized JSON value.</param>
    void LoadDocumentData(JsonElement data);
}
