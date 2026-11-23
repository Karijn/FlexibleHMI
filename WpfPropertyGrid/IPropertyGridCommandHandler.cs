namespace WpfPropertyGrid;
/// <summary>
/// Handles routed command availability and execution for a property value.
/// </summary>
public interface IPropertyGridCommandHandler
{
    /// <summary>
    /// Determines whether the routed command can execute for the property value.
    /// </summary>
    /// <param name="property">The property whose value is handling the command.</param>
    /// <param name="sender">The command source.</param>
    /// <param name="e">The routed command event arguments to update.</param>
    void CanExecute(PropertyGridProperty property, object? sender, CanExecuteRoutedEventArgs e);
    /// <summary>
    /// Handles execution of a routed command for the property value.
    /// </summary>
    /// <param name="property">The property whose value is handling the command.</param>
    /// <param name="sender">The command source.</param>
    /// <param name="e">The routed command event arguments.</param>
    void Executed(PropertyGridProperty property, object? sender, ExecutedRoutedEventArgs e);
}