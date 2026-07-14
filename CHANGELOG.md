# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.2] - 2026-07-14

### Added
- Name-based game ignoring via `ignored_games.json` to allow users to completely exclude specific games from triggering detection (e.g., ignoring *Wallpaper Engine*), preventing scanner fallback to helper executables.
- Split layout in the **Ignored List** tab separating **Blacklisted Executables** (generic background helpers like crash handlers) and **Ignored Games** (specific games).
- "Restart Service" button in the bottom-right corner of the **Scanned Library** tab that restarts the tray application via a new `--settings` command-line argument.
- Support for a `--settings` command-line startup argument to directly open the settings window.

## [1.4.1] - 2026-07-12

### Added
- Expanded launcher scanner depth to 4 to find deeply nested game executables.
- Added mapping database override for *Delta Force* (Steam AppID `2507950`) to map the launcher and main client.

### Fixed
- Fixed Custom Games grid UI binding bug where the grid was disabled if `custom_games.json` did not exist on first launch.

## [1.4.0] - 2026-07-10

### Added
- Scanned library ignore/track action button toggles.
- Settings UI theme switching fixes.

## [1.3.0] - 2026-07-08

### Added
- Scanned Library settings page displaying auto-detected games.
- App ID support for Custom Games.
- Auto theme mode (Light/Dark synchronized with Windows system settings).

### Fixed
- Fixed transition UI rendering glitches.
- Fixed startup crash exceptions when loading malformed configuration structures.

## [1.2.0] - 2026-06-30

### Added
- Exporter utility to save scanned library results to `detected_games.json` diagnostic database.

## [1.1.1] - 2026-06-20

### Changed
- Moved hardcoded game mappings database and ignored blacklist arrays to external JSON source files (`known_game_mappings.json`, `ignored_executables.json`).
- Expanded standard known game mappings database to support 120 games.

## [1.1.0] - 2026-06-15

### Added
- Support for scanning and matching multiple executables per game.
- User-editable mappings and executable blacklist configuration files.
- Visual settings tabs in the Settings WPF UI.

## [1.0.0] - 2026-06-01

### Added
- First public standalone version of GameDetect (formerly GameModeAPI).
- Background polling, fullscreen D3D application detection, and automated game scanner (Steam, GOG, Epic, EA Desktop, Xbox).
- Direct MQTT integration with Home Assistant (auto-discovery for Game Mode binary sensor and Active Game sensor).
- System tray context menu control, autostart manager, settings backups, and custom games UI.
