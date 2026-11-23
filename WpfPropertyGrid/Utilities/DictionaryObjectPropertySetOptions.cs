namespace WpfPropertyGrid.Utilities;

/// <summary>
/// Controls notifications, equality checks, and rollback when setting a dictionary-backed value.
/// </summary>
[Flags]
public enum DictionaryObjectPropertySetOptions
{
    /// <summary>
    /// Uses the default property-setting behavior.
    /// </summary>
    None = 0x0,
    /// <summary>
    /// Suppresses the property-changing notification.
    /// </summary>
    DontRaiseOnPropertyChanging = 0x1,
    /// <summary>
    /// Suppresses the property-changed notification.
    /// </summary>
    DontRaiseOnPropertyChanged = 0x2,
    /// <summary>
    /// Replaces the stored value without checking equality.
    /// </summary>
    DontTestValuesForEquality = 0x4,
    /// <summary>
    /// Suppresses the validation-errors-changed notification.
    /// </summary>
    DontRaiseOnErrorsChanged = 0x8,
    /// <summary>
    /// Raises the property-changed notification even when the value is equal.
    /// </summary>
    ForceRaiseOnPropertyChanged = 0x10,
    /// <summary>
    /// Restores the previous value if validation reports an error.
    /// </summary>
    RollbackChangeOnError = 0x20,
}
