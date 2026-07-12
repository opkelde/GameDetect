using System.Text.Json.Serialization;

namespace GameDetect.Detection.Models;

public class IgnoredExecutablesDatabase
{
    [JsonPropertyName("ignored_executables")]
    public List<string> IgnoredExecutables { get; set; } = [];
}
