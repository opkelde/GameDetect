using System.Text.Json.Serialization;

namespace GameDetect.Detection.Models;

public class KnownGameMapping
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("launcher")]
    public string? Launcher { get; set; }

    [JsonPropertyName("executables")]
    public List<string> Executables { get; set; } = [];
}

public class KnownGameMappingsDatabase
{
    [JsonPropertyName("games")]
    public List<KnownGameMapping> Games { get; set; } = [];
}
