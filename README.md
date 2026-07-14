# 🎮 GameDetect

A standalone Windows desktop companion application that detects running games on your PC and publishes your active game status to [Home Assistant](https://www.home-assistant.io) via MQTT.

## What It Does

GameDetect quietly monitors your Windows PC in the background for running games and automatically reports two entities to Home Assistant:

- **Game Mode** (`binary_sensor`) — `ON` when any game is detected, `OFF` otherwise.
- **Active Game** (`sensor`) — The name of the currently running game, with extended attributes like the launcher used, fullscreen status, and session start time.

## Use Cases

- 🎮 **Gaming Mode Automations** — Automatically dim the lights, switch your room to a gaming scene, or mute notifications the moment you start playing.
- 📊 **Game Session Tracking** — Track exactly which games you play and build a history of your gaming sessions in Home Assistant.
- 🔔 **Smart Notifications** — Prevent text-to-speech announcements or push notifications from firing while you're in an intense gaming session.
- 💡 **RGB Sync** — Trigger custom WLED lighting effects based on whether you're actively gaming.

## How It Works

GameDetect uses multiple passive detection methods for reliable game identification:

1. **Launcher Scanning** — Automatically discovers installed games from Steam, GOG, Epic Games Store, EA Desktop, and Xbox Game Pass.
2. **Process Monitoring** — Polls running processes against a known game database.
3. **Fullscreen Detection** — Uses Windows APIs to detect D3D fullscreen applications.
4. **Custom Game List** — Allows you to define custom games directly in the UI for standalone or non-launcher titles.

---

## 🛠 Prerequisites: Setting up MQTT on Home Assistant

To use GameDetect, your Home Assistant instance must have an MQTT broker running. The easiest way is to use the official **Mosquitto broker** add-on.

If you don't have MQTT set up yet, follow these quick steps:

1. Open your Home Assistant dashboard and navigate to **Settings** > **Add-ons**.
2. Click the **Add-on Store** button in the bottom right corner.
3. Search for **"Mosquitto broker"**, select the official add-on, and click **Install**.
4. Once installed, go to the **Info** tab, toggle on **Start on boot** and **Watchdog**, then click **Start**.
5. MQTT needs a user. Create one via settings > People > Users > + Add User
  name the user something like mqtt_user. It is recommended to set the user to local only for better security. 
  Navigate to **Settings** > **Devices & services**. Home Assistant should automatically pop up a "New device discovered" notification for MQTT. 
  Click **Configure**, enter the user details you made earlier and then **Submit**.



---

## 🚀 Quick Start Guide

Setting up GameDetect on your gaming PC is simple:

1. **Download & Install**: Download the latest release of `GameDetect_Setup.exe` from the releases page and run the installer.
2. **Open Settings**: Once installed, run GameDetect. Double-click the tray controller icon in your Windows System Tray (bottom right corner) to open the modern Settings panel.
3. **Configure MQTT**: 
   - **Host**: Enter the IP address of your Home Assistant instance (e.g., `192.168.1.100` or `homeassistant.local`). Do not include `http://` or ports.
   - **Username / Password**: Enter the credentials for your MQTT broker.
4. **Device Name**: Customize the name of your PC (e.g., `Gaming PC`). This determines how it appears in Home Assistant.
5. **Autostart**: Check the "Start with Windows" box if you want GameDetect to run automatically in the background on boot.
6. **Save**: Click **Save Settings**. GameDetect will instantly connect to Home Assistant!

Your Game Mode entities will automatically appear in Home Assistant under the MQTT integration as a new device!

---

## 🔧 UI & Advanced Configuration

GameDetect provides advanced configuration tabs in the Settings panel:

### 1. Custom Games
If you play standalone games or emulators that aren't managed by standard launchers:
1. Navigate to the **Custom Games** section.
2. Add the exact executable name (e.g., `minecraft.exe`), a Display Name, and optionally a window title match condition.
3. Click **Save Games**.

### 2. Game Mappings
If a launcher game is tracked using the wrong executable (e.g., tracking a launcher process instead of the main game client):
1. Navigate to the **Game Mappings** section.
2. Enter the game name, launcher type, App ID (if applicable), and a comma-separated list of executable files (e.g., `DeltaForceClient-Win64-Shipping.exe, DeltaForceLauncher.exe`).
3. Click **Save Mappings**.

### 3. Scanned Library & Ignoring
- **Scanned Library**: Displays all automatically detected games found on your system.
- **Ignore / Track**: You can exclude any detected game from tracking by clicking **Ignore**.
- **Ignored List (Split Layout)**:
  - **Blacklisted Executables** (left grid): Filter out generic helper executables (e.g., crash handlers or overlays like `crashpad_handler.exe`) globally so they are never mapped to any game.
  - **Ignored Games** (right grid): Completely exclude specific games by name from triggering detection (e.g., ignoring *Wallpaper Engine*).
- **Restart Service**: Clicking the "Restart Service" button in the bottom right of the Scanned Library tab will restart the background service in the tray and reload all settings.

### 4. Maintenance
- **Configuration Backup**: Backup and restore your configurations to/from ZIP files.
- **Theme Selection**: Customize the interface with Light Mode, Dark Mode, or Auto (System Default).

---

## ⚙️ Advanced: Configuration Data

GameDetect stores all settings and databases in your Windows AppData folder:
`%AppData%\GameDetect\`

This folder contains:
- `config.json` (General service & MQTT settings)
- `custom_games.json` (Configured custom games)
- `known_game_mappings.json` (Custom game executable overrides)
- `ignored_executables.json` (Blacklisted processes)
- `ignored_games.json` (Ignored game names)

---

## License

This project is licensed under the GPL-3.0 License — see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [HASS.Agent](https://github.com/hass-agent/HASS.Agent) — The Windows companion app for Home Assistant that originally inspired this project.
- [GameFinder](https://github.com/erri120/GameFinder) — Game installation detection library.
- [MQTTnet](https://github.com/dotnet/MQTTnet) — The MQTT library used for seamless broker communication.
- [WPF-UI](https://github.com/lepoco/wpfui) — The library powering the beautiful modern interface.

