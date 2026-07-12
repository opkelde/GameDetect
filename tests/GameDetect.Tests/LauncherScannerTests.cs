using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using GameDetect.Configuration;
using GameDetect.Detection;
using GameDetect.Detection.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameDetect.Tests;

public class LauncherScannerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _mappingsPath;
    private readonly string _ignoredPath;
    private readonly DetectionSettings _settings;

    public LauncherScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GameDetect_Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        _mappingsPath = Path.Combine(_tempDir, "known_game_mappings.json");
        _ignoredPath = Path.Combine(_tempDir, "ignored_executables.json");

        _settings = new DetectionSettings
        {
            KnownGameMappingsPath = _mappingsPath,
            IgnoredExecutablesPath = _ignoredPath
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void EnsureDefaultConfigFiles_CreatesFiles_WhenNotExist()
    {
        // Arrange
        var scanner = new LauncherScanner(NullLogger<LauncherScanner>.Instance, Options.Create(_settings));
        var ensureMethod = typeof(LauncherScanner).GetMethod("EnsureDefaultConfigFiles", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        ensureMethod!.Invoke(scanner, null);

        // Assert
        File.Exists(_mappingsPath).Should().BeTrue();
        File.Exists(_ignoredPath).Should().BeTrue();

        var mappingsJson = File.ReadAllText(_mappingsPath);
        var mappingsDb = JsonSerializer.Deserialize<KnownGameMappingsDatabase>(mappingsJson);
        mappingsDb.Should().NotBeNull();
        mappingsDb!.Games.Should().NotBeEmpty();

        var ignoredJson = File.ReadAllText(_ignoredPath);
        var ignoredDb = JsonSerializer.Deserialize<IgnoredExecutablesDatabase>(ignoredJson);
        ignoredDb.Should().NotBeNull();
        ignoredDb!.IgnoredExecutables.Should().NotBeEmpty();
    }

    [Fact]
    public void ScanDirectoriesForExecutables_FindsExesAndExcludesFolders()
    {
        // Arrange
        var scanner = new LauncherScanner(NullLogger<LauncherScanner>.Instance, Options.Create(_settings));
        var scanMethod = typeof(LauncherScanner).GetMethod("ScanDirectoriesForExecutables", BindingFlags.NonPublic | BindingFlags.Instance);

        // Set up directory structure
        // Root: game1.exe
        // Sub1 (bin): game2.exe
        // Sub2 (content): game3.exe (ignored directory)
        // Sub3 (bin/win64): game4.exe (depth 2)
        // Sub4 (bin/win64/shipping/deep): game5.exe (depth 4)
        // Sub5 (bin/win64/shipping/deep/deeper): game6.exe (depth 5 - out of range)
        
        var binDir = Path.Combine(_tempDir, "bin");
        var contentDir = Path.Combine(_tempDir, "content");
        var win64Dir = Path.Combine(binDir, "win64");
        var deepDir = Path.Combine(win64Dir, "shipping", "deep");
        var deeperDir = Path.Combine(deepDir, "deeper");
 
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(contentDir);
        Directory.CreateDirectory(win64Dir);
        Directory.CreateDirectory(deepDir);
        Directory.CreateDirectory(deeperDir);
 
        File.WriteAllText(Path.Combine(_tempDir, "game1.exe"), "");
        File.WriteAllText(Path.Combine(binDir, "game2.exe"), "");
        File.WriteAllText(Path.Combine(contentDir, "game3.exe"), "");
        File.WriteAllText(Path.Combine(win64Dir, "game4.exe"), "");
        File.WriteAllText(Path.Combine(deepDir, "game5.exe"), "");
        File.WriteAllText(Path.Combine(deeperDir, "game6.exe"), "");
 
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 
        // Act
        scanMethod!.Invoke(scanner, new object[] { _tempDir, 0, 4, result });
 
        // Assert
        result.Should().Contain("game1.exe");
        result.Should().Contain("game2.exe");
        result.Should().NotContain("game3.exe"); // content folder is ignored
        result.Should().Contain("game4.exe");
        result.Should().Contain("game5.exe"); // within depth 4
        result.Should().NotContain("game6.exe"); // beyond depth 4
    }

    [Fact]
    public void AddGame_RegistersMultiExes_ExcludesBlacklist_AppliesMappings()
    {
        // Arrange
        var scanner = new LauncherScanner(NullLogger<LauncherScanner>.Instance, Options.Create(_settings));
        var addGameMethod = typeof(LauncherScanner).GetMethod("AddGame", BindingFlags.NonPublic | BindingFlags.Instance);

        // Create a fake game directory
        var gameDir = Path.Combine(_tempDir, "TestGame");
        var binDir = Path.Combine(gameDir, "bin");
        Directory.CreateDirectory(binDir);

        File.WriteAllText(Path.Combine(gameDir, "launcher.exe"), "");
        File.WriteAllText(Path.Combine(binDir, "game_shipping.exe"), "");
        File.WriteAllText(Path.Combine(binDir, "crashpad_handler.exe"), ""); // blacklist target

        var db = new Dictionary<string, KnownGame>(StringComparer.OrdinalIgnoreCase);
        
        var mappings = new KnownGameMappingsDatabase
        {
            Games = new List<KnownGameMapping>
            {
                new KnownGameMapping
                {
                    Name = "Test Game",
                    AppId = "999",
                    Launcher = "Steam",
                    Executables = new List<string> { "mapped_extra.exe" }
                }
            }
        };

        var ignoredExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "crashpad_handler.exe"
        };

        // Act
        addGameMethod!.Invoke(scanner, new object[] { db, "Test Game", gameDir, "Steam", "999", mappings, ignoredExes });

        // Assert
        // We should have mapped launcher.exe, game_shipping.exe, and mapped_extra.exe to the game.
        // crashpad_handler.exe should be excluded.
        db.Keys.Should().Contain("launcher.exe");
        db.Keys.Should().Contain("game_shipping.exe");
        db.Keys.Should().Contain("mapped_extra.exe");
        db.Keys.Should().NotContain("crashpad_handler.exe");

        // They should all point to the same KnownGame object
        db["launcher.exe"].Name.Should().Be("Test Game");
        db["game_shipping.exe"].Name.Should().Be("Test Game");
        db["mapped_extra.exe"].Name.Should().Be("Test Game");
    }

    [Fact]
    public void ScanAll_CreatesDetectedGamesJson_WithUniqueSortedGames()
    {
        // Arrange
        var scanner = new LauncherScanner(NullLogger<LauncherScanner>.Instance, Options.Create(_settings));

        // Act
        scanner.ScanAll();

        // Assert
        var detectedGamesPath = Path.Combine(Path.GetDirectoryName(_mappingsPath) ?? string.Empty, "detected_games.json");
        File.Exists(detectedGamesPath).Should().BeTrue();

        var json = File.ReadAllText(detectedGamesPath);
        var games = JsonSerializer.Deserialize<List<KnownGame>>(json);
        games.Should().NotBeNull();
        
        var sortedNames = games!.Select(g => g.Name).ToList();
        sortedNames.Should().BeInAscendingOrder();
        sortedNames.Distinct().Count().Should().Be(sortedNames.Count);
    }
}
