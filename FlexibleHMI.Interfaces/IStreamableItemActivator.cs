using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

public interface IStreamableItemActivator
{
    public string StreamItemType { get; }

    public string TypeName { get; }

    public string Group { get; }

    public ImageSource Image { get; }

    IStreamableItem Create();
}


