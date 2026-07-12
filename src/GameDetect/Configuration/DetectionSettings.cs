namespace GameDetect.Configuration;

public class DetectionSettings
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int DebounceSeconds { get; set; } = 10;
    public bool EnableFullscreenDetection { get; set; } = true;
    public bool EnableLauncherScanning { get; set; } = true;
    public string CustomGamesPath { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "GameDetect", "custom_games.json");
    public string KnownGameMappingsPath { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "GameDetect", "known_game_mappings.json");
    public string IgnoredExecutablesPath { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "GameDetect", "ignored_executables.json");
    public string IgnoredGamesPath { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "GameDetect", "ignored_games.json");
    public int LauncherRescanIntervalMinutes { get; set; } = 60;
}

