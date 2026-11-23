namespace WpfPropertyGrid;

/// <summary>
/// Specifies preprocessing and matching options for a universal converter.
/// </summary>
[Flags]
public enum UniversalConverterOptions
{
    /// <summary>
    /// Applies no preprocessing or special matching options.
    /// </summary>
    None = 0x0,
    /// <summary>
    /// Trims whitespace from strings before comparison.
    /// </summary>
    Trim = 0x1,
    /// <summary>
    /// Converts comparison values to the input type when needed.
    /// </summary>
    Convert = 0x2,
    /// <summary>
    /// Treats an empty string as null.
    /// </summary>
    Nullify = 0x4,
    /// <summary>
    /// Allows null to match a non-value comparison type.
    /// </summary>
    NullMatchesType = 0x8,
    /// <summary>
    /// Uses the converter parameter as the converted result.
    /// </summary>
    ConvertedValueIsConverterParameter = 0x20,
    /// <summary>
    /// Uses the input value as the converted result.
    /// </summary>
    ConvertedValueIsConverterValue = 0x40,
}
