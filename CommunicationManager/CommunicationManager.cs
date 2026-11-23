using CommunicationManager.Interfaces;
using PropertyManager.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace CommunicationManager;

public sealed class CommunicationManager : IDisposable
{
    private readonly IVariableHandler _variableHandler;
    private readonly IStructTypeRegistry _structTypeRegistry;
    private readonly ISerialPortDevice _serialPortDevice;
    private readonly object _syncRoot = new();
    private readonly AsyncLocal<string?> _propertyBeingUpdatedByDevice = new();
    private CommunicationPropertyRoute[] _routes = [];
    private bool _isRunning;
    private bool _disposed;

    public CommunicationManager(
        IVariableHandler variableHandler,
        IStructTypeRegistry structTypeRegistry,
        ISerialPortDevice serialPortDevice)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _structTypeRegistry = structTypeRegistry ?? throw new ArgumentNullException(nameof(structTypeRegistry));
        _serialPortDevice = serialPortDevice ?? throw new ArgumentNullException(nameof(serialPortDevice));
        _variableHandler.VariableChanged += OnVariableChanged;
    }

    public event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    public event EventHandler<CommunicationMessageEventArgs>? MessageReceived;

    public event EventHandler<CommunicationMessageEventArgs>? MessageSent;

    public IReadOnlyList<string> GetPortNames() => _serialPortDevice.GetPortNames();

    public bool IsRunning
    {
        get
        {
            lock (_syncRoot)
            {
                return _isRunning;
            }
        }
    }

    public void Start(
        string portName,
        int baudRate,
        IEnumerable<CommunicationPropertyRoute> routes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);
        ArgumentNullException.ThrowIfNull(routes);

        var configuredRoutes = routes.ToArray();
        ValidateRoutes(configuredRoutes);

        lock (_syncRoot)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("The communication manager is already running.");
            }

            _routes = configuredRoutes;
            _isRunning = true;
        }

        _serialPortDevice.MessageReceived += OnMessageReceived;
        _serialPortDevice.MessageSent += OnMessageSent;
        _serialPortDevice.ErrorOccurred += OnDeviceErrorOccurred;
        try
        {
            _serialPortDevice.Open(portName, baudRate);
        }
        catch
        {
            _serialPortDevice.MessageReceived -= OnMessageReceived;
            _serialPortDevice.MessageSent -= OnMessageSent;
            _serialPortDevice.ErrorOccurred -= OnDeviceErrorOccurred;
            lock (_syncRoot)
            {
                _routes = [];
                _isRunning = false;
            }

            throw;
        }

        foreach (var route in configuredRoutes.Where(route => route.SendUpdates))
        {
            SendCurrentValue(route);
        }
    }

    public void SendMessage(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (message.Contains('\r') || message.Contains('\n'))
        {
            throw new ArgumentException("A direct message must not contain a line break.", nameof(message));
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Connect to a serial port before sending a direct message.");
        }

        _serialPortDevice.WriteLine(message);
    }

    public void Stop()
    {
        lock (_syncRoot)
        {
            _isRunning = false;
            _routes = [];
        }

        _serialPortDevice.MessageReceived -= OnMessageReceived;
        _serialPortDevice.MessageSent -= OnMessageSent;
        _serialPortDevice.ErrorOccurred -= OnDeviceErrorOccurred;
        _serialPortDevice.Close();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            Stop();
        }
        finally
        {
            _variableHandler.VariableChanged -= OnVariableChanged;
            _serialPortDevice.Dispose();
            _disposed = true;
        }
    }

    private static void ValidateRoutes(IReadOnlyCollection<CommunicationPropertyRoute> routes)
    {
        var externalNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in routes)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentException.ThrowIfNullOrWhiteSpace(route.ExternalName);
            ArgumentException.ThrowIfNullOrWhiteSpace(route.PropertyName);

            if (route.ExternalName != route.ExternalName.Trim() ||
                route.ExternalName.IndexOfAny(['!', ':', '\r', '\n']) >= 0)
            {
                throw new ArgumentException(
                    $"External name '{route.ExternalName}' must not contain '!', ':', or line breaks, and must not have surrounding whitespace.",
                    nameof(routes));
            }

            if (!externalNames.Add(route.ExternalName))
            {
                throw new ArgumentException($"External name '{route.ExternalName}' is configured more than once.", nameof(routes));
            }
        }
    }

    private void OnMessageReceived(object? sender, CommunicationMessageEventArgs eventArgs)
    {
        if (!IsRunning)
        {
            return;
        }

        MessageReceived?.Invoke(this, eventArgs);
        var line = eventArgs.Message;
        if (!line.StartsWith('!'))
        {
            ReportError("Received a serial message that does not start with '!'.");
            return;
        }

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex < 2)
        {
            ReportError("Received a serial message without an external name and ':'.");
            return;
        }

        var externalName = line[1..separatorIndex].Trim();
        if (externalName.Length == 0)
        {
            ReportError("Received a serial message with an empty external name.");
            return;
        }

        var arguments = line[(separatorIndex + 1)..].Trim();
        if (arguments.Length == 0)
        {
            return;
        }

        var route = GetRoutes().FirstOrDefault(candidate =>
            string.Equals(candidate.ExternalName, externalName, StringComparison.Ordinal));
        if (route is null)
        {
            ReportError($"No property is configured for external name '{externalName}'.");
            return;
        }

        ApplyIncomingValue(route.PropertyName, arguments);
    }

    private void ApplyIncomingValue(string propertyName, string arguments)
    {
        try
        {
            _propertyBeingUpdatedByDevice.Value = propertyName;
            if (_variableHandler.TryGet<int>(propertyName, out _) &&
                int.TryParse(arguments, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                _variableHandler.Set(propertyName, integer);
                return;
            }

            if (_variableHandler.TryGet<double>(propertyName, out _) &&
                double.TryParse(arguments, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
                double.IsFinite(number))
            {
                _variableHandler.Set(propertyName, number);
                return;
            }

            if (_variableHandler.TryGet<bool>(propertyName, out _) &&
                BooleanValueParser.TryParse(arguments, out var boolean))
            {
                _variableHandler.Set(propertyName, boolean);
                return;
            }

            if (_variableHandler.TryGet<string>(propertyName, out _))
            {
                _variableHandler.Set(propertyName, arguments);
                return;
            }

            if (_variableHandler.TryGet<DateTime>(propertyName, out _) &&
                DateTime.TryParse(arguments, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                _variableHandler.Set(propertyName, dateTime);
                return;
            }

            if (_variableHandler.TryGet<TimeSpan>(propertyName, out _) &&
                TimeSpan.TryParse(arguments, CultureInfo.InvariantCulture, out var timeSpan))
            {
                _variableHandler.Set(propertyName, timeSpan);
                return;
            }

            if (_variableHandler.TryGet<System.Drawing.Color>(propertyName, out _) && ColorValue.TryParse(arguments, out var color))
            {
                _variableHandler.Set(propertyName, color);
                return;
            }

            if (_variableHandler.TryGet<StructValue>(propertyName, out var structure) && structure is not null)
            {
                try
                {
                    _variableHandler.Set(propertyName, _structTypeRegistry.ParseValue(structure.TypeName, arguments));
                    return;
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
                {
                    ReportError($"'{arguments}' is not a valid value for struct property '{propertyName}': {exception.Message}");
                    return;
                }
            }

            if (_variableHandler.TryGet<ArrayValue>(propertyName, out var array) && array is not null)
            {
                try
                {
                    _variableHandler.Set(propertyName, _structTypeRegistry.ParseTypedValue(
                        $"{array.ElementTypeName}[]", arguments));
                    return;
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
                {
                    ReportError($"'{arguments}' is not a valid value for array property '{propertyName}': {exception.Message}");
                    return;
                }
            }

            if (_variableHandler.GetVariableNames().Contains(propertyName, StringComparer.Ordinal))
            {
                ReportError($"'{arguments}' is not a valid value for property '{propertyName}'.");
            }
            else
            {
                ReportError($"Property '{propertyName}' does not exist.");
            }
        }
        finally
        {
            _propertyBeingUpdatedByDevice.Value = null;
        }
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        if (change.NewValue is null || !IsRunning)
        {
            return;
        }

        var propertyBeingUpdatedByDevice = _propertyBeingUpdatedByDevice.Value;
        var isIncomingValue = string.Equals(propertyBeingUpdatedByDevice, change.Name, StringComparison.Ordinal);
        var routes = GetRoutes().Where(route => route.SendUpdates).ToArray();
        var matchingRoutes = routes.Where(route =>
            string.Equals(route.PropertyName, change.Name, StringComparison.Ordinal)).ToArray();
        if (!isIncomingValue && matchingRoutes.Length > 0 &&
            !(propertyBeingUpdatedByDevice?.StartsWith($"{change.Name}.", StringComparison.Ordinal) ?? false))
        {
            foreach (var route in matchingRoutes)
            {
                SendValue(route, change.NewValue);
            }
        }

        var candidates = new List<(string PropertyName, StructValue Value, string MemberPath)>();
        foreach (var propertyName in routes.Select(route => route.PropertyName).Distinct(StringComparer.Ordinal))
        {
            if (!_variableHandler.TryGet<StructValue>(propertyName, out var structure) || structure is null)
            {
                continue;
            }

            var memberName = change.Name.StartsWith($"{propertyName}.", StringComparison.Ordinal)
                ? change.Name[(propertyName.Length + 1)..]
                : change.Name;
            candidates.AddRange(_structTypeRegistry.FindMemberPaths(structure, memberName)
                .Select(memberPath => (propertyName, structure, memberPath)));
        }

        var distinctCandidates = candidates
            .DistinctBy(candidate => (candidate.PropertyName, candidate.MemberPath))
            .ToArray();
        if (distinctCandidates.Length == 0)
        {
            return;
        }

        if (distinctCandidates.Length > 1)
        {
            ReportError($"Member property '{change.Name}' matches more than one field in mapped structs; use a qualified member name.");
            return;
        }

        var candidate = distinctCandidates[0];
        var previousPropertyBeingUpdatedByDevice = _propertyBeingUpdatedByDevice.Value;
        try
        {
            if (previousPropertyBeingUpdatedByDevice is not null)
            {
                _propertyBeingUpdatedByDevice.Value = $"{candidate.PropertyName}.{candidate.MemberPath}";
            }

            _variableHandler.Set(candidate.PropertyName,
                _structTypeRegistry.UpdateMemberValue(candidate.Value, candidate.MemberPath, change.NewValue));
        }
        catch (InvalidDataException exception)
        {
            ReportError($"Unable to update mapped struct '{candidate.PropertyName}' from member '{change.Name}': {exception.Message}");
        }
        finally
        {
            _propertyBeingUpdatedByDevice.Value = previousPropertyBeingUpdatedByDevice;
        }
    }

    private void SendCurrentValue(CommunicationPropertyRoute route)
    {
        if (_variableHandler.TryGet<int>(route.PropertyName, out var integer))
        {
            SendValue(route, integer);
        }
        else if (_variableHandler.TryGet<double>(route.PropertyName, out var number))
        {
            SendValue(route, number);
        }
        else if (_variableHandler.TryGet<bool>(route.PropertyName, out var boolean))
        {
            SendValue(route, boolean);
        }
        else if (_variableHandler.TryGet<string>(route.PropertyName, out var text))
        {
            SendValue(route, text);
        }
        else if (_variableHandler.TryGet<DateTime>(route.PropertyName, out var dateTime))
        {
            SendValue(route, dateTime);
        }
        else if (_variableHandler.TryGet<TimeSpan>(route.PropertyName, out var timeSpan))
        {
            SendValue(route, timeSpan);
        }
        else if (_variableHandler.TryGet<System.Drawing.Color>(route.PropertyName, out var color))
        {
            SendValue(route, color);
        }
        else if (_variableHandler.TryGet<StructValue>(route.PropertyName, out var structure) && structure is not null)
        {
            SendValue(route, structure);
        }
        else if (_variableHandler.TryGet<ArrayValue>(route.PropertyName, out var array) && array is not null)
        {
            SendValue(route, array);
        }
        else
        {
            ReportError($"Property '{route.PropertyName}' does not exist or has an unsupported type.");
        }
    }

    private void SendValue(CommunicationPropertyRoute route, object? value)
    {
        string? formattedValue;
        try
        {
            formattedValue = value switch
            {
                int integer => integer.ToString(CultureInfo.InvariantCulture),
                double number when double.IsFinite(number) => number.ToString("R", CultureInfo.InvariantCulture),
                bool boolean => boolean ? "True" : "False",
                string text => text,
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
                System.Drawing.Color color => ColorValue.Format(color),
                StructValue structure => _structTypeRegistry.SerializeValue(structure),
                ArrayValue array => _structTypeRegistry.SerializeTypedValue($"{array.ElementTypeName}[]", array),
                _ => null
            };
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or NotSupportedException)
        {
            ReportError($"Property '{route.PropertyName}' cannot be sent: {exception.Message}");
            return;
        }

        if (formattedValue is null)
        {
            ReportError($"Property '{route.PropertyName}' has a value that cannot be sent.");
            return;
        }

        try
        {
            _serialPortDevice.WriteLine($"!{route.ExternalName}:{formattedValue}");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            ReportError(exception.Message);
        }
    }

    private CommunicationPropertyRoute[] GetRoutes()
    {
        lock (_syncRoot)
        {
            return _routes;
        }
    }

    private void OnMessageSent(object? sender, CommunicationMessageEventArgs eventArgs) => MessageSent?.Invoke(this, eventArgs);

    private void OnDeviceErrorOccurred(object? sender, CommunicationErrorEventArgs eventArgs) => ReportError(eventArgs.Message);

    private void ReportError(string message) => ErrorOccurred?.Invoke(this, new CommunicationErrorEventArgs(message));
}
