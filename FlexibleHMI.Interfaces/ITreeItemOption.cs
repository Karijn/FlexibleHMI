using System.Windows.Media;

namespace FlexibleHMI.Interfaces
{
    /// <summary>Describes an item type available for insertion into the document tree.</summary>
    public interface ITreeItemOption
    {
        /// <summary>Gets the item type description.</summary>
        string Description { get; }
        /// <summary>Gets the display group.</summary>
        string Group { get; }
        /// <summary>Gets the item icon.</summary>
        ImageSource Image { get; }
        /// <summary>Gets the item kind identifier.</summary>
        string Kind { get; }
        /// <summary>Gets the display name.</summary>
        string Name { get; }
        /// <summary>Creates a tree item.</summary>
        /// <returns>The created item.</returns>
        TreeItemNode Create();
        /// <summary>Returns a display representation.</summary>
        /// <returns>The display text.</returns>
        string ToString();
    }
}