namespace WpfPropertyGrid;
/// <summary>
/// Resolves a type from its assembly-qualified or full name.
/// </summary>
public class BaseTypeResolver : ITypeResolver
{
    /// <inheritdoc/>
    public virtual Type? ResolveType(string fullName, bool throwOnError)
    {
        ArgumentNullException.ThrowIfNull(fullName);
        return Type.GetType(fullName, throwOnError);
    }
}
