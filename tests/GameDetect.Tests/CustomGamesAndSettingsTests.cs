using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
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
}
