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

        static abstract ImageSource CreateDiamondImage(Color color);
        TreeItemNode Create();
        string ToString();
    }
}