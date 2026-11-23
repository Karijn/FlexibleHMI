# Flexible HMI

Flexible HMI is a plugin-based application for building documents with live properties, calculations, signal generators, and visual controls. The Property Manager, Calculator, and Function Generator are available on the properties/calculator/function-generator tab.

## Properties

Properties are named values shared by the calculator, function generator, communication manager, and controls. In **Property Manager**, select **New property**, enter a name, choose a type and value, and select **Save property**. Select an existing property to edit or delete it. Property names are case-sensitive and must be unique. Values and struct definitions are saved with the document. For detailed instructions and add-in integration, see the [Property Manager README](PropertyManager/README.md).

| Type | Value and example |
| --- | --- |
| Integer | 32-bit integer, for example `42`. |
| Double | Finite floating-point number, for example `3.14`. |
| Boolean | Select or clear the **True** checkbox. |
| String | Text entered in the value field. |
| DateTime | Date and time, for example `2026-11-23T17:49:32`. |
| TimeSpan | Duration, for example `01:30:00`. |
| Color | A color name such as `Red`, or hex such as `#FF0000` or `#80FF0000` (alpha, red, green, blue). Colors are displayed as `#AARRGGBB`. |
| Struct | A value of a named struct type, entered as a JSON object. |
| Array | A typed array entered as a JSON array, for example `[1, 2, 3]`. |

The property editor parses scalar numeric and date/time input using the current culture. Color values inside JSON objects or arrays are JSON strings, for example `["Red", "#80FF0000"]`.

### Structs and arrays

Use the **Struct types** tab to create a named type and add fields. Fields can use primitive types, other struct types, or typed arrays. In the **Properties** tab, choose **Struct** and the type, then enter a matching JSON object. For example, a type `Point` with Double fields `X` and `Y` can contain `{"X": 1.5, "Y": 2.0}`. For an array of points, choose **Array**, enter `Point` as the element type, and supply `[{"X": 1.5, "Y": 2.0}]`. Array element types can themselves be arrays, such as `Integer[]`.

## Calculator

Select **Add calculation**, enter an expression and an output property name, and choose its output type. Enabled calculations recalculate when properties change; **Calculate** also recalculates explicitly. Disable a calculation to stop its updates. The result and any error appear on the calculation card. Calculations may create or replace scalar output properties. Avoid using the output as an input to its own calculation, or creating cycles between calculations.

For **Struct** and **Array** outputs, first create a destination property with the desired struct or element type. The expression must produce a matching JSON object or array; the calculator validates it against that destination type. Integer results are rounded to the nearest integer, with midpoint values rounded to even. Numeric outputs must be finite.

### Expression syntax

- Refer to properties directly: `Temperature`, `MyStruct.Settings.Limit`, `Samples[0]`, or `Devices[1].Temperature`. Use braces for names or paths containing spaces, for example `{Outside Temperature}`. Property and field names are case-sensitive.
- Numeric literals use a decimal point and may use scientific notation. String literals use single or double quotes; a backslash escapes the next character. Boolean literals include `true` and `false`; numeric constants include `pi` and `e`.
- Arithmetic operators: `+`, `-`, `*`, `/`, `%`, and `^`; comparisons: `==`, `!=`, `<`, `<=`, `>`, `>=`; Boolean operators: `&&`, `||`, `!`. Parentheses group expressions. A String on the left of `+` concatenates the string representation of the right operand.
- DateTime plus or minus TimeSpan yields a DateTime; subtracting two DateTimes yields a TimeSpan. TimeSpans can be added or subtracted.
- Function names are case-insensitive. Arguments are separated by commas. String searches and comparisons are ordinal and case-sensitive unless a case-conversion function is used.

### Numeric functions

Trigonometric functions use radians. `round` uses midpoint-to-even rounding; `log` and `log10` are base 10, and `ln` is the natural logarithm.

| Function | Description |
| --- | --- |
| `abs(x)`, `sqrt(x)`, `exp(x)`, `ln(x)`, `log(x)`, `log10(x)` | Absolute value, square root, exponential, and logarithms. |
| `sin(x)`, `cos(x)`, `tan(x)`, `asin(x)`, `acos(x)`, `atan(x)` | Trigonometric and inverse trigonometric functions. |
| `floor(x)`, `ceil(x)`, `round(x)` | Round a number. |
| `min(a,b)`, `max(a,b)`, `pow(a,b)`, `clamp(x,min,max)` | Minimum, maximum, power, and range limiting. |

Example: `clamp(Temperature * 1.8 + 32, -40, 212)`.

### String functions

| Function | Description |
| --- | --- |
| `string(value)` | Convert a value to text. |
| `len(text)`, `length(text)` | String length. |
| `lower(text)`, `lowercase(text)`, `upper(text)`, `toupper(text)` | Invariant case conversion. |
| `trim(text)`, `trimstart(text)`, `trimend(text)` | Remove whitespace. |
| `substring(text,start)` or `substring(text,start,length)` | Extract text using a zero-based start index. |
| `replace(text,old,new)`, `concat(value,...)` | Replace text or concatenate one or more values. |
| `contains(text,search)`, `startswith(text,search)`, `endswith(text,search)` | Return a Boolean match. |
| `indexof(text,search)` | Zero-based index, or `-1` if not found. |

Example: `concat('Temperature: ', string(Temperature))` with String output.

### DateTime and TimeSpan functions

| Function | Description |
| --- | --- |
| `now()`, `utcnow()`, `today()` | Current local date/time, UTC date/time, or local date at midnight. These are evaluated when the calculation runs; they do not themselves schedule updates. |
| `datetime(text)`, `datetime(year,month,day)`, `datetime(year,month,day,hour,minute,second)` | Parse or construct a DateTime. |
| `date(value)` | Date portion of a DateTime. |
| `year(value)`, `month(value)`, `day(value)`, `hour(value)`, `minute(value)`, `second(value)`, `millisecond(value)`, `dayofweek(value)` | DateTime components; day of week is Sunday = 0 through Saturday = 6. |
| `adddays(date,n)`, `addhours(date,n)`, `addminutes(date,n)`, `addseconds(date,n)`, `addmilliseconds(date,n)` | Add an amount to a DateTime. |
| `formatdatetime(date)` or `formatdatetime(date,format)` | Format using an invariant .NET date/time format; default is round-trip `O`. |
| `timespan(text)`, `timespan(hours,minutes,seconds)`, `timespan(days,hours,minutes,seconds[,milliseconds])` | Parse or construct a TimeSpan. |
| `days(span)`, `hours(span)`, `minutes(span)`, `seconds(span)`, `milliseconds(span)` | Individual TimeSpan components. |
| `totaldays(span)`, `totalhours(span)`, `totalminutes(span)`, `totalseconds(span)`, `totalmilliseconds(span)` | Total duration in the requested unit. |
| `addtimespan(a,b)`, `subtracttimespan(a,b)` | Add or subtract durations. |

Examples: `adddays(datetime('2026-11-23T12:00:00'), 1)` and `totalminutes(timespan('01:30:00'))`.

### JSON functions

| Function | Description |
| --- | --- |
| `json(value)` | Convert a value to a JSON value. |
| `jsonobject(key,value,...)`, `jsoncreate(key,value,...)` | Create an object from one or more key/value pairs; keys must be nonempty and unique. |
| `jsonarray(value,...)`, `arrayjson(value,...)` | Create an array; zero arguments creates an empty array. |
| `jsonparse(text)`, `parsejson(text)` | Parse JSON text. |
| `jsonget(json[,path])`, `jsonextract(json[,path])`, `jsonvalue(json[,path])` | Return the root or the value at a path. Missing paths report an error. |
| `jsonstring(json)`, `jsonserialize(json)` | Serialize JSON as a String. |
| `jsonhas(json,path)` | Test whether a path exists. |
| `jsonlength(json)` | Number of array elements, object fields, or characters in a JSON string. |
| `jsonvalid(text)` | Test whether text is valid JSON. |

JSON inputs can be parsed JSON values or JSON text. Paths use names and zero-based indexes, such as `items[0].name`; an empty path identifies the root. `jsonget` returns scalar values as calculator values and objects or arrays as JSON values. A JSON null extracts as an empty String.

Examples:

```text
jsonstring(jsonobject('name', 'Pump', 'values', jsonarray(1, 2, 3)))
jsonget('{"items":[{"value":42}]}', 'items[0].value')
```

The first example uses String output; the second uses Double or Integer output. To extract an object into a struct property, create the destination struct property first, select **Struct** as the output type, and use an expression such as `jsonget(JsonText, 'point')`.

### Color functions

| Function | Description |
| --- | --- |
| `color(text)`, `colorfromstring(text)` | Parse a color name or hex string. `color` also accepts an existing Color. |
| `rgb(r,g,b)`, `argb(a,r,g,b)` | Construct a Color from integer components in the range 0-255. |
| `colortostring(color)`, `colortojson(color)` | Produce `#AARRGGBB` text or a JSON-encoded color string. |
| `colortoarray(color)`, `colortostruct(color)` | Produce JSON values `[A,R,G,B]` or `{"A":...,"R":...,"G":...,"B":...}`. |
| `colorfromarray(value)`, `colorfromstruct(value)`, `colorfromjson(value)` | Convert JSON or JSON text to a Color. Arrays accept `[R,G,B]` or `[A,R,G,B]`; objects require R, G, B and accept optional A. Alpha defaults to 255. `colorfromjson` also accepts color strings. |

Example: `colortostring(argb(128, 255, 0, 0))` produces `#80FF0000`. To store `colortoarray` as an Array output, create an Integer array destination first. For `colortostruct`, create a struct destination with Integer fields `A`, `R`, `G`, and `B`.

## Function generator

The Function Generator writes sampled Double values to named output properties. Use **Add waveform output** to configure multiple outputs, each with its own start and stop buttons. **Start all outputs** and **Stop all outputs** operate on every output. Available waveforms are Sawtooth, Sinus, Cosinus, Block, Triangle, WhiteNoise, GaussianNoise, PinkNoise, and BrownNoise.

For each output, set the output property name, waveform, frequency, amplitude, offset, and update interval. Frequency must be greater than 0 and at most 1000 Hz; amplitude and offset must be finite; the update interval is 1-10000 ms. The generated value is `offset + amplitude * waveform`. Timing is software-based, not a real-time guarantee.

The optional selectors beneath frequency, amplitude, offset, and interval bind those settings to existing Integer or Double properties. A blank selector uses the manual value; bound values are read while running. An invalid bound value stops the output and displays an error. Stop and restart to apply changes to the waveform, output property, or binding names; values of already-bound properties update live.

The optional global **Start/stop Boolean property** starts all outputs when true and stops them when false. Each output also has an optional **Running Boolean property** for independent control. If both are configured, both must be true for an automatic start. A missing or non-Boolean control property stops the affected outputs and reports an error. Manual start/stop buttons still act immediately; the next control-property change reapplies its value. An output cannot write to a configured start/stop property. Bindings and settings are saved with the document.

Example: create Boolean properties `RunSignals` and `RunMotor`, bind them as the global and per-output running properties, respectively, and use an on-off input bound to either property to control generation. Both must be true for that output to start automatically.
