using System.Windows.Media;

namespace FlexibleHMI.Interfaces
{
    public interface ITreeItemOption
    {
        string Description { get; }
        string Group { get; }
        ImageSource Image { get; }
        string Kind { get; }
        string Name { get; }
        TreeItemNode Create();
        string ToString();
    }
}