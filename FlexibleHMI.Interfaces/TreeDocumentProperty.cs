namespace FlexibleHMI.Interfaces;

/// <summary>Represents a serialized document-level property.</summary>
/// <param name="Name">The property name.</param>
/// <param name="Type">The serialized type identifier.</param>
/// <param name="Value">The serialized value.</param>
public sealed record TreeDocumentProperty(string Name, string Type, string Value);

/// <summary>Provides document-level properties for persistence.</summary>
public interface ITreeDocumentProperties
{
    /// <summary>Occurs when document properties change.</summary>
    event EventHandler? Changed;

    /// <summary>Gets the properties to save.</summary>
    /// <returns>The serialized property records.</returns>
    IReadOnlyList<TreeDocumentProperty> GetProperties();

    /// <summary>Restores properties from document records.</summary>
    /// <param name="properties">The serialized property records.</param>
    void LoadProperties(IReadOnlyList<TreeDocumentProperty> properties);
}
