using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameDetect.Detection.Models;

public class IgnoredGamesDatabase
{
    [JsonPropertyName("ignored_games")]
    public List<string> IgnoredGames { get; set; } = new();
}
