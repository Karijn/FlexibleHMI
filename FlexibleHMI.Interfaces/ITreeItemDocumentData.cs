using System.Text.Json;

namespace FlexibleHMI.Interfaces;

public interface ITreeItemDocumentData
{
    JsonElement SaveDocumentData();

    void LoadDocumentData(JsonElement data);
}
