namespace FlexibleHMI.Interfaces;

/// <summary>Handles document-item removal and subsequent reattachment.</summary>
public interface IDocumentItemDetached
{
    /// <summary>Releases or suspends resources when the item is detached.</summary>
    void OnDocumentItemDetached();

    /// <summary>Restores resources or subscriptions after reattachment.</summary>
    void OnDocumentItemReattached();
}
