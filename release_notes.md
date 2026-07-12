## What's New in v1.3.0

This release introduces enhancements to the settings interface, custom game mappings, stability, and runtime diagnostic visibility.

### Scanned Library Diagnostic Tab
* Exposed the `detected_games.json` diagnostics file directly within the Settings panel.
* Added a read-only **Scanned Library** tab containing a table of all auto-detected games with fields for Game Name, App ID, Launcher, Primary Executable, and Install Path.

### Expanded Custom Games Mapping Options
* Redesigned custom games configurations to match launcher mapping functionality.
* Custom games now support mapping an **App ID**, custom **Launcher** type, and **multiple executables** (entered as comma-separated values in the UI), giving users full control without cluttering the auto-detected library.

### Fully Dynamic & Glitch-Free Theme Customization
* Replaced the separate Light/Dark buttons with a appearance drop-box featuring *Auto (System Default)*, *Light Mode*, and *Dark Mode*, defaulting to Auto mode.
* Moved theme and control style registration to application-level resources, eliminating some UI rendering glitches (such as dark headers persisting in light mode).
* Settings are now automatically saved and persisted in `config.json`.

### Core Stability & Exception Safeguards
* Resolved a startup crash exception caused by calling `SystemThemeWatcher.UnWatch()` prior to the settings window being fully loaded.
* Redirected `wpf_crash.log` output to the writable `%AppData%\GameDetect` folder to prevent write privilege failures from crashing the app when run in the Program Files installation directory.
* Expanded the unit test suite with validation checks for custom game multi-executable matches and configuration deserialization.
