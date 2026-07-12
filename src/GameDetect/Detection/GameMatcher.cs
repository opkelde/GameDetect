using GameDetect.Detection.Models;
using GameDetect.Native;

namespace GameDetect.Detection;

public class GameMatcher : IGameMatcher
{
    public DetectedGame? Match(
        List<ProcessInfo> candidates, 
        IReadOnlyDictionary<string, KnownGame> gameDb, 
        List<CustomGameEntry> customGames,
        bool isFullscreen)
    {
        var matchedGames = new List<(ProcessInfo Proc, string Name, string Launcher, string? AppId)>();

        foreach (var proc in candidates)
        {
            var exeName = proc.ProcessName + ".exe";

            // 1. Check Custom Games first (overrides)
            var customMatch = customGames.FirstOrDefault(c => 
                c.Executables != null && c.Executables.Any(exe => string.Equals(exe, exeName, StringComparison.OrdinalIgnoreCase)));

            if (customMatch != null)
            {
                if (!string.IsNullOrWhiteSpace(customMatch.MatchWindowTitle))
                {
                    if (proc.WindowTitle != null && proc.WindowTitle.Contains(customMatch.MatchWindowTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedGames.Add((proc, customMatch.Name, customMatch.Launcher, customMatch.AppId));
                        continue;
                    }
                }
                else
                {
                    matchedGames.Add((proc, customMatch.Name, customMatch.Launcher, customMatch.AppId));
                    continue;
                }
            }

            // 2. Check Auto-Detected Database
            if (gameDb.TryGetValue(exeName, out var knownGame))
            {
                matchedGames.Add((proc, knownGame.Name, knownGame.Launcher, knownGame.AppId));
            }
        }

        if (matchedGames.Count == 0) return null;

        if (matchedGames.Count == 1)
        {
            var m = matchedGames[0];
            return new DetectedGame(m.Name, m.Launcher, m.AppId, m.Proc.ProcessName, isFullscreen, m.Proc.StartTime);
        }

        // Priority resolution: Active window wins, otherwise latest StartTime
        var foregroundHwnd = NativeMethods.GetForegroundWindow();
        var foregroundMatch = matchedGames.FirstOrDefault(m => m.Proc.WindowHandle == foregroundHwnd);
        
        if (foregroundMatch.Proc != null)
        {
            return new DetectedGame(foregroundMatch.Name, foregroundMatch.Launcher, foregroundMatch.AppId, foregroundMatch.Proc.ProcessName, isFullscreen, foregroundMatch.Proc.StartTime);
        }

        var latestMatch = matchedGames.OrderByDescending(m => m.Proc.StartTime).First();
        return new DetectedGame(latestMatch.Name, latestMatch.Launcher, latestMatch.AppId, latestMatch.Proc.ProcessName, isFullscreen, latestMatch.Proc.StartTime);
    }
}

