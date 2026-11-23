namespace PropertyManager;

/// <summary>Contains a received variable message and its interpreted value.</summary>
public sealed class VariableMessageReceivedEventArgs : EventArgs
{
    /// <summary>Creates event data for a received variable message.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The interpreted value.</param>
    /// <param name="rawMessage">The original message text.</param>
    public VariableMessageReceivedEventArgs(string name, object? value, string rawMessage)
    {
        Name = name;
        Value = value;
        RawMessage = rawMessage;
    }

    /// <summary>Gets the variable name.</summary>
    public string Name { get; }

    /// <summary>Gets the interpreted value.</summary>
    public object? Value { get; }

    /// <summary>Gets the original message text.</summary>
    public string RawMessage { get; }
}

