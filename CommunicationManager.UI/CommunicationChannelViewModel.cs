using CommunicationManager.Interfaces;
using CommunicationManagerRuntime = global::CommunicationManager.CommunicationManager;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace CommunicationManager.UI;

public sealed partial class CommunicationChannelViewModel : ObservableObject
{
    private const int MaximumDirectMessageHistoryCount = 100;

    private readonly CommunicationManagerRuntime _communicationManager;
    private readonly IVariableHandler _variableHandler;
    private readonly DirectMessageHistoryStore _historyStore;
    private readonly Action<CommunicationLogEntry> _logMessage;
    private readonly List<string> _directMessageHistory = [];
    private int _directMessageHistoryIndex;
    private string _directMessageDraft = string.Empty;
    private bool _isNavigatingDirectMessageHistory;
    private bool _disposed;

    public CommunicationChannelViewModel(
        CommunicationManagerRuntime communicationManager,
        IVariableHandler variableHandler,
        DirectMessageHistoryStore historyStore,
        Action<CommunicationLogEntry> logMessage)
    {
        _communicationManager = communicationManager ?? throw new ArgumentNullException(nameof(communicationManager));
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        _logMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        HistoryId = Guid.NewGuid();
        BaudRates = [1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200];

        WeakEventManager<CommunicationManagerRuntime, CommunicationErrorEventArgs>.AddHandler(
            _communicationManager,
            nameof(CommunicationManagerRuntime.ErrorOccurred),
            OnCommunicationErrorOccurred);
        WeakEventManager<CommunicationManagerRuntime, CommunicationMessageEventArgs>.AddHandler(
            _communicationManager,
            nameof(CommunicationManagerRuntime.MessageReceived),
            OnMessageReceived);
        WeakEventManager<CommunicationManagerRuntime, CommunicationMessageEventArgs>.AddHandler(
            _communicationManager,
            nameof(CommunicationManagerRuntime.MessageSent),
            OnMessageSent);

        RefreshPorts();
        RefreshProperties();
    }

    public Guid HistoryId { get; private set; }

    public ObservableCollection<string> PortNames { get; } = [];

    public ObservableCollection<int> BaudRates { get; }

    public ObservableCollection<string> PropertyNames { get; } = [];

    public ObservableCollection<CommunicationMappingViewModel> Mappings { get; } = [];

    [ObservableProperty]
    private string _portName = string.Empty;

    [ObservableProperty]
    private int _baudRate = 9600;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _status = "Disconnected";

    [ObservableProperty]
    private string _directMessage = string.Empty;

    public bool CanConfigure => !IsRunning;

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanConfigure));
        SendDirectMessageCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    partial void OnDirectMessageChanged(string value)
    {
        SendDirectMessageCommand.NotifyCanExecuteChanged();
        if (!_isNavigatingDirectMessageHistory)
        {
            _directMessageHistoryIndex = _directMessageHistory.Count;
            _directMessageDraft = value;
        }
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        try
        {
            var selectedPort = PortName;
            PortNames.Clear();
            foreach (var port in _communicationManager.GetPortNames())
            {
                PortNames.Add(port);
            }

            if (string.IsNullOrWhiteSpace(selectedPort) && PortNames.Count > 0)
            {
                PortName = PortNames[0];
            }
            else if (!string.IsNullOrWhiteSpace(selectedPort) && !PortNames.Contains(selectedPort, StringComparer.OrdinalIgnoreCase))
            {
                PortName = selectedPort;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Status = $"Unable to list serial ports: {exception.Message}";
        }
    }

    [RelayCommand]
    private void RefreshProperties()
    {
        var selectedNames = _variableHandler.GetVariableNames()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        PropertyNames.Clear();
        foreach (var name in selectedNames)
        {
            PropertyNames.Add(name);
        }
    }

    [RelayCommand]
    private void AddMapping()
    {
        RefreshProperties();
        var propertyName = PropertyNames.FirstOrDefault() ?? string.Empty;
        Mappings.Add(new CommunicationMappingViewModel(propertyName, propertyName));
    }

    [RelayCommand]
    private void RemoveMapping(CommunicationMappingViewModel? mapping)
    {
        if (mapping is not null)
        {
            Mappings.Remove(mapping);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        try
        {
            Status = $"Connecting to {PortName} at {BaudRate} baud...";
            _communicationManager.Start(
                PortName.Trim(),
                BaudRate,
                Mappings.Select(mapping => new CommunicationPropertyRoute(
                    mapping.ExternalName.Trim(),
                    mapping.PropertyName.Trim(),
                    mapping.SendUpdates)));
            IsRunning = true;
            if (Status.StartsWith("Connecting", StringComparison.Ordinal))
            {
                Status = $"Connected to {PortName} at {BaudRate} baud.";
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Status = $"Unable to connect: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop()
    {
        try
        {
            _communicationManager.Stop();
            IsRunning = false;
            Status = "Disconnected";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            IsRunning = _communicationManager.IsRunning;
            Status = $"Unable to disconnect: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendDirectMessage))]
    private void SendDirectMessage()
    {
        try
        {
            var message = DirectMessage;
            _communicationManager.SendMessage(message);
            _directMessageHistory.Add(message);
            if (_directMessageHistory.Count > MaximumDirectMessageHistoryCount)
            {
                _directMessageHistory.RemoveAt(0);
            }

            _directMessageHistoryIndex = _directMessageHistory.Count;
            try
            {
                _historyStore.Save(HistoryId, _directMessageHistory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Status = $"Message sent, but history could not be saved: {exception.Message}";
            }

            DirectMessage = string.Empty;
            _directMessageDraft = string.Empty;
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Status = $"Unable to send message: {exception.Message}";
        }
    }

    public void LoadDirectMessageHistory(Guid? historyId)
    {
        if (historyId is { } savedHistoryId && savedHistoryId == Guid.Empty)
        {
            throw new InvalidDataException("The document contains an invalid direct-message history identifier.");
        }

        HistoryId = historyId ?? Guid.NewGuid();
        _directMessageHistory.Clear();
        _directMessageHistory.AddRange(_historyStore.Load(HistoryId));
        _directMessageHistoryIndex = _directMessageHistory.Count;
    }

    public bool RecallPreviousDirectMessage()
    {
        if (_directMessageHistory.Count == 0)
        {
            return false;
        }

        if (_directMessageHistoryIndex == _directMessageHistory.Count)
        {
            _directMessageDraft = DirectMessage;
        }

        if (_directMessageHistoryIndex > 0)
        {
            _directMessageHistoryIndex--;
        }

        SetDirectMessageFromHistory(_directMessageHistory[_directMessageHistoryIndex]);
        return true;
    }

    public bool RecallNextDirectMessage()
    {
        if (_directMessageHistoryIndex >= _directMessageHistory.Count)
        {
            return false;
        }

        _directMessageHistoryIndex++;
        SetDirectMessageFromHistory(_directMessageHistoryIndex == _directMessageHistory.Count
            ? _directMessageDraft
            : _directMessageHistory[_directMessageHistoryIndex]);
        return true;
    }

    public void StopRuntimeActivity()
    {
        _communicationManager.Stop();
        IsRunning = false;
        Status = "Disconnected";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _communicationManager.Dispose();
        }
        finally
        {
            WeakEventManager<CommunicationManagerRuntime, CommunicationErrorEventArgs>.RemoveHandler(
                _communicationManager,
                nameof(CommunicationManagerRuntime.ErrorOccurred),
                OnCommunicationErrorOccurred);
            WeakEventManager<CommunicationManagerRuntime, CommunicationMessageEventArgs>.RemoveHandler(
                _communicationManager,
                nameof(CommunicationManagerRuntime.MessageReceived),
                OnMessageReceived);
            WeakEventManager<CommunicationManagerRuntime, CommunicationMessageEventArgs>.RemoveHandler(
                _communicationManager,
                nameof(CommunicationManagerRuntime.MessageSent),
                OnMessageSent);
            _disposed = true;
            IsRunning = false;
        }
    }

    private bool CanStart() => !IsRunning;

    private bool CanSendDirectMessage() =>
        IsRunning && !string.IsNullOrWhiteSpace(DirectMessage) &&
        !DirectMessage.Contains('\r') && !DirectMessage.Contains('\n');

    private void SetDirectMessageFromHistory(string message)
    {
        _isNavigatingDirectMessageHistory = true;
        try
        {
            DirectMessage = message;
        }
        finally
        {
            _isNavigatingDirectMessageHistory = false;
        }
    }

    private void OnCommunicationErrorOccurred(object? sender, CommunicationErrorEventArgs eventArgs) =>
        DispatchToUi(() => Status = eventArgs.Message);

    private void OnMessageReceived(object? sender, CommunicationMessageEventArgs eventArgs) =>
        _logMessage(new CommunicationLogEntry(DateTime.Now, PortName, "Incoming", eventArgs.Message));

    private void OnMessageSent(object? sender, CommunicationMessageEventArgs eventArgs) =>
        _logMessage(new CommunicationLogEntry(DateTime.Now, PortName, "Outgoing", eventArgs.Message));

    private static void DispatchToUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action, DispatcherPriority.DataBind);
    }
}
