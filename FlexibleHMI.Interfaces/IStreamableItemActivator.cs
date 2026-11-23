using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

/// <summary>Describes and creates a streamable item type.</summary>
public interface IStreamableItemActivator
{
    /// <summary>Gets the item type identifier.</summary>
    public string StreamItemType { get; }

    /// <summary>Gets the display name of the item type.</summary>
    public string TypeName { get; }

    /// <summary>Gets the display group.</summary>
    public string Group { get; }

    /// <summary>Gets the item icon.</summary>
    public ImageSource Image { get; }

    /// <summary>Creates a streamable item.</summary>
    /// <returns>The new item.</returns>
    IStreamableItem Create();
}


