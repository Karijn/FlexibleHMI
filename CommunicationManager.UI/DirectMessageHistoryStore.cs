using System.IO;
using System.Text.Json;

namespace CommunicationManager.UI;

public sealed class DirectMessageHistoryStore
{
    private const int MaximumHistoryCount = 100;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _historyDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlexibleHMI",
        "CommunicationHistory");

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
