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

/// <summary>Represents one timestamped line in a serial connection log.</summary>
/// <param name="Timestamp">The local time when the line was observed.</param>
/// <param name="PortName">The serial port associated with the line.</param>
/// <param name="Direction">Whether the line was incoming or outgoing.</param>
/// <param name="Message">The line contents.</param>
public sealed record CommunicationLogEntry(DateTime Timestamp, string PortName, string Direction, string Message);

/// <summary>Coordinates serial connection channels and the communication log shown in the add-in view.</summary>
public partial class CommunicationManagerViewModel : TreeItemNode, ITreeItemDocumentData
{
    private readonly Func<CommunicationManagerRuntime> _communicationManagerFactory;
    private readonly IVariableHandler _variableHandler;
    private readonly DirectMessageHistoryStore _historyStore;

    /// <summary>Creates the manager view model with factories and services shared by its channels.</summary>
    /// <param name="communicationManagerFactory">Creates an independent runtime manager for each channel.</param>
    /// <param name="variableHandler">Provides the document properties available for mapping.</param>
    /// <param name="historyStore">Loads and saves each channel's direct-message history.</param>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
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

    /// <summary>Gets the independently configurable serial channels displayed by the view.</summary>
    public ObservableCollection<CommunicationChannelViewModel> Channels { get; } = [];

    /// <summary>Gets the incoming and outgoing lines displayed in the communication log.</summary>
    public ObservableCollection<CommunicationLogEntry> Messages { get; } = [];

    /// <summary>Gets the current manager status or the most recent shutdown error.</summary>
    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>Gets or sets whether the manager section is expanded in the view.</summary>
    [ObservableProperty]
    private bool _isManagerExpanded = true;

    /// <summary>Gets or sets whether the communication log section is expanded in the view.</summary>
    [ObservableProperty]
    private bool _isLogExpanded = true;

    /// <summary>Adds a newly configured serial channel.</summary>
    [RelayCommand]
    private void AddChannel() => Channels.Add(CreateChannel());

    /// <summary>Stops, disposes, and removes the specified channel.</summary>
    /// <param name="channel">The channel to remove; null or an unowned channel is ignored.</param>
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

    /// <summary>Gets the document-tree kind identifier for this item.</summary>
    public override string Kind => CommunicationManagerCreator.DefaultKind;

    /// <summary>Gets zero because a Communication Manager item cannot contain child items.</summary>
    public override int MaxChildren => 0;

    /// <summary>Gets the description shown for this leaf item's child rule.</summary>
    public override string ChildRule => "No Children here";

    /// <summary>Gets whether this item can accept child items; always false.</summary>
    public override bool CanAcceptChildren => false;

    /// <summary>Stops and disposes every channel, reporting connection or disposal failures.</summary>
    /// <exception cref="IOException">One or more channels could not be stopped or disposed.</exception>
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

    /// <summary>Creates a copy of this item with its document settings and channel configuration.</summary>
    /// <returns>A new manager view model with copied tree-item metadata and saved settings.</returns>
    public override TreeItemNode Copy()
    {
        var copy = new CommunicationManagerViewModel(_communicationManagerFactory, _variableHandler, _historyStore);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    /// <summary>Serializes channel settings and the manager and log expansion states.</summary>
    /// <returns>A JSON value suitable for storing with the document item.</returns>
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

    /// <summary>Validates and restores channel settings, including legacy single-channel settings.</summary>
    /// <param name="data">The JSON value previously saved for this document item.</param>
    /// <exception cref="InvalidDataException">The supplied settings or channel configuration is invalid.</exception>
    /// <exception cref="JsonException">The JSON value cannot be deserialized as valid settings.</exception>
    /// <exception cref="InvalidOperationException">The JSON value has an unsupported structure.</exception>
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
