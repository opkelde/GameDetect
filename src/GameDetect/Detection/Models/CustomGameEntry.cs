using System.Text.Json.Serialization;

namespace GameDetect.Detection.Models;

public class CustomGameEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("launcher")]
    public string Launcher { get; set; } = "Custom";

    [JsonPropertyName("executables")]
    public List<string> Executables { get; set; } = [];

    [JsonPropertyName("match_window_title")]
    public string? MatchWindowTitle { get; set; }
}

public class CustomGamesDatabase
{
    [JsonPropertyName("games")]
    public List<CustomGameEntry> Games { get; set; } = [];
}
