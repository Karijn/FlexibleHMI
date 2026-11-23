using System.IO;
using System.Text.Json;

namespace CommunicationManager.UI;

/// <summary>Persists bounded direct-message histories for communication channels.</summary>
public sealed class DirectMessageHistoryStore
{
    private const int MaximumHistoryCount = 100;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _historyDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlexibleHMI",
        "CommunicationHistory");

    /// <summary>Loads and validates the most recent direct messages for a history identifier.</summary>
    /// <param name="historyId">The non-empty identifier of the history to load.</param>
    /// <returns>The saved messages, or an empty list if no history file exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="historyId"/> is empty.</exception>
    /// <exception cref="InvalidDataException">The saved history contains invalid messages.</exception>
    /// <exception cref="JsonException">The history file is not valid JSON.</exception>
    /// <exception cref="IOException">The history file cannot be read.</exception>
    public IReadOnlyList<string> Load(Guid historyId)
    {
        var historyPath = GetHistoryPath(historyId);
        if (!File.Exists(historyPath))
        {
            return [];
        }

        var history = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(historyPath))
            ?? throw new InvalidDataException("Saved direct-message history is invalid.");
        if (history.Any(message => string.IsNullOrWhiteSpace(message) || message.Contains('\r') || message.Contains('\n')))
        {
            throw new InvalidDataException("Saved direct-message history contains an invalid message.");
        }

        return history.TakeLast(MaximumHistoryCount).ToArray();
    }

    /// <summary>Validates and saves a channel's direct-message history.</summary>
    /// <param name="historyId">The non-empty identifier of the history to save.</param>
    /// <param name="messages">The messages to persist; at most 100 single-line non-whitespace messages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is null.</exception>
    /// <exception cref="ArgumentException">The identifier or message list is invalid.</exception>
    /// <exception cref="IOException">The history file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the history directory is denied.</exception>
    public void Save(Guid historyId, IReadOnlyList<string> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count > MaximumHistoryCount ||
            messages.Any(message => string.IsNullOrWhiteSpace(message) || message.Contains('\r') || message.Contains('\n')))
        {
            throw new ArgumentException("Direct-message history is invalid.", nameof(messages));
        }

        Directory.CreateDirectory(_historyDirectory);
        var historyPath = GetHistoryPath(historyId);
        var temporaryPath = historyPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(messages, SerializerOptions));
        File.Move(temporaryPath, historyPath, true);
    }

    private string GetHistoryPath(Guid historyId)
    {
        if (historyId == Guid.Empty)
        {
            throw new ArgumentException("A history identifier cannot be empty.", nameof(historyId));
        }

        return Path.Combine(_historyDirectory, $"{historyId:N}.json");
    }
}
