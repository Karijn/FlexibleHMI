using CommunicationManager.Interfaces;
using CommunicationManagerRuntime = global::CommunicationManager.CommunicationManager;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace CommunicationManager.UI;

public sealed record CommunicationLogEntry(DateTime Timestamp, string PortName, string Direction, string Message);

public partial class CommunicationManagerViewModel : TreeItemNode, ITreeItemDocumentData
{
    private readonly Func<CommunicationManagerRuntime> _communicationManagerFactory;
    private readonly IVariableHandler _variableHandler;
    private readonly DirectMessageHistoryStore _historyStore;

    public CommunicationManagerViewModel(
        Func<CommunicationManagerRuntime> communicationManagerFactory,
        IVariableHandler variableHandler,
        DirectMessageHistoryStore historyStore)
    {
        _communicationManagerFactory = communicationManagerFactory ?? throw new ArgumentNullException(nameof(communicationManagerFactory));
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        Name = "Communication Manager";
        Description = "Communicate with external devices over multiple serial ports.";
        Image = CommunicationManagerCreator.GetIcon();
        Channels.Add(CreateChannel());
    }

    public ObservableCollection<CommunicationChannelViewModel> Channels { get; } = [];

    public ObservableCollection<CommunicationLogEntry> Messages { get; } = [];

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isManagerExpanded = true;

    [ObservableProperty]
    private bool _isLogExpanded = true;

    [RelayCommand]
    private void AddChannel() => Channels.Add(CreateChannel());

    [RelayCommand]
    private void RemoveChannel(CommunicationChannelViewModel? channel)
    {
        if (channel is null || !Channels.Contains(channel))
        {
            return;
        }

        try
        {
            channel.StopRuntimeActivity();
            channel.Dispose();
            Channels.Remove(channel);
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Status = $"Unable to disconnect {channel.PortName}: {exception.Message}";
        }
    }

    private CommunicationChannelViewModel CreateChannel() =>
        new(_communicationManagerFactory(), _variableHandler, _historyStore, AddMessage);

    private void AddMessage(CommunicationLogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Messages.Add(entry);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => Messages.Add(entry)), DispatcherPriority.DataBind);
    }

    public override string Kind => CommunicationManagerCreator.DefaultKind;

    public override int MaxChildren => 0;

    public override string ChildRule => "No Children here";

    public override bool CanAcceptChildren => false;

    public override void StopRuntimeActivity()
    {
        var errors = new List<Exception>();
        foreach (var channel in Channels)
        {
            try
            {
                channel.StopRuntimeActivity();
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            Status = $"Unable to stop all serial connections: {string.Join("; ", errors.Select(error => error.Message))}";
            throw new IOException(Status, new AggregateException(errors));
        }

        foreach (var channel in Channels)
        {
            try
            {
                channel.Dispose();
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            Status = $"Unable to close all serial connections: {string.Join("; ", errors.Select(error => error.Message))}";
            throw new IOException(Status, new AggregateException(errors));
        }

        Status = "Disconnected";
    }

    public override TreeItemNode Copy()
    {
        var copy = new CommunicationManagerViewModel(_communicationManagerFactory, _variableHandler, _historyStore);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        new CommunicationDocumentSettings(
            Channels.Select(channel => new CommunicationChannelSettings(
                channel.PortName,
                channel.BaudRate,
                channel.Mappings.Select(mapping => new MappingSettings(
                    mapping.PropertyName,
                    mapping.ExternalName,
                    mapping.SendUpdates)).ToList(),
                channel.HistoryId)).ToList(),
            IsManagerExpanded,
            IsLogExpanded));

    public void LoadDocumentData(JsonElement data)
    {
        List<CommunicationChannelSettings> channels;
        if (data.TryGetProperty("Channels", out _))
        {
            var settings = data.Deserialize<CommunicationDocumentSettings>();
            if (settings?.Channels is null || settings.Channels.Any(channel => channel is null))
            {
                throw new InvalidDataException("The document contains invalid communication manager settings.");
            }

            channels = settings.Channels;
            IsManagerExpanded = settings.IsManagerExpanded;
            IsLogExpanded = settings.IsLogExpanded;
        }
        else
        {
            var legacySettings = data.Deserialize<LegacyCommunicationSettings>();
            if (legacySettings is null || legacySettings.BaudRate <= 0 || legacySettings.Mappings is null ||
                legacySettings.Mappings.Any(mapping => mapping is null))
            {
                throw new InvalidDataException("The document contains invalid communication manager settings.");
            }

            channels =
            [
                new CommunicationChannelSettings(
                    legacySettings.PortName,
                    legacySettings.BaudRate,
                    legacySettings.Mappings,
                    null)
            ];
        }

        foreach (var channel in channels)
        {
            if (channel.BaudRate <= 0 || channel.Mappings is null || channel.Mappings.Any(mapping => mapping is null))
            {
                throw new InvalidDataException("The document contains invalid serial connection settings.");
            }
        }

        foreach (var existingChannel in Channels)
        {
            existingChannel.Dispose();
        }

        Channels.Clear();
        foreach (var channel in channels)
        {
            var viewModel = CreateChannel();
            viewModel.PortName = channel.PortName ?? string.Empty;
            viewModel.BaudRate = channel.BaudRate;
            foreach (var mapping in channel.Mappings!)
            {
                viewModel.Mappings.Add(new CommunicationMappingViewModel(
                    mapping.PropertyName ?? string.Empty,
                    mapping.ExternalName ?? string.Empty)
                {
                    SendUpdates = mapping.SendUpdates
                });
            }

            viewModel.LoadDirectMessageHistory(channel.HistoryId);
            viewModel.RefreshPortsCommand.Execute(null);
            viewModel.RefreshPropertiesCommand.Execute(null);
            Channels.Add(viewModel);
        }
    }

    private sealed record CommunicationDocumentSettings(
        List<CommunicationChannelSettings>? Channels,
        bool IsManagerExpanded = true,
        bool IsLogExpanded = true);

    private sealed record CommunicationChannelSettings(
        string? PortName,
        int BaudRate,
        List<MappingSettings>? Mappings,
        Guid? HistoryId = null);

    private sealed record LegacyCommunicationSettings(
        string? PortName,
        int BaudRate,
        List<MappingSettings>? Mappings);

    private sealed record MappingSettings(string? PropertyName, string? ExternalName, bool SendUpdates);
}
