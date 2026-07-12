using GameDetect.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GameDetect;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameDetect");
        if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);
        builder.Configuration.AddJsonFile(Path.Combine(appDataFolder, "config.json"), optional: true, reloadOnChange: true);

        // Windows Service support
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "GameDetect";
        });

        // Serilog
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.File("logs/GameDetect-.log", rollingInterval: RollingInterval.Day));

        // Configuration binding
        builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("Mqtt"));
        builder.Services.Configure<DetectionSettings>(builder.Configuration.GetSection("Detection"));
        builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection("Service"));

        // Services (DI registration)
        builder.Services.AddSingleton<GameDetect.Detection.ILauncherScanner, GameDetect.Detection.LauncherScanner>();
        builder.Services.AddSingleton<GameDetect.Detection.IProcessMonitor, GameDetect.Detection.ProcessMonitor>();
        builder.Services.AddSingleton<GameDetect.Detection.IFullscreenDetector, GameDetect.Detection.FullscreenDetector>();
        builder.Services.AddSingleton<GameDetect.Detection.IGameMatcher, GameDetect.Detection.GameMatcher>();
        builder.Services.AddSingleton<GameDetect.Detection.ICustomGamesLoader, GameDetect.Detection.CustomGamesLoader>();
        builder.Services.AddSingleton<GameDetect.State.IStateManager, GameDetect.State.StateManager>();
        builder.Services.AddSingleton<GameDetect.Mqtt.IMqttPublisher, GameDetect.Mqtt.MqttPublisher>();
        builder.Services.AddHostedService<GameDetect.Detection.GameScannerService>();

        var host = builder.Build();

        // Start background host
        _ = host.StartAsync();

        // Start WPF Application
        var app = new System.Windows.Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        app.Properties["Host"] = host;

        // Determine initial theme on start (Auto by default)
        var initialTheme = "Auto";
        try
        {
            var configPath = Path.Combine(appDataFolder, "config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json);
                initialTheme = node?["Service"]?["Theme"]?.ToString() ?? "Auto";
            }
        }
        catch {}

        var themeDict = new Wpf.Ui.Markup.ThemesDictionary();
        if (string.Equals(initialTheme, "Light", StringComparison.OrdinalIgnoreCase))
        {
            themeDict.Theme = Wpf.Ui.Appearance.ApplicationTheme.Light;
        }
        else if (string.Equals(initialTheme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            themeDict.Theme = Wpf.Ui.Appearance.ApplicationTheme.Dark;
        }
        else
        {
            // Auto - match system theme
            var systemTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetSystemTheme();
            themeDict.Theme = systemTheme == Wpf.Ui.Appearance.SystemTheme.Light ? Wpf.Ui.Appearance.ApplicationTheme.Light : Wpf.Ui.Appearance.ApplicationTheme.Dark;
        }

        app.Resources.MergedDictionaries.Add(themeDict);
        app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
        
        GameDetect.UI.TrayIconManager? trayIcon = null;
        
        app.DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                var crashPath = Path.Combine(appDataFolder, "wpf_crash.log");
                File.WriteAllText(crashPath, e.Exception.ToString());
            }
            catch {}
            e.Handled = true;
        };

        app.Startup += (s, e) => 
        {
            try
            {
                trayIcon = new GameDetect.UI.TrayIconManager();
                
                var mqttHost = builder.Configuration.GetSection("Mqtt")["Host"];
                if (string.IsNullOrEmpty(mqttHost) || mqttHost == "homeassistant.local")
                {
                    new GameDetect.UI.SettingsWindow().Show();
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("wpf_startup_crash.log", ex.ToString());
            }
        };

        app.Exit += async (s, e) => 
        {
            trayIcon?.Dispose();
            await host.StopAsync();
            host.Dispose();
        };

        app.Run();
    }
}

