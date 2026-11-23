namespace WpfPropertyGrid;
/// <summary>
/// Stores services used by property-grid conversion, activation, type resolution, and text formatting.
/// </summary>
public class PropertyGridServiceProvider : IServiceProvider
{
    /// <summary>
    /// Gets the current.
    /// </summary>
    public static PropertyGridServiceProvider Current => _current;

    private readonly ConcurrentDictionary<Type, object?> _services = new();
    private static readonly PropertyGridServiceProvider _current = new();
    /// <summary>
    /// Initializes a new instance of PropertyGridServiceProvider.
    /// </summary>
    public PropertyGridServiceProvider()
    {
        ResetDefaultServices();
    }

    /// <summary>
    /// Ensures that default converter, decamelizer, type resolver, and activator services are registered.
    /// </summary>
    protected virtual void ResetDefaultServices()
    {
        if (!_services.ContainsKey(typeof(IConverter)))
        {
            _services[typeof(IConverter)] = new BaseConverter();
        }

        if (!_services.ContainsKey(typeof(IDecamelizer)))
        {
            _services[typeof(IDecamelizer)] = new BaseDecamelizer();
        }

        if (!_services.ContainsKey(typeof(ITypeResolver)))
        {
            _services[typeof(ITypeResolver)] = new BaseTypeResolver();
        }

        if (!_services.ContainsKey(typeof(IActivator)))
        {
            _services[typeof(IActivator)] = new BaseActivator();
        }
    }
    /// <summary>
    /// Gets the service registered for the requested service type.
    /// </summary>
    public T GetService<T>() => (T)GetService(typeof(T))!;
    /// <summary>
    /// Registers or replaces a service and returns the previous registration.
    /// </summary>
    public virtual object? SetService(Type serviceType, object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        _services.TryGetValue(serviceType, out var previous);
        _services[serviceType] = service;
        ResetDefaultServices();
        return previous;
    }
    /// <inheritdoc/>
    public virtual object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        _services.TryGetValue(serviceType, out var value);
        return value;
    }
}
