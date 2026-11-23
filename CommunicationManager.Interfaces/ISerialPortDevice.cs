namespace CommunicationManager.Interfaces;

/// <summary>Provides the text of a message received from or sent to a serial device.</summary>
/// <param name="message">The message text.</param>
public sealed class CommunicationMessageEventArgs(string message) : EventArgs
{
    /// <summary>Gets the message text.</summary>
    public string Message { get; } = message;
}

/// <summary>Provides the text of an error reported by a serial device or manager.</summary>
/// <param name="message">The error description.</param>
public sealed class CommunicationErrorEventArgs(string message) : EventArgs
{
    /// <summary>Gets the error description.</summary>
    public string Message { get; } = message;
}

/// <summary>Maps a serial protocol name to a document property.</summary>
/// <remarks>External names are unique within a connection and cannot contain <c>!</c>, <c>:</c>, line breaks, or surrounding whitespace. When updates are enabled, the current value is sent on connection and subsequent property changes are transmitted.</remarks>
/// <param name="ExternalName">The case-sensitive name used in serial messages.</param>
/// <param name="PropertyName">The name of the property to read or update.</param>
/// <param name="SendUpdates">Whether property changes are sent to the serial device.</param>
public sealed record CommunicationPropertyRoute(
    string ExternalName,
    string PropertyName,
    bool SendUpdates);

/// <summary>Provides serial-port discovery and line-based communication.</summary>
/// <remarks>Implementations raise message and error events for device activity.</remarks>
public interface ISerialPortDevice : IDisposable
{
    /// <summary>Occurs when a complete line is received from the open port.</summary>
    event EventHandler<CommunicationMessageEventArgs>? MessageReceived;

    /// <summary>Occurs after a line has been written to the open port.</summary>
    event EventHandler<CommunicationMessageEventArgs>? MessageSent;

    /// <summary>Occurs when serial communication reports an error.</summary>
    event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    /// <summary>Gets the names of serial ports currently available on the system.</summary>
    /// <returns>The available port names.</returns>
    IReadOnlyList<string> GetPortNames();

    /// <summary>Opens a serial port with the specified baud rate.</summary>
    /// <param name="portName">The system name of the port to open.</param>
    /// <param name="baudRate">The communication rate, in bits per second.</param>
    /// <exception cref="ArgumentException">The port name is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The baud rate is not positive.</exception>
    /// <exception cref="InvalidOperationException">The port is already open.</exception>
    /// <exception cref="IOException">The port could not be opened.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the port is denied.</exception>
    void Open(string portName, int baudRate);

    /// <summary>Writes one line to the open serial port.</summary>
    /// <param name="message">The message, without a line terminator.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> contains a line break.</exception>
    /// <exception cref="InvalidOperationException">The port is not open.</exception>
    /// <exception cref="IOException">The message could not be written to the port.</exception>
    void WriteLine(string message);

    /// <summary>Closes the serial port and clears any buffered partial line.</summary>
    void Close();
}
