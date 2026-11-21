using FlexibleHMI.Interfaces;
using System.IO;

namespace FlexibleHMI.Services;

public sealed class EmptyTreeDocumentProperties : ITreeDocumentProperties
{
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public IReadOnlyList<TreeDocumentProperty> GetProperties() => Array.Empty<TreeDocumentProperty>();

    public void LoadProperties(IReadOnlyList<TreeDocumentProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Count > 0)
        {
            throw new InvalidDataException("The document contains properties, but no property provider is available.");
        }
    }
}
