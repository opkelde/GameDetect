# GameDetect Roadmap

This document outlines the planned improvements, architectural updates, and feature milestones for GameDetect.

> [!NOTE]
> Functional code prototypes for many of these features are prepared inside the untracked folder `ROADMAP-TEMP/` for rapid future development.

---

## 🗺️ Release Phases

### 🔹 Phase 1: UX Polish & Connectivity (v1.5.0)
Focuses on enhancing the Settings interface with immediate configuration feedback and better navigation helper tools.

- [ ] **MQTT Connection Test**: Add a "Test Connection" button on the General Settings tab to validate MQTT credentials and host accessibility in real-time, displaying a success/error message.
- [ ] **Library Search & Filtering**: Add a search box and launcher filter (Steam, EA, etc.) to the **Scanned Library** tab to easily manage large collections.
- [ ] **Export/Import Profiles**: Support exporting and importing custom game profiles and ignore lists to facilitate settings migrations.

---

### 🔹 Phase 2: Core Matching Engine & Launchers (v1.6.0)
Expands support for more game store platforms and improves matching accuracy.

- [ ] **Expanded Store Handlers**: Integrate and test support for Xbox App/Microsoft Store, Ubisoft Connect, and Battle.net launchers.
- [ ] **Resource Optimization**: Refactor process scanning polling to adjust frequency dynamically based on system idle state.
- [ ] **Manual Game Association**: Allow users to browse and associate custom launchers or paths directly from the UI rather than manual typing.

---

### 🔹 Phase 3: Tray Actions & System Options (v1.7.0)
Increases control over the background daemon directly from the Windows notification area.

- [ ] **Pause Detection**: Add a context menu option to the system tray to pause game detection (e.g., "Pause for 1 hour", "Pause until restart").
- [ ] **Launch settings on Startup option**: Ensure the Settings panel can run minimized to tray on Windows startup.
- [ ] **Game State Notifications**: Optional Windows toast notifications when a game is successfully matched or unmatched.

---

### 🔹 Phase 4: DevOps & CI/CD Automation (v2.0.0)
Implements testing pipelines and automated software updates.

- [ ] **GitHub Actions Integration**: Set up build and test automation on commit pushes.
- [ ] **Auto-Update Notifications**: Alert the user within the Settings Window when a new GitHub release is available.
- [ ] **Universal Linux Support (Proton/WINE)**: Explore head-less runner support for Steam Deck (SteamOS) and Linux systems.
