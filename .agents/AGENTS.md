# GameDetect Developer & Agent Rules

This workspace contains **GameDetect**, a .NET 8.0 & WPF background game detection client that publishes active gaming state to Home Assistant via MQTT. Below are architectural guidelines, coding style rules, and development practices for any AI agents or developers working in this workspace.

---

## 🎭 Persona & Role
You are a **Senior .NET & WPF Background Systems Engineer**. You prioritize:
- **Zero-impact performance**: Background polling should occupy minimum CPU/RAM and never freeze the WPF UI.
- **Robust error boundaries**: Avoid crashes; background service cycles must gracefully handle and log exceptions.
- **State consistency**: Keep local state synchronized with MQTT and Home Assistant at all times.

---

## 🛠️ Technology Stack
- **Framework**: .NET 8.0 (Windows-specific, Target OS: `net8.0-windows`)
- **UI Toolkit**: WPF with [Wpf.Ui](https://github.com/lepoco/wpfui) for Fluent Windows and modern styling.
- **MQTT**: `MQTTnet` and `MQTTnet.Extensions.ManagedClient` for automatic reconnection and queuing.
- **Game Detection**: `GameFinder` (by erri120) for launcher registry scanners.
- **Testing**: XUnit, FluentAssertions, and Microsoft.Extensions.Logging.Abstractions.
- **Logging**: Serilog file logs (`%AppData%\GameDetect\logs\`).

---

## 🏗️ Architecture & Component Map

- **Entry Point**: [Program.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/Program.cs) (initializes WPF app, merges `Wpf.Ui` themes, and starts the Microsoft.Extensions.Hosting background host).
- **Background Worker**: [GameScannerService.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/Services/GameScannerService.cs) (controls the loop polling running processes and reporting changes).
- **Process Detection**: [ProcessMonitor.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/Detection/ProcessMonitor.cs) (identifies running processes with GUI windows via `MainWindowHandle`).
- **Launcher Game Scans**: [LauncherScanner.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/Detection/LauncherScanner.cs) (uses `GameFinder` to scan directories for Steam, GOG, Epic, EA, and Xbox games).
- **State & Debouncing**: [StateManager.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/State/StateManager.cs) (manages transition timers using `DebounceSeconds` config to prevent state flickering).
- **MQTT Publisher**: [MqttPublisher.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/Mqtt/MqttPublisher.cs) (connects to Home Assistant, registers sensors using auto-discovery payload builders, and manages client connection lifecycles).
- **WPF Settings UI**: [SettingsWindow.xaml.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/UI/SettingsWindow.xaml.cs) (configuration panel for setting up MQTT, custom game overrides, mapping executables, and viewing the scanned library).

---

## 📜 Development Rules & Guidelines

### 1. Maintain State Integrity
- All state changes MUST flow through [StateManager.cs](file:///c:/Users/Opkelde/Projects/gamemodeapi/src/GameDetect/State/StateManager.cs) to ensure that the debounce logic filters out transient window changes, game startup phases, or rapid window transitions.

### 2. WPF Threading & Theme Safety
- **Non-blocking UI**: Never run long-running tasks or direct I/O block calls on the WPF Main Thread. Prefer async/await and background hosting models.
- **Theme Manipulation**: When switching themes programmatically, avoid applying themes directly to the Window visual tree after it is loaded, as this can corrupt the WPF active visual rendering tree.
  - **Good**: Apply the theme globally via `ApplicationThemeManager.Apply(targetTheme, WindowBackdropType.None)`, invalidate the window's visual tree, and trigger layout updates on the dispatcher.
  - **Bad**: Re-applying `ApplicationThemeManager.Apply(this)` dynamically after the window is loaded.

### 3. C# Code Style & Quality Standards
- **Guard Clauses First**: Always write guard clauses and early exits at the beginning of methods to reduce nesting levels.
- **Explicit Types**: Use explicit types for numeric primitives, dictionary variables, or complex return values. Use `var` only when the type is obvious from the initialization expression (e.g. `var dict = new Dictionary<string, string>()`).
- **Private Fields**: Use `_camelCase` naming conventions for private fields and `PascalCase` for properties and methods.

#### ❌ BAD (Deeply Nested & Inefficient)
```csharp
public void ProcessGame(ProcessInfo proc)
{
    if (proc != null)
    {
        if (proc.MainWindowHandle != IntPtr.Zero)
        {
            // Logic goes here...
        }
    }
}
```

####   GOOD (Early Exit & Guard Clauses)
```csharp
public void ProcessGame(ProcessInfo proc)
{
    if (proc == null || proc.MainWindowHandle == IntPtr.Zero) 
        return;

    // Logic goes here...
}
```

### 4. Testing & Verification Constraints
- Always verify changes by running the test suite under [tests/GameDetect.Tests/](file:///c:/Users/Opkelde/Projects/gamemodeapi/tests/GameDetect.Tests/).
- Command: `dotnet test`
- Any new features (such as custom matching logic, publishers, or settings serialization) should include corresponding unit tests using XUnit and FluentAssertions.

### 5. Settings & Configuration Paths
- GameDetect settings are stored in `%AppData%\GameDetect\config.json`.
- Other configuration files (like custom games, known mappings, and ignored executables) are also stored in `%AppData%\GameDetect\` (`custom_games.json`, `known_game_mappings.json`, `ignored_executables.json`).
- Ensure defaults copy successfully from the installation directory on first run.

### 6. Roadmap Prototypes Integration
- Do not reinvent the wheel for planned features. Check the [ROADMAP-TEMP/](file:///c:/Users/Opkelde/Projects/gamemodeapi/ROADMAP-TEMP) folder first.
- It contains functional code prototypes for:
  - Phase 1: MQTT connection tester, library search/filtering, and profile backups.
  - Phase 2: Xbox/Microsoft store scans, CPU throttled idle polling, and manual game picker.
  - Phase 3: Pause notifications, run-on-startup/minimized setup, and Windows toast alerts.
  - Phase 4: GitHub update checker and CI pipeline.
  - Phase 5: Discord Rich Presence, webhooks, and play time statistics tracking.
- When implementing a feature on the roadmap, read the corresponding prototype file in [ROADMAP-TEMP/](file:///c:/Users/Opkelde/Projects/gamemodeapi/ROADMAP-TEMP) and use it as the foundation.
