namespace CommunicationManager.Interfaces;

public sealed class CommunicationMessageEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public sealed class CommunicationErrorEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public sealed record CommunicationPropertyRoute(
    string ExternalName,
    string PropertyName,
    bool SendUpdates);

public interface ISerialPortDevice : IDisposable
{
    event EventHandler<CommunicationMessageEventArgs>? MessageReceived;

    event EventHandler<CommunicationMessageEventArgs>? MessageSent;

    event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    IReadOnlyList<string> GetPortNames();

    void Open(string portName, int baudRate);

    void WriteLine(string message);

    void Close();
}
