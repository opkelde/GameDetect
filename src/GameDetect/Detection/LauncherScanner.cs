using System.IO;
using System.Text.Json;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Xbox;
using GameDetect.Detection.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameDetect.Configuration;
using NexusMods.Paths;

namespace GameDetect.Detection;

public class LauncherScanner : ILauncherScanner
{
    private readonly ILogger<LauncherScanner> _logger;
    private readonly DetectionSettings _settings;
    private Dictionary<string, KnownGame> _gameDb = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastScan = DateTime.MinValue;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "content", "data", "assets", "media", "resource", "resources", "sound", "music", 
        "locale", "localization", "redist", "_CommonRedist", "directx", "mono", "steamworks"
    };

    public LauncherScanner(ILogger<LauncherScanner> logger, IOptions<DetectionSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public IReadOnlyDictionary<string, KnownGame> GameDatabase => _gameDb;

    public bool NeedsRescan(int intervalMinutes)
    {
        return (DateTime.UtcNow - _lastScan).TotalMinutes >= intervalMinutes;
    }

    private void EnsureDefaultConfigFiles()
    {
        try
        {
            var appDataDir = Path.GetDirectoryName(_settings.KnownGameMappingsPath);
            if (!string.IsNullOrEmpty(appDataDir) && !Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            EnsureConfigFileExists(_settings.KnownGameMappingsPath, "known_game_mappings.json");
            EnsureConfigFileExists(_settings.IgnoredExecutablesPath, "ignored_executables.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default configuration files");
        }
    }

    private void EnsureConfigFileExists(string targetPath, string fileName)
    {
        if (!File.Exists(targetPath))
        {
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, targetPath);
                _logger.LogInformation("Copied default configuration file from {Source} to {Target}", sourcePath, targetPath);
            }
            else
            {
                var emptyJson = fileName.Contains("mappings") ? "{\"games\":[]}" : "{\"ignored_executables\":[]}";
                File.WriteAllText(targetPath, emptyJson);
                _logger.LogWarning("Source configuration file {File} not found in base directory. Created empty file at {Target}", fileName, targetPath);
            }
        }
    }

    private KnownGameMappingsDatabase LoadMappings()
    {
        try
        {
            if (File.Exists(_settings.KnownGameMappingsPath))
            {
                var json = File.ReadAllText(_settings.KnownGameMappingsPath);
                var db = JsonSerializer.Deserialize<KnownGameMappingsDatabase>(json);
                if (db != null) return db;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game mappings from {Path}", _settings.KnownGameMappingsPath);
        }
        return new KnownGameMappingsDatabase();
    }

    private HashSet<string> LoadIgnoredExecutables()
    {
        try
        {
            if (File.Exists(_settings.IgnoredExecutablesPath))
            {
                var json = File.ReadAllText(_settings.IgnoredExecutablesPath);
                var db = JsonSerializer.Deserialize<IgnoredExecutablesDatabase>(json);
                if (db?.IgnoredExecutables != null)
                {
                    return new HashSet<string>(db.IgnoredExecutables, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ignored executables from {Path}", _settings.IgnoredExecutablesPath);
        }
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public void ScanAll()
    {
        _logger.LogInformation("Scanning all launchers for installed games...");
        
        EnsureDefaultConfigFiles();
        var mappings = LoadMappings();
        var ignoredExes = LoadIgnoredExecutables();

        var db = new Dictionary<string, KnownGame>(StringComparer.OrdinalIgnoreCase);

        ScanSteam(db, mappings, ignoredExes);
        ScanGOG(db, mappings, ignoredExes);
        ScanEGS(db, mappings, ignoredExes);
        ScanEA(db, mappings, ignoredExes);
        ScanXbox(db, mappings, ignoredExes);

        _gameDb = db;
        _lastScan = DateTime.UtcNow;
        _logger.LogInformation("Launcher scan complete. Found {Count} game executables mappings.", db.Count);

        try
        {
            var uniqueGames = db.Values.Distinct().OrderBy(g => g.Name).ToList();
            var detectedGamesPath = Path.Combine(Path.GetDirectoryName(_settings.KnownGameMappingsPath) ?? string.Empty, "detected_games.json");
            
            var json = JsonSerializer.Serialize(uniqueGames, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(detectedGamesPath, json);
            _logger.LogInformation("Saved scanned library containing {Count} unique games to {Path}", uniqueGames.Count, detectedGamesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write detected_games.json");
        }
    }

    private void ScanDirectoriesForExecutables(string currentDir, int currentDepth, int maxDepth, HashSet<string> result)
    {
        if (currentDepth > maxDepth)
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(currentDir, "*.exe", SearchOption.TopDirectoryOnly))
            {
                result.Add(Path.GetFileName(file));
            }

            if (currentDepth < maxDepth)
            {
                foreach (var dir in Directory.EnumerateDirectories(currentDir))
                {
                    try
                    {
                        var dirName = Path.GetFileName(dir);
                        if (IgnoredDirectories.Contains(dirName) || dirName.StartsWith('.'))
                            continue;

                        ScanDirectoriesForExecutables(dir, currentDepth + 1, maxDepth, result);
                    }
                    catch
                    {
                        // Ignore access exceptions on subdirectories
                    }
                }
            }
        }
        catch
        {
            // Ignore access exceptions
        }
    }

    private string ResolvePrimaryExecutable(List<string> exes, string gameName, string rootPath)
    {
        if (exes.Count == 1) return exes[0];

        // 1. Try to find an executable that matches the game name (contains parts of the game name)
        var cleanGameName = new string(gameName.Where(char.IsLetterOrDigit).ToArray());
        if (!string.IsNullOrEmpty(cleanGameName))
        {
            var bestMatch = exes.FirstOrDefault(exe =>
            {
                var cleanExe = new string(Path.GetFileNameWithoutExtension(exe).Where(char.IsLetterOrDigit).ToArray());
                return cleanExe.Contains(cleanGameName, StringComparison.OrdinalIgnoreCase) ||
                       cleanGameName.Contains(cleanExe, StringComparison.OrdinalIgnoreCase);
            });
            if (bestMatch != null) return bestMatch;
        }

        // 2. Try to find the largest executable file
        try
        {
            long maxBytes = -1;
            string bestExe = exes[0];
            foreach (var exe in exes)
            {
                var files = Directory.GetFiles(rootPath, exe, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    var fileInfo = new FileInfo(files[0]);
                    if (fileInfo.Length > maxBytes)
                    {
                        maxBytes = fileInfo.Length;
                        bestExe = exe;
                    }
                }
            }
            return bestExe;
        }
        catch
        {
            // Fallback
        }

        return exes[0];
    }

    private void AddGame(Dictionary<string, KnownGame> db, string name, string? path, string launcher, string? appId, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            var detectedExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Scan the game directory recursively (depth up to 3)
            ScanDirectoriesForExecutables(path, 0, 3, detectedExes);

            // 2. Load mappings and check if this game has any additional mapped executables
            KnownGameMapping? matchedMapping = null;
            if (!string.IsNullOrEmpty(appId))
            {
                matchedMapping = mappings.Games.FirstOrDefault(m => string.Equals(m.AppId, appId, StringComparison.OrdinalIgnoreCase));
            }
            if (matchedMapping == null)
            {
                matchedMapping = mappings.Games.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedMapping != null && matchedMapping.Executables != null)
            {
                foreach (var exe in matchedMapping.Executables)
                {
                    if (!string.IsNullOrWhiteSpace(exe))
                    {
                        detectedExes.Add(exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? exe : exe + ".exe");
                    }
                }
            }

            // 3. Filter out blacklisted executables
            var filteredExes = detectedExes
                .Where(exe => !ignoredExes.Contains(exe))
                .ToList();

            if (filteredExes.Count == 0) return;

            // 4. Resolve the primary executable
            var primaryExe = ResolvePrimaryExecutable(filteredExes, name, path);

            var game = new KnownGame(name, primaryExe, path, launcher, appId);

            // 5. Add ALL filtered executables to the database dictionary mapping to this game
            foreach (var exe in filteredExes)
            {
                db.TryAdd(exe, game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve executables for {GameName} at {Path}", name, path);
        }
    }

    private void ScanSteam(Dictionary<string, KnownGame> db, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        try
        {
            var handler = new SteamHandler(FileSystem.Shared, WindowsRegistry.Shared);
            var results = handler.FindAllGames();
            foreach (var result in results)
            {
                if (result.TryGetGame(out var game) && game != null)
                {
                    AddGame(db, game.Name, game.Path.ToString(), "Steam", game.AppId.Value.ToString(), mappings, ignoredExes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan Steam games");
        }
    }

    private void ScanGOG(Dictionary<string, KnownGame> db, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        try
        {
            var handler = new GOGHandler(WindowsRegistry.Shared, FileSystem.Shared);
            var results = handler.FindAllGames();
            foreach (var result in results)
            {
                if (result.TryGetGame(out var game) && game != null)
                {
                    AddGame(db, game.Name, game.Path.ToString(), "GOG", game.Id.Value.ToString(), mappings, ignoredExes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan GOG games");
        }
    }

    private void ScanEGS(Dictionary<string, KnownGame> db, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        try
        {
            var handler = new EGSHandler(WindowsRegistry.Shared, FileSystem.Shared);
            var results = handler.FindAllGames();
            foreach (var result in results)
            {
                if (result.TryGetGame(out var game) && game != null)
                {
                    AddGame(db, game.DisplayName, game.InstallLocation.ToString(), "Epic", game.CatalogItemId.Value, mappings, ignoredExes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan Epic Games");
        }
    }

    private void ScanEA(Dictionary<string, KnownGame> db, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        try
        {
            var handler = new EADesktopHandler(FileSystem.Shared, new HardwareInfoProvider());
            var results = handler.FindAllGames();
            foreach (var result in results)
            {
                if (result.TryGetGame(out var game) && game != null)
                {
                    AddGame(db, game.BaseSlug, game.BaseInstallPath.ToString(), "EA", game.EADesktopGameId.Value, mappings, ignoredExes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan EA Desktop games. This can happen on hardware changes.");
        }
    }

    private void ScanXbox(Dictionary<string, KnownGame> db, KnownGameMappingsDatabase mappings, HashSet<string> ignoredExes)
    {
        try
        {
            var handler = new XboxHandler(FileSystem.Shared);
            var results = handler.FindAllGames();
            foreach (var result in results)
            {
                if (result.TryGetGame(out var game) && game != null)
                {
                    AddGame(db, game.DisplayName, game.Path.ToString(), "Xbox", game.Id.Value, mappings, ignoredExes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan Xbox games");
        }
    }
}

