using System.IO;
using MessageBox = System.Windows.MessageBox;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using Microsoft.Win32;
using GameDetect.Configuration;
using Wpf.Ui.Appearance;
using System.Collections.ObjectModel;
using GameDetect.Detection.Models;

using System.Linq;

namespace GameDetect.UI;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameDetect");
    private static readonly string AppSettingsPath = Path.Combine(AppDataDir, "config.json");
    private static readonly string CustomGamesPath = Path.Combine(AppDataDir, "custom_games.json");
    private static readonly string MappingsPath = Path.Combine(AppDataDir, "known_game_mappings.json");
    private static readonly string IgnoredPath = Path.Combine(AppDataDir, "ignored_executables.json");

    public ObservableCollection<CustomGameUIEntry> CustomGames { get; set; } = new();
    public ObservableCollection<GameMappingEntry> GameMappings { get; set; } = new();
    public ObservableCollection<IgnoredExecutableEntry> IgnoredExecutables { get; set; } = new();
    public ObservableCollection<KnownGame> DetectedGames { get; set; } = new();

    public SettingsWindow()
    {
        InitializeComponent();
        ApplicationThemeManager.Apply(this);

        LoadSettings();
        LoadCustomGames();
        LoadMappings();
        LoadIgnoredExecutables();
        LoadDetectedGames();
        
        ChkAutostart.IsChecked = AutostartManager.IsAutostartEnabled();
    }

    private bool _isThemeInitialized = false;

    private void LoadSettings()
    {
        if (!File.Exists(AppSettingsPath))
        {
            _isThemeInitialized = true;
            ApplySelectedTheme("Auto");
            return;
        }

        try
        {
            var json = File.ReadAllText(AppSettingsPath);
            var node = JsonNode.Parse(json);

            if (node != null && node["Mqtt"] != null)
            {
                TxtMqttHost.Text = node["Mqtt"]?["Host"]?.ToString() ?? "";
                TxtMqttPort.Text = node["Mqtt"]?["Port"]?.ToString() ?? "";
                TxtMqttUser.Text = node["Mqtt"]?["Username"]?.ToString() ?? "";
                TxtMqttPass.Password = node["Mqtt"]?["Password"]?.ToString() ?? "";
            }

            if (node != null && node["Service"]?["DeviceName"] != null)
            {
                TxtDeviceName.Text = node["Service"]?["DeviceName"]?.ToString() ?? "";
            }

            string theme = "Auto";
            if (node != null && node["Service"]?["Theme"] != null)
            {
                theme = node["Service"]?["Theme"]?.ToString() ?? "Auto";
            }

            _isThemeInitialized = false;
            if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase))
                CmbTheme.SelectedIndex = 1;
            else if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
                CmbTheme.SelectedIndex = 2;
            else
                CmbTheme.SelectedIndex = 0;

            _isThemeInitialized = true;
            ApplySelectedTheme(theme);
        }
        catch 
        {
            _isThemeInitialized = true;
            ApplySelectedTheme("Auto");
        }
    }

    private void LoadCustomGames()
    {
        if (!File.Exists(CustomGamesPath)) return;
        try
        {
            var json = File.ReadAllText(CustomGamesPath);
            var root = JsonDocument.Parse(json).RootElement;
            if (root.TryGetProperty("games", out var gamesElement))
            {
                var games = JsonSerializer.Deserialize<CustomGameEntry[]>(gamesElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (games != null)
                {
                    foreach (var game in games)
                    {
                        CustomGames.Add(new CustomGameUIEntry
                        {
                            Name = game.Name,
                            AppId = game.AppId ?? "",
                            Launcher = game.Launcher ?? "Custom",
                            ExecutablesString = game.Executables != null ? string.Join(", ", game.Executables) : "",
                            MatchWindowTitle = game.MatchWindowTitle ?? ""
                        });
                    }
                }
            }
        }
        catch { }

        DgCustomGames.ItemsSource = CustomGames;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
            JsonNode node = File.Exists(AppSettingsPath) ? JsonNode.Parse(File.ReadAllText(AppSettingsPath))! : new JsonObject();

            if (node["Mqtt"] == null) node["Mqtt"] = new JsonObject();
            
            var rawHost = TxtMqttHost.Text?.Trim() ?? "";
            if (Uri.TryCreate(rawHost, UriKind.Absolute, out var uri) && 
                (rawHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rawHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                rawHost = uri.Host;
            }
            else
            {
                rawHost = rawHost.TrimEnd('/');
            }
            
            node["Mqtt"]!["Host"] = rawHost;
            node["Mqtt"]!["Port"] = int.TryParse(TxtMqttPort.Text, out var port) ? port : 1883;
            node["Mqtt"]!["Username"] = TxtMqttUser.Text;
            node["Mqtt"]!["Password"] = TxtMqttPass.Password;

            if (node["Service"] == null) node["Service"] = new JsonObject();
            node["Service"]!["DeviceName"] = TxtDeviceName.Text;

            string selectedTheme = "Auto";
            if (CmbTheme.SelectedIndex == 1) selectedTheme = "Light";
            else if (CmbTheme.SelectedIndex == 2) selectedTheme = "Dark";
            node["Service"]!["Theme"] = selectedTheme;

            File.WriteAllText(AppSettingsPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            if (ChkAutostart.IsChecked == true) AutostartManager.EnableAutostart();
            else AutostartManager.DisableAutostart();

            MessageBox.Show("Settings saved successfully! Changes have been applied instantly.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSaveGames_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.GetDirectoryName(CustomGamesPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var dbGames = CustomGames.Select(c => new CustomGameEntry
            {
                Name = c.Name,
                AppId = string.IsNullOrWhiteSpace(c.AppId) ? null : c.AppId.Trim(),
                Launcher = string.IsNullOrWhiteSpace(c.Launcher) ? "Custom" : c.Launcher.Trim(),
                Executables = c.ExecutablesString.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList(),
                MatchWindowTitle = string.IsNullOrWhiteSpace(c.MatchWindowTitle) ? null : c.MatchWindowTitle.Trim()
            }).ToList();

            var root = new { games = dbGames };
            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CustomGamesPath, json);

            MessageBox.Show("Custom games saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save custom games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Zip Archive|*.zip", FileName = "GameDetect_Backup.zip" };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
                BackupManager.BackupConfiguration(dialog.FileName);
                MessageBox.Show("Backup created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    private void BtnRestore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Zip Archive|*.zip" };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                BackupManager.RestoreConfiguration(dialog.FileName);
                LoadSettings();
                CustomGames.Clear();
                LoadCustomGames();
                GameMappings.Clear();
                LoadMappings();
                IgnoredExecutables.Clear();
                LoadIgnoredExecutables();
                MessageBox.Show("Configuration restored successfully! Please restart the service.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Restore failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    private void LoadDetectedGames()
    {
        var detectedPath = Path.Combine(AppDataDir, "detected_games.json");
        if (!File.Exists(detectedPath)) return;
        try
        {
            var json = File.ReadAllText(detectedPath);
            var games = JsonSerializer.Deserialize<KnownGame[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (games != null)
            {
                foreach (var game in games) DetectedGames.Add(game);
            }
        }
        catch { }

        DgScannedLibrary.ItemsSource = DetectedGames;
    }

    private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isThemeInitialized || CmbTheme == null) return;
        
        string selectedTheme = "Auto";
        if (CmbTheme.SelectedIndex == 1) selectedTheme = "Light";
        else if (CmbTheme.SelectedIndex == 2) selectedTheme = "Dark";
        
        ApplySelectedTheme(selectedTheme);
    }

    private void ApplySelectedTheme(string themeStr)
    {
        bool isLoaded = this.IsLoaded;
        if (string.Equals(themeStr, "Light", StringComparison.OrdinalIgnoreCase))
        {
            if (isLoaded)
            {
                try { SystemThemeWatcher.UnWatch(this); } catch {}
            }
            ApplicationThemeManager.Apply(ApplicationTheme.Light, Wpf.Ui.Controls.WindowBackdropType.None);
            ApplicationThemeManager.Apply(this);
        }
        else if (string.Equals(themeStr, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            if (isLoaded)
            {
                try { SystemThemeWatcher.UnWatch(this); } catch {}
            }
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.None);
            ApplicationThemeManager.Apply(this);
        }
        else
        {
            if (isLoaded)
            {
                try { SystemThemeWatcher.UnWatch(this); } catch {}
            }
            ApplicationThemeManager.Apply(ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.None);
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(this);
        }
    }

    private void EnsureConfigFileExists(string targetPath, string fileName)
    {
        if (!File.Exists(targetPath))
        {
            var appDataDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(appDataDir) && !Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, targetPath);
            }
            else
            {
                var emptyJson = fileName.Contains("mappings") ? "{\"games\":[]}" : "{\"ignored_executables\":[]}";
                File.WriteAllText(targetPath, emptyJson);
            }
        }
    }

    private void LoadMappings()
    {
        EnsureConfigFileExists(MappingsPath, "known_game_mappings.json");

        try
        {
            var json = File.ReadAllText(MappingsPath);
            var root = JsonDocument.Parse(json).RootElement;
            if (root.TryGetProperty("games", out var gamesElement))
            {
                var games = JsonSerializer.Deserialize<KnownGameMapping[]>(gamesElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (games != null)
                {
                    foreach (var game in games)
                    {
                        GameMappings.Add(new GameMappingEntry
                        {
                            Name = game.Name,
                            AppId = game.AppId ?? "",
                            Launcher = game.Launcher ?? "Steam",
                            ExecutablesString = string.Join(", ", game.Executables)
                        });
                    }
                }
            }
        }
        catch { }

        DgGameMappings.ItemsSource = GameMappings;
    }

    private void LoadIgnoredExecutables()
    {
        EnsureConfigFileExists(IgnoredPath, "ignored_executables.json");

        try
        {
            var json = File.ReadAllText(IgnoredPath);
            var root = JsonDocument.Parse(json).RootElement;
            if (root.TryGetProperty("ignored_executables", out var ignoredElement))
            {
                var ignored = JsonSerializer.Deserialize<string[]>(ignoredElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (ignored != null)
                {
                    foreach (var exe in ignored)
                    {
                        IgnoredExecutables.Add(new IgnoredExecutableEntry { Executable = exe });
                    }
                }
            }
        }
        catch { }

        DgIgnoredList.ItemsSource = IgnoredExecutables;
    }

    private void BtnSaveMappings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);

            var gamesList = new List<KnownGameMapping>();
            foreach (var entry in GameMappings)
            {
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;

                var exes = entry.ExecutablesString
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                gamesList.Add(new KnownGameMapping
                {
                    Name = entry.Name,
                    AppId = string.IsNullOrWhiteSpace(entry.AppId) ? null : entry.AppId.Trim(),
                    Launcher = string.IsNullOrWhiteSpace(entry.Launcher) ? null : entry.Launcher.Trim(),
                    Executables = exes
                });
            }

            var db = new KnownGameMappingsDatabase { Games = gamesList };
            var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MappingsPath, json);

            MessageBox.Show("Game mappings saved successfully! They will take effect on next scan.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save game mappings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSaveIgnored_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);

            var ignoredList = IgnoredExecutables
                .Where(entry => !string.IsNullOrEmpty(entry.Executable))
                .Select(entry => entry.Executable!.Trim())
                .ToList();

            var db = new IgnoredExecutablesDatabase { IgnoredExecutables = ignoredList };
            var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(IgnoredPath, json);

            MessageBox.Show("Ignored executables list saved successfully! It will take effect on next scan.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save ignored list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class GameMappingEntry
{
    public string Name { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string Launcher { get; set; } = "Steam";
    public string ExecutablesString { get; set; } = string.Empty;
}

public class IgnoredExecutableEntry
{
    public string Executable { get; set; } = string.Empty;
}

public class CustomGameUIEntry
{
    public string Name { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string Launcher { get; set; } = "Custom";
    public string ExecutablesString { get; set; } = string.Empty;
    public string MatchWindowTitle { get; set; } = string.Empty;
}

