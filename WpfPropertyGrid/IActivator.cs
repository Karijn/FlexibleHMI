namespace WpfPropertyGrid;
/// <summary>
/// Defines the service used to create property-grid objects.
/// </summary>
public interface IActivator
{
    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="args">Optional constructor arguments.</param>
    /// <returns>The created instance, or null if no instance could be created.</returns>
    object? CreateInstance(Type type, params object?[]? args);
    /// <summary>
    /// Creates an instance of the specified type parameter.
    /// </summary>
    /// <param name="args">Optional constructor arguments.</param>
    /// <returns>The created instance, or null if no instance could be created.</returns>
    T? CreateInstance<T>(params object?[]? args);
}
