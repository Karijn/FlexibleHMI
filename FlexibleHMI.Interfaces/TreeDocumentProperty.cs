namespace FlexibleHMI.Interfaces;

public sealed record TreeDocumentProperty(string Name, string Type, string Value);

public interface ITreeDocumentProperties
{
    event EventHandler? Changed;

    IReadOnlyList<TreeDocumentProperty> GetProperties();

    void LoadProperties(IReadOnlyList<TreeDocumentProperty> properties);
}
