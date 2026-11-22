using CommunicationManager.Interfaces;
using System.IO.Ports;
using System.Text;

namespace CommunicationManager;

public sealed class SerialPortDevice : ISerialPortDevice
{
    private const int MaximumLineLength = 4096;
    private readonly SerialPort _serialPort = new();
    private readonly StringBuilder _lineBuffer = new();
    private readonly object _bufferLock = new();
    private readonly object _portLock = new();
    private bool _discardingLine;
    private bool _disposed;

    public event EventHandler<CommunicationMessageEventArgs>? MessageReceived;

    public event EventHandler<CommunicationMessageEventArgs>? MessageSent;

    public event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    public IReadOnlyList<string> GetPortNames() => SerialPort.GetPortNames()
        .OrderBy(portName => portName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void Open(string portName, int baudRate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        lock (_portLock)
        {
            if (_serialPort.IsOpen)
            {
                throw new InvalidOperationException("The serial port is already open.");
            }

            lock (_bufferLock)
            {
                _lineBuffer.Clear();
                _discardingLine = false;
            }

            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.NewLine = "\n";
            _serialPort.Encoding = Encoding.UTF8;
            _serialPort.DataReceived += OnDataReceived;

            try
            {
                _serialPort.Open();
            }
            catch
            {
                _serialPort.DataReceived -= OnDataReceived;
                throw;
            }
        }
    }

    public void WriteLine(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        if (message.Contains('\r') || message.Contains('\n'))
        {
            throw new ArgumentException("A serial message must not contain a line break.", nameof(message));
        }

        lock (_portLock)
        {
            if (!_serialPort.IsOpen)
            {
                throw new InvalidOperationException("The serial port is not open.");
            }

            _serialPort.Write(message + "\n");
        }

        MessageSent?.Invoke(this, new CommunicationMessageEventArgs(message));
    }

    public void Close()
    {
        lock (_portLock)
        {
            _serialPort.DataReceived -= OnDataReceived;

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        lock (_bufferLock)
        {
            _lineBuffer.Clear();
            _discardingLine = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            Close();
        }
        finally
        {
            _serialPort.Dispose();
            _disposed = true;
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs eventArgs)
    {
        List<string> completedLines = [];
        var lineTooLong = false;

        try
        {
            var receivedText = _serialPort.ReadExisting();
            lock (_bufferLock)
            {
                foreach (var character in receivedText)
                {
                    if (character == '\n')
                    {
                        if (!_discardingLine)
                        {
                            completedLines.Add(_lineBuffer.ToString().TrimEnd('\r'));
                        }

                        _lineBuffer.Clear();
                        _discardingLine = false;
                        continue;
                    }

                    if (_discardingLine)
                    {
                        continue;
                    }

                    if (_lineBuffer.Length == MaximumLineLength)
                    {
                        _lineBuffer.Clear();
                        _discardingLine = true;
                        lineTooLong = true;
                        continue;
                    }

                    _lineBuffer.Append(character);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            ErrorOccurred?.Invoke(this, new CommunicationErrorEventArgs(exception.Message));
            return;
        }

        if (lineTooLong)
        {
            ErrorOccurred?.Invoke(this, new CommunicationErrorEventArgs(
                $"A serial message exceeded the {MaximumLineLength}-character limit and was discarded."));
        }

        foreach (var line in completedLines)
        {
            MessageReceived?.Invoke(this, new CommunicationMessageEventArgs(line));
        }
    }
}
