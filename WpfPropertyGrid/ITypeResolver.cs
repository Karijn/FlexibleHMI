namespace WpfPropertyGrid;
/// <summary>
/// Defines the service used to resolve type names.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Resolves a type by its full name.
    /// </summary>
    /// <param name="fullName">The name of the type to resolve.</param>
    /// <param name="throwOnError">true to throw if the type cannot be resolved; otherwise, false.</param>
    /// <returns>The resolved type, or null when it cannot be found and throwing is disabled.</returns>
    Type? ResolveType(string fullName, bool throwOnError);
}
