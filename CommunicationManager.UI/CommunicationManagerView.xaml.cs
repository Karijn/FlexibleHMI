using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CommunicationManager.UI;

public partial class CommunicationManagerView : UserControl
{
    public CommunicationManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.OldValue is CommunicationManagerViewModel oldViewModel)
        {
            oldViewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }

        if (eventArgs.NewValue is CommunicationManagerViewModel newViewModel)
        {
            newViewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.Action == NotifyCollectionChangedAction.Add && eventArgs.NewItems is { Count: > 0 })
        {
            var newItem = eventArgs.NewItems[^1];
            Dispatcher.BeginInvoke(new Action(() => MessageListView.ScrollIntoView(newItem)), DispatcherPriority.Loaded);
        }
    }

    private void DirectMessageTextBox_PreviewKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (sender is not TextBox { DataContext: CommunicationChannelViewModel channelViewModel })
        {
            return;
        }

        if (eventArgs.Key == Key.Enter)
        {
            eventArgs.Handled = true;
            if (channelViewModel.SendDirectMessageCommand.CanExecute(null))
            {
                channelViewModel.SendDirectMessageCommand.Execute(null);
            }

            return;
        }

        var recalled = eventArgs.Key switch
        {
            Key.Up => channelViewModel.RecallPreviousDirectMessage(),
            Key.Down => channelViewModel.RecallNextDirectMessage(),
            _ => false
        };

        if (recalled && sender is TextBox textBox)
        {
            eventArgs.Handled = true;
            textBox.CaretIndex = textBox.Text.Length;
        }
    }
}
