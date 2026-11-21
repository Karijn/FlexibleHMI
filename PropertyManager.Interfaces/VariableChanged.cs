namespace PropertyManager.Interfaces;


/// <summary>
/// Describes a change to a named variable.
/// </summary>
/// <param name="Name">The variable name.</param>
/// <param name="OldValue">The previous value, or <see langword="null"/>.</param>
/// <param name="NewValue">The new value, or <see langword="null"/> when removed.</param>
public sealed record VariableChanged(string Name, object? OldValue, object? NewValue);

