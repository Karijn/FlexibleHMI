namespace WpfPropertyGrid
{
    /// <summary>
    /// Represents an item in a property-grid editor, such as a selectable enum value.
    /// </summary>
    public class PropertyGridItem : DictionaryObject
    {
        /// <summary>
        /// Initializes a new instance of PropertyGridItem.
        /// </summary>
        public PropertyGridItem()
        {
            IsChecked = false;
        }
        /// <summary>
        /// Gets or sets the dictionary object raise on property changed.
        /// </summary>
        public new bool DictionaryObjectRaiseOnPropertyChanged { get => base.DictionaryObjectRaiseOnPropertyChanged; set => base.DictionaryObjectRaiseOnPropertyChanged = value; }
        /// <summary>
        /// Gets or sets the dictionary object raise on property changing.
        /// </summary>
        public new bool DictionaryObjectRaiseOnPropertyChanging { get => base.DictionaryObjectRaiseOnPropertyChanging; set => base.DictionaryObjectRaiseOnPropertyChanging = value; }
        /// <summary>
        /// Gets or sets the dictionary object raise on errors changed.
        /// </summary>
        public new bool DictionaryObjectRaiseOnErrorsChanged { get => base.DictionaryObjectRaiseOnErrorsChanged; set => base.DictionaryObjectRaiseOnErrorsChanged = value; }
        /// <summary>
        /// Gets or sets whether this item represents an unset value.
        /// </summary>
        public virtual bool IsUnset { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <summary>
        /// Gets or sets whether this item represents the zero value.
        /// </summary>
        public virtual bool IsZero { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <summary>
        /// Gets or sets the item display name.
        /// </summary>
        public virtual string? Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <summary>
        /// Gets or sets the item value.
        /// </summary>
        public virtual object? Value { get => DictionaryObjectGetPropertyValue<object>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <summary>
        /// Gets or sets whether this item is selected.
        /// </summary>
        public virtual bool? IsChecked { get => DictionaryObjectGetPropertyValue<bool?>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <summary>
        /// Gets or sets the property represented by this item.
        /// </summary>
        public virtual PropertyGridProperty? Property { get => DictionaryObjectGetPropertyValue<PropertyGridProperty>(); set => DictionaryObjectSetPropertyValue(value); }
        /// <inheritdoc/>
        public override string ToString() => Name ?? string.Empty;
    }
}