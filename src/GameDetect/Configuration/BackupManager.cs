using System.IO;
using System.IO.Compression;

namespace GameDetect.Configuration;

public static class BackupManager
{
    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameDetect");
    private static readonly string[] FilesToBackup = {
        "config.json",
        "custom_games.json",
        "known_game_mappings.json",
        "ignored_executables.json"
    };

    public static void BackupConfiguration(string zipPath)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var file in FilesToBackup)
        {
            var fullPath = Path.Combine(AppDataDir, file);
            if (File.Exists(fullPath))
            {
                archive.CreateEntryFromFile(fullPath, file, CompressionLevel.Optimal);
            }
        }
    }

    public static void RestoreConfiguration(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);

        foreach (var entry in archive.Entries)
        {
            var destPath = Path.Combine(AppDataDir, entry.FullName);
            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            entry.ExtractToFile(destPath, overwrite: true);
        }
    }
}

