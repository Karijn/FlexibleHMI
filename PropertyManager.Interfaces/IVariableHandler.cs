using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyManager.Interfaces;

/// <summary>
/// Provides thread-safe storage and change notifications for named variables
/// of different types.
/// </summary>
public interface IVariableHandler
{
    /// <summary>
    /// Occurs when a variable is added, updated, or removed.
    /// </summary>
    event EventHandler<VariableChanged>? VariableChanged;

    /// <summary>
    /// Gets the names of all stored variables.
    /// </summary>
    /// <returns>a collection of variable names.</returns>
    IEnumerable<string> GetVariableNames();

    /// <summary>
    /// Gets the names of all stored variables compatible with a specific type.
    /// </summary>
    /// <typeparam name="T">The requested variable type.</typeparam>
    /// <returns>A snapshot of matching variable names.</returns>
    IEnumerable<string> GetVariableNames<T>();

    /// <summary>Gets the names and values of variables compatible with a specific type.</summary>
    /// <typeparam name="T">The requested variable type.</typeparam>
    /// <returns>A snapshot keyed by variable name.</returns>
    Dictionary<string, T> GetVariables<T>();

    /// <summary>
    /// Stores or updates a variable.
    /// </summary>
    /// <typeparam name="T">The variable value type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(string name, T value);

    /// <summary>
    /// Attempts to retrieve a variable of the requested type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The retrieved value, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the variable exists and has the requested type;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryGet<T>(string name, out T? value);

    /// <summary>
    /// Retrieves a variable of the requested type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The stored variable value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the variable does not exist or has a different type.
    /// </exception>
    T Get<T>(string name);

    /// <summary>
    /// Removes a variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>
    /// <see langword="true"/> when the variable was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool Remove(string name);

    /// <summary>
    /// Stores an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="values">The values to store.</param>
    void SetArray<T>(
        string name,
        IEnumerable<T> values);

    /// <summary>
    /// Retrieves an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>A copy of the stored array.</returns>
    T[] GetArray<T>(string name);

    /// <summary>
    /// Attempts to retrieve an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="values">The retrieved array, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the array exists and has the requested type;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryGetArray<T>(
        string name,
        out T[]? values);

    /// <summary>
    /// Appends an element to an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="value">The element to append.</param>
    void AddToArray<T>(
        string name,
        T value);

    /// <summary>
    /// Removes one or all matching elements from an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="value">The value to remove.</param>
    /// <param name="removeAll">
    /// Removes every matching element when <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when at least one element was removed.
    /// </returns>
    bool RemoveFromArray<T>(
        string name,
        T value,
        bool removeAll = false);

    /// <summary>
    /// Inserts an element at the specified array index.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="value">The element to insert.</param>
    void InsertArrayElement<T>(
        string name,
        int index,
        T value);

    /// <summary>
    /// Updates an existing array element.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The index of the element to update.</param>
    /// <param name="value">The replacement value.</param>
    void UpdateArrayElement<T>(
        string name,
        int index,
        T value);

    /// <summary>
    /// Removes an array element at the specified index.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    /// <param name="index">The index of the element to remove.</param>
    /// <param name="removedValue">The removed element, if found.</param>
    /// <returns>
    /// <see langword="true"/> when an element was removed.
    /// </returns>
    bool RemoveArrayElement<T>(
        string name,
        int index,
        out T? removedValue);

    /// <summary>
    /// Removes all elements from an array variable.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="name">The array variable name.</param>
    void ClearArray<T>(string name);

    /// <summary>
    /// Retrieves multiple variables with the same type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="names">The variable names to retrieve.</param>
    /// <returns>A dictionary keyed by variable name.</returns>
    IReadOnlyDictionary<string, T> GetMany<T>(
        IEnumerable<string> names);

    /// <summary>
    /// Attempts to retrieve multiple variables with the same type.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="names">The variable names to retrieve.</param>
    /// <param name="values">The retrieved variables, if successful.</param>
    /// <returns>
    /// <see langword="true"/> when all variables exist and have the requested type;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryGetMany<T>(
        IEnumerable<string> names,
        out IReadOnlyDictionary<string, T> values);

    /// <summary>
    /// Stores a value type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The structure value.</param>
    void SetStruct<T>(
        string name,
        T value)
        where T : struct;

    /// <summary>
    /// Retrieves a value type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The stored structure.</returns>
    T GetStruct<T>(string name)
        where T : struct;

    /// <summary>
    /// Attempts to retrieve a value type variable.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The retrieved structure, if found.</param>
    /// <returns>
    /// <see langword="true"/> when the structure exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryGetStruct<T>(
        string name,
        out T value)
        where T : struct;
}
