using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using GameDetect.Configuration;
using GameDetect.Detection;
using GameDetect.Detection.Models;
using Xunit;

namespace GameDetect.Tests;

public class CustomGamesAndSettingsTests
{
    [Fact]
    public void CustomGameEntry_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "Custom Game X",
            "app_id": "12345",
            "launcher": "Custom Steam",
            "executables": ["game.exe", "launcher.exe"],
            "match_window_title": "Custom Window"
        }
        """;

        // Act
        var entry = JsonSerializer.Deserialize<CustomGameEntry>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        entry.Should().NotBeNull();
        entry!.Name.Should().Be("Custom Game X");
        entry.AppId.Should().Be("12345");
        entry.Launcher.Should().Be("Custom Steam");
        entry.Executables.Should().ContainInOrder("game.exe", "launcher.exe");
        entry.MatchWindowTitle.Should().Be("Custom Window");
    }

    [Fact]
    public void GameMatcher_MatchesCustomGame_WithMultipleExecutables()
    {
        // Arrange
        var matcher = new GameMatcher();
        var customGames = new List<CustomGameEntry>
        {
            new CustomGameEntry
            {
                Name = "Custom Test Game",
                AppId = "99999",
                Launcher = "Custom",
                Executables = new List<string> { "game_test.exe", "launcher_test.exe" }
            }
        };

        var gameDb = new Dictionary<string, KnownGame>();

        var candidates1 = new List<ProcessInfo>
        {
            new ProcessInfo("game_test", null, "Custom Game Window", DateTime.Now, IntPtr.Zero)
        };

        var candidates2 = new List<ProcessInfo>
        {
            new ProcessInfo("launcher_test", null, "Custom Launcher Window", DateTime.Now, IntPtr.Zero)
        };

        // Act
        var match1 = matcher.Match(candidates1, gameDb, customGames, isFullscreen: false);
        var match2 = matcher.Match(candidates2, gameDb, customGames, isFullscreen: false);

        // Assert
        match1.Should().NotBeNull();
        match1!.Name.Should().Be("Custom Test Game");
        match1.AppId.Should().Be("99999");

        match2.Should().NotBeNull();
        match2!.Name.Should().Be("Custom Test Game");
        match2.AppId.Should().Be("99999");
    }

    [Fact]
    public void LauncherScanner_FiltersOutIgnoredExecutables()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GameDetect_FilterTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var mappingsPath = Path.Combine(tempDir, "known_game_mappings.json");
            var ignoredPath = Path.Combine(tempDir, "ignored_executables.json");

            var settings = new DetectionSettings
            {
                KnownGameMappingsPath = mappingsPath,
                IgnoredExecutablesPath = ignoredPath
            };

            var scanner = new LauncherScanner(Microsoft.Extensions.Logging.Abstractions.NullLogger<LauncherScanner>.Instance, Microsoft.Extensions.Options.Options.Create(settings));
            var addGameMethod = typeof(LauncherScanner).GetMethod("AddGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var db = new Dictionary<string, KnownGame>();
            var mappings = new KnownGameMappingsDatabase
            {
                Games = new List<KnownGameMapping>
                {
                    new KnownGameMapping
                    {
                        Name = "Test Game",
                        AppId = "11111",
                        Launcher = "Steam",
                        Executables = new List<string> { "game_normal.exe", "game_ignored.exe" }
                    }
                }
            };
            
            var ignoredExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "game_ignored.exe" };

            var gameFolder = Path.Combine(tempDir, "TestGame");
            Directory.CreateDirectory(gameFolder);
            File.WriteAllText(Path.Combine(gameFolder, "game_normal.exe"), "");
            File.WriteAllText(Path.Combine(gameFolder, "game_ignored.exe"), "");

            // Act
            addGameMethod!.Invoke(scanner, new object[] { db, "Test Game", gameFolder, "Steam", "11111", mappings, ignoredExes });

            // Assert
            db.Should().ContainKey("game_normal.exe");
            db.Should().NotContainKey("game_ignored.exe");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IgnoredGamesDatabase_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "ignored_games": [
                "Wallpaper Engine",
                "Spam Game"
            ]
        }
        """;

        // Act
        var db = JsonSerializer.Deserialize<IgnoredGamesDatabase>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        db.Should().NotBeNull();
        db!.IgnoredGames.Should().ContainInOrder("Wallpaper Engine", "Spam Game");
    }
}
