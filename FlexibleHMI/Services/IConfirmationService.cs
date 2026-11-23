namespace FlexibleHMI.Services;

/// <summary>Represents the user's choice when prompted to resolve unsaved changes.</summary>
public enum UnsavedChangesDecision
{
    /// <summary>Save the changes before continuing.</summary>
    Save,
    /// <summary>Discard the changes and continue.</summary>
    Discard,
    /// <summary>Keep the current document open and abandon the requested operation.</summary>
    Cancel
}

/// <summary>Displays confirmation dialogs used by the application.</summary>
public interface IConfirmationService
{
    /// <summary>Asks the user to confirm an operation.</summary>
    /// <param name="message">The confirmation message.</param>
    /// <param name="title">The dialog title.</param>
    /// <returns><see langword="true"/> when the user confirms; otherwise, <see langword="false"/>.</returns>
    bool Confirm(string message, string title);

    /// <summary>Asks how to handle unsaved changes.</summary>
    /// <returns>The user's decision to save, discard, or cancel.</returns>
    UnsavedChangesDecision ConfirmSaveChanges();
}
