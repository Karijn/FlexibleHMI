# Property Manager

The Property Manager maintains the named values shared by a Flexible HMI document. Calculators, function generators, communication channels, and controls can read or update these properties. The **Property Manager** is on the properties/calculator/function-generator tab.

## Create, edit, and delete properties

1. In **Properties**, select **New property**.
2. Enter a name, choose a type, and enter a value. For a Boolean, use the **True** checkbox.
3. Select **Save property**. The property appears in the list with its type and current value.
4. To change a property, select it in the list, edit its fields, and select **Save property**. To remove it, select **Delete selected**.

Names are case-sensitive, must not be blank, and must be unique. Leading and trailing whitespace is removed when saving. Renaming a property removes its old name; update any controls, calculations, and other bindings that referenced it. Changing or deleting a property can also invalidate existing bindings. Errors are displayed in the editor.

Values can change outside the editor: for example, an input control, calculator, communication channel, or function generator can update a property. The list shows incoming updates. Saving a document stores supported property values and struct definitions; opening a document replaces the current document's properties and definitions.

## Property types

| Type | Editor input | Example |
| --- | --- | --- |
| Integer | 32-bit signed integer | `42` |
| Double | Finite floating-point number | `3.14` |
| Boolean | **True** checkbox | checked or unchecked |
| String | Text | `Pump 1` |
| DateTime | Date and time | `2026-11-23T17:49:32` |
| TimeSpan | Duration | `01:30:00` |
| Color | Non-system color name or hex | `Red`, `#FF0000`, `#80FF0000` |
| Struct | JSON object matching a named struct type | `{"X": 1.5, "Y": 2.0}` |
| Array | JSON array with a selected element type | `[1, 2, 3]` |

Scalar numeric, DateTime, and TimeSpan values use the current culture when entered in the editor. JSON uses standard JSON number syntax; DateTime and TimeSpan values in JSON are strings parsed using the invariant culture. Colors accept `#RGB`, `#ARGB`, `#RRGGBB`, and `#AARRGGBB`, or a non-system color name. They are displayed and saved in `#AARRGGBB` form; the first component is alpha. Within JSON, use a string such as `"#80FF0000"`.

## Struct types

A struct type defines named, typed fields that can be reused across properties.

1. Open **Struct types** and select **New struct type**.
2. Enter a type name, add fields, and specify a type for each field.
3. Select **Save struct type**. In **Properties**, choose **Struct**, select the new type, and enter a JSON object with its fields.

For example, define `Point` with `X` and `Y` fields of type `Double`, then create a `Point` property with:

```json
{"X": 1.5, "Y": 2.0}
```

A struct value must contain every defined field, with matching case and compatible values; unknown fields are rejected. Fields can be primitive types, other defined struct types, or typed arrays such as `Integer[]` or `Point[]`. Cycles of non-array struct fields are rejected; an array field can reference a struct type, including its containing type.

Type and field names must not be blank; field names must be unique within the type. Primitive type names (`Integer`, `Double`, `Boolean`, `String`, `DateTime`, `TimeSpan`, and `Color`) are reserved for struct definitions. Type names cannot contain `[` or `]`. Editing a definition migrates existing struct and array values: compatible fields are retained and added or incompatible fields receive default values. Renaming a type also updates references. A type cannot be deleted while another type or property uses it.

## Arrays

Choose **Array**, select or enter its element type, and enter a JSON array. For example, an `Integer` array is `[1, 2, 3]`; an array with element type `Point` can be `[{"X": 1.5, "Y": 2.0}]`. Use an element type such as `Integer[]` for nested arrays, for example `[[1, 2], [3, 4]]`. All elements must match the selected type. Empty arrays are written as `[]`.

## Using properties in calculations

In the Calculator, use a name such as `Temperature`, a struct field such as `Position.X`, or an array element such as `Samples[0]`. Names or paths containing spaces can be enclosed in braces, for example `{Outside Temperature}`. For the expression syntax, output types, and function reference, see the [main README](../README.md#calculator).

## Integration for add-ins

`PropertyManager.Interfaces` exposes `IVariableHandler` for typed `Set`, `TryGet`, `Get`, `Remove`, and change notifications. Its `VariableChanged` event reports the name and old and new values when a variable changes; consumers that update WPF controls from background notifications must marshal to the UI dispatcher. Names are case-sensitive. `IStructTypeRegistry` provides struct definitions and typed JSON parsing and serialization.

Document-backed arrays are represented by `ArrayValue` and struct values by `StructValue`; use the registry's typed parsing and serialization for these values. The generic `SetArray<T>` and `GetArray<T>` helpers work with native CLR arrays, which are separate from document-backed `ArrayValue` properties and are not among the property types supported by document persistence.
