using PropertyManager.Interfaces;

namespace PropertyManager;

/// <summary>
/// Stores variables of different types and publishes changes to subscribers.
/// </summary>
/// <remarks>
/// Values are protected by a synchronization lock. Array values are copied
/// when read or written so changes are published through <see cref="VariableChanged"/>
/// instead of being made silently by external callers.
/// </remarks>
public sealed class VariableHandler : IVariableHandler
{
    private readonly Dictionary<string, object?> _variables = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// Occurs when a variable is added, updated, or removed.
    /// </summary>
    public event EventHandler<VariableChanged>? VariableChanged;

    public VariableHandler()
    {
        //Set("Test1", 10.1);
        //Set("Test2", 20.2);
        //Set("Test3", 30.3);
    }

    /// <summary>
    /// Gets the names of all stored variables.
    /// </summary>
    /// <returns>a collection of variable names.</returns>
    public IEnumerable<string> GetVariableNames()
    {
        lock (_syncRoot)
        {
            return _variables.Keys.ToArray();
        }
    }

    public IEnumerable<string> GetVariableNames<T>()
    {
        lock (_syncRoot)
        {
            List<string> names = [];
            foreach (var name in _variables.Keys.ToArray())
            {
                if(TryGet<T>(name, out T? val))
                {
                    names.Add(name);
                }
            }
            return names;
        }
    }

    public Dictionary<string, T> GetVariables<T>()
    {
        lock (_syncRoot)
        {
            Dictionary<string, T> namevalues = [];
            foreach (var name in _variables.Keys.ToArray())
            {
                if (TryGet<T>(name, out T? val))
                {
                    namevalues.Add(name, val);
                }
            }
            return namevalues;
        }
    }


    /// <summary>
    /// Adds or updates a variable.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        object? oldValue;
        bool shouldPublish;

        lock (_syncRoot)
        {
            var exists = _variables.TryGetValue(name, out oldValue);

            shouldPublish = !exists || !Equals(oldValue, value);

            if (shouldPublish)
            {
                _variables[name] = value;
            }
        }

        if (shouldPublish)
        {
            PublishChanged(name, oldValue, value);
        }
    }

    /// <summary>
    /// Attempts to retrieve a variable with the requested type.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The retrieved value, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the variable exists and has the requested type.
    /// </returns>
    public bool TryGet<T>(string name, out T? value)
    {
        lock (_syncRoot)
        {
            if (_variables.TryGetValue(name, out var storedValue))
            {
                if (storedValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                if (storedValue is null && default(T) is null)
                {
                    value = default;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Retrieves a variable with the requested type.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the variable does not exist or has a different type.
    /// </exception>
    public T Get<T>(string name)
    {
        if (!TryGet<T>(name, out var value))
        {
            throw new KeyNotFoundException($"Variable '{name}' was not found or has an incompatible type.");
        }

        return value!;
    }

    /// <summary>
    /// Removes a variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>
    /// <see langword="true"/> when a variable was removed.
    /// </returns>
    public bool Remove(string name)
    {
        object? oldValue;

        lock (_syncRoot)
        {
            if (!_variables.Remove(name, out oldValue))
            {
                return false;
            }
        }

        PublishChanged(name, oldValue, null);
        return true;
    }

    /// <summary>
    /// Stores an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="values">The values to store.</param>
    public void SetArray<T>(string name, IEnumerable<T> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        // Store a copy to prevent untracked external mutations.
        Set(name, values.ToArray());
    }

    /// <summary>
    /// Retrieves an array variable as a copy.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>A copy of the stored array.</returns>
    public T[] GetArray<T>(string name)
    {
        if (!TryGetArray<T>(name, out var values))
        {
            throw new KeyNotFoundException($"Array variable '{name}' was not found or has an incompatible type.");
        }

        return values!;
    }

    /// <summary>
    /// Attempts to retrieve an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="values">The copied array, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the array exists and has the requested type.
    /// </returns>
    public bool TryGetArray<T>(string name, out T[]? values)
    {
        if (TryGet<T[]>(name, out var storedArray))
        {
            values = storedArray?.ToArray();
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>
    /// Appends an element to an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="value">The value to append.</param>
    public void AddToArray<T>(string name, T value)
    {
        var values = TryGetArray<T>(name, out var existingValues)
            ? existingValues!
            : Array.Empty<T>();

        var updatedValues = new T[values.Length + 1];

        values.CopyTo(updatedValues, 0);
        updatedValues[^1] = value;

        Set(name, updatedValues);
    }

    /// <summary>
    /// Removes one or all matching elements from an array.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="value">The value to remove.</param>
    /// <param name="removeAll">
    /// Removes all matching values when <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when at least one value was removed.
    /// </returns>
    public bool RemoveFromArray<T>(string name, T value, bool removeAll = false)
    {
        if (!TryGetArray<T>(name, out var existingValues))
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        var updatedValues = new List<T>(existingValues!.Length);
        var removed = false;

        foreach (var item in existingValues)
        {
            if (comparer.Equals(item, value) &&
                (removeAll || !removed))
            {
                removed = true;
                continue;
            }

            updatedValues.Add(item);
        }

        if (removed)
        {
            Set(name, updatedValues.ToArray());
        }

        return removed;
    }

    /// <summary>
    /// Inserts an element at an array index.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="value">The value to insert.</param>
    public void InsertArrayElement<T>(string name, int index, T value)
    {
        var values = GetArray<T>(name);

        if (index < 0 || index > values.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The index must be between 0 and {values.Length}.");
        }

        var updatedValues = new T[values.Length + 1];

        Array.Copy(values, 0, updatedValues, 0, index);
        updatedValues[index] = value;

        Array.Copy(values, index, updatedValues, index + 1, values.Length - index);

        Set(name, updatedValues);
    }

    /// <summary>
    /// Replaces an element at an array index.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The element index.</param>
    /// <param name="value">The replacement value.</param>
    public void UpdateArrayElement<T>(string name, int index, T value)
    {
        var values = GetArray<T>(name);

        ValidateElementIndex(index, values.Length);

        if (EqualityComparer<T>.Default.Equals(values[index], value))
        {
            return;
        }

        values[index] = value;
        Set(name, values);
    }

    /// <summary>
    /// Removes an element at an array index.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The element index.</param>
    /// <param name="removedValue">The removed value, if successful.</param>
    /// <returns>
    /// <see langword="true"/> when an element was removed.
    /// </returns>
    public bool RemoveArrayElement<T>(string name, int index, out T? removedValue)
    {
        if (!TryGetArray<T>(name, out var values))
        {
            removedValue = default;
            return false;
        }

        ValidateElementIndex(index, values!.Length);

        removedValue = values[index];

        var updatedValues = new T[values.Length - 1];

        Array.Copy(values, 0, updatedValues, 0, index);

        Array.Copy(values, index + 1, updatedValues, index, values.Length - index - 1);

        Set(name, updatedValues);

        return true;
    }

    /// <summary>
    /// Removes all elements from an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    public void ClearArray<T>(string name)
    {
        if (!TryGetArray<T>(name, out _))
        {
            throw new KeyNotFoundException($"Array variable '{name}' was not found or has an incompatible type.");
        }

        Set(name, Array.Empty<T>());
    }

    /// <summary>
    /// Retrieves multiple variables with the same type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="names">The variable names to retrieve.</param>
    /// <returns>A dictionary keyed by variable name.</returns>
    public IReadOnlyDictionary<string, T> GetMany<T>(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        var result = new Dictionary<string, T>();

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!TryGet<T>(name, out var value))
            {
                throw new KeyNotFoundException($"Variable '{name}' was not found or has an incompatible type.");
            }

            result[name] = value!;
        }

        return result;
    }

    /// <summary>
    /// Attempts to retrieve multiple variables with the same type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="names">The variable names to retrieve.</param>
    /// <param name="values">The retrieved variables, if successful.</param>
    /// <returns>
    /// <see langword="true"/> when all requested variables are valid.
    /// </returns>
    public bool TryGetMany<T>(IEnumerable<string> names, out IReadOnlyDictionary<string, T> values)
    {
        ArgumentNullException.ThrowIfNull(names);

        var result = new Dictionary<string, T>();

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!TryGet<T>(name, out var value))
            {
                values = new Dictionary<string, T>();
                return false;
            }

            result[name] = value!;
        }

        values = result;
        return true;
    }

    /// <summary>
    /// Stores a value-type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The structure value.</param>
    public void SetStruct<T>(string name, T value)
        where T : struct
    {
        Set(name, value);
    }

    /// <summary>
    /// Retrieves a value-type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The stored structure.</returns>
    public T GetStruct<T>(string name)
        where T : struct
    {
        return Get<T>(name);
    }

    /// <summary>
    /// Attempts to retrieve a value-type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The retrieved structure, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the structure exists and has the requested type.
    /// </returns>
    public bool TryGetStruct<T>(string name, out T value)
        where T : struct
    {
        return TryGet(name, out value);
    }

    /// <summary>
    /// Publishes a variable change to subscribers.
    /// </summary>
    private void PublishChanged(string name, object? oldValue, object? newValue)
    {
        VariableChanged?.Invoke(this, new VariableChanged(name, oldValue, newValue));
    }

    /// <summary>
    /// Validates an existing array element index.
    /// </summary>
    private static void ValidateElementIndex(int index, int length)
    {
        if (index < 0 || index >= length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The index must be between 0 and {length - 1}.");
        }
    }
}
