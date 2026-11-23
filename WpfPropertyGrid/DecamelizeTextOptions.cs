namespace WpfPropertyGrid;

/// <summary>
/// Controls casing, escape decoding, and separator handling during decamelization.
/// </summary>
/// <summary>
/// Controls casing, escape decoding, and separator replacement during decamelization.
/// </summary>
[Flags]
public enum DecamelizeTextOptions
{
    /// <summary>
    /// Applies no additional text-formatting transformations.
    /// </summary>
    None = 0,
    /// <summary>
    /// Converts the first character to uppercase.
    /// </summary>
    ForceFirstUpper = 1,
    /// <summary>
    /// Converts characters after the first to lowercase.
    /// </summary>
    ForceRestLower = 2,
    /// <summary>
    /// Converts Unicode escape sequences to their characters.
    /// </summary>
    UnescapeUnicode = 4,
    /// <summary>
    /// Converts hexadecimal escape sequences to their characters.
    /// </summary>
    UnescapeHexadecimal = 8,
    /// <summary>
    /// Replaces spaces with underscores.
    /// </summary>
    ReplaceSpacesByUnderscore = 0x10,
    /// <summary>
    /// Replaces spaces with hyphens.
    /// </summary>
    ReplaceSpacesByMinus = 0x20,
    /// <summary>
    /// Replaces spaces with periods.
    /// </summary>
    ReplaceSpacesByDot = 0x40,
    /// <summary>
    /// Preserves leading underscores.
    /// </summary>
    KeepFirstUnderscores = 0x80,
    /// <summary>
    /// Leaves numeric portions of the text undecamelized.
    /// </summary>
    DontDecamelizeNumbers = 0x100,
    /// <summary>
    /// Preserves formatting indices.
    /// </summary>
    KeepFormattingIndices = 0x200,
    /// <summary>
    /// Uses the default casing, escape-decoding, and leading-underscore options.
    /// </summary>
    Default = ForceFirstUpper | UnescapeUnicode | UnescapeHexadecimal | KeepFirstUnderscores,
}
