using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

/// <summary>Describes a named streamable item and its display metadata.</summary>
public interface IStreamableItem
{
    /// <summary>Gets or sets the item type identifier.</summary>
    public string StreamItemType { get; set; }

    /// <summary>Gets the display name of the item type.</summary>
    public string TypeName { get; }

    /// <summary>Gets or sets the item name.</summary>
    public string Name { get; set; }

    /// <summary>Gets the optional item icon.</summary>
    public ImageSource? Image { get; }
}
