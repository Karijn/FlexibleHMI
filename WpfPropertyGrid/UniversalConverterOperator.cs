namespace WpfPropertyGrid;
/// <summary>
/// Specifies the comparison or string-matching operation used by a universal converter.
/// </summary>
public enum UniversalConverterOperator
{
    /// <summary>
    /// Matches every input value.
    /// </summary>
    AlwaysMatches,
    /// <summary>
    /// Matches when the input equals the comparison value.
    /// </summary>
    Equal,
    /// <summary>
    /// Matches when the input differs from the comparison value.
    /// </summary>
    NotEqual,
    /// <summary>
    /// Matches when the input is greater than the comparison value.
    /// </summary>
    GreaterThan,
    /// <summary>
    /// Matches when the input is greater than or equal to the comparison value.
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// Matches when the input is less than the comparison value.
    /// </summary>
    LesserThan,
    /// <summary>
    /// Matches when the input is less than or equal to the comparison value.
    /// </summary>
    LesserThanOrEqual,
    /// <summary>
    /// Matches when the input is at least the minimum and less than the maximum.
    /// </summary>
    Between,
    /// <summary>
    /// Matches when the input string starts with the comparison string.
    /// </summary>
    StartsWith,
    /// <summary>
    /// Matches when the input string ends with the comparison string.
    /// </summary>
    EndsWith,
    /// <summary>
    /// Matches when the input string contains the comparison string.
    /// </summary>
    Contains,
    /// <summary>
    /// Matches when the input runtime type exactly equals the comparison type.
    /// </summary>
    IsType,
    /// <summary>
    /// Matches when the comparison type is assignable from the input runtime type.
    /// </summary>
    IsOfType
}
