namespace FlexibleHMI.ViewModels;

internal sealed class ConfirmationDialogViewModel(string message, string dialogTitle)
{
    public string Message { get; } = message;

    public string DialogTitle { get; } = dialogTitle;
}
