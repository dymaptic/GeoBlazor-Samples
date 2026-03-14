namespace NationFinder.Client;

public record CommunicationResult(bool Success, string? ErrorMessage = null);

public static class State
{
    public static readonly List<string> ConnectedUsers = [];
    public static readonly HashSet<string> Emails = [];
    public static readonly Dictionary<string, string> UserGuesses = [];
    public static string? CurrentCountry { get; set; }
    public static Func<Task>? UpdateGameBoard { get; set; }
    public static List<string> Winners { get; set; } = [];
    public static Func<Task>? FinalizeGame { get; set; }
    public static Func<Task>? ResetGame { get; set; }
}