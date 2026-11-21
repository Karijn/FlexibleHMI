using System.Windows.Media;


namespace FlexibleHMI.Interfaces;

public interface IStreamableItem
{
    public string StreamItemType { get; set; }

    public string TypeName { get; }

    public string Name { get; set; }

    public ImageSource? Image { get; }
}
