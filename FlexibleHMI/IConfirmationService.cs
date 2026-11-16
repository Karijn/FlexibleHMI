namespace FlexibleHMI;

public enum UnsavedChangesDecision
{
    Save,
    Discard,
    Cancel
}

public interface IConfirmationService
{
    bool Confirm(string message, string title);

    UnsavedChangesDecision ConfirmSaveChanges();
}
