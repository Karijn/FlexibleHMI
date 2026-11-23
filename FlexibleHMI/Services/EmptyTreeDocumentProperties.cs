using FlexibleHMI.Interfaces;
using System.IO;

namespace FlexibleHMI.Services;

/// <summary>Provides no document properties and rejects documents containing any.</summary>
public sealed class EmptyTreeDocumentProperties : ITreeDocumentProperties
{
    /// <inheritdoc/>
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public IReadOnlyList<TreeDocumentProperty> GetProperties() => Array.Empty<TreeDocumentProperty>();

    /// <inheritdoc/>
    public void LoadProperties(IReadOnlyList<TreeDocumentProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Count > 0)
        {
            throw new InvalidDataException("The document contains properties, but no property provider is available.");
        }
    }
}
