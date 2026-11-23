namespace WpfPropertyGrid;
/// <summary>
/// Provides type resolution through the configured property-grid type resolver.
/// </summary>
public static class TypeResolutionService
{
    /// <summary>
    /// Resolves a type by name, optionally throwing when the type cannot be found.
    /// </summary>
    public static Type? ResolveType(string fullName, bool throwOnError = false) => PropertyGridServiceProvider.Current.GetService<ITypeResolver>().ResolveType(fullName, throwOnError);
}
