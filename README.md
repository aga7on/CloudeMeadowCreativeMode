# CloudMeadow.CreativeMode v6.0.1

BepInEx plugin for Cloud Meadow - Universal Creative Editor.

## Features

### Player Editor
- Name, level, gender, pronouns
- Primary stats (Physique, Stamina, Intuition, Swiftness)
- HP/XP management
- Genodriver forms and abilities
- Equipment management

### Monster Editor
- Species, gender, level, loyalty
- Pigment/pattern selection
- Traits with [Bloodline] tags
- Chimera variants
- Family tree and parentage
- Job assignments

### Inventory
- Universal container detection
- Item quality and quantity editing
- Multi-select operations
- Quest-item safety checks

### Farm Editor
- Building upgrades
- Crop management (water/grow/harvest)
- Work assignments
- Farm integrity audit

### Quest Editor
- Auto-discovery of new quests
- Safe jump and force complete
- Quest diagnostics and repair

### World Editor
- Migrations and calendar events
- Weather and season control
- Dungeon persistent state

### Combat Editor
- God mode and instant win
- Unit healing and status management
- Cooldown resets

### Technical
- Save backup system
- Transaction/Undo support
- Compatibility scanner
- Error center with diagnostics

## Installation

### Players (prebuilt mod)

1. Close Cloud Meadow.
2. Download **BepInEx 5 x64** and extract the **contents** of its archive into the game folder — the folder containing `Cloud Meadow.exe`. After this step that folder must contain `BepInEx/`, `winhttp.dll`, and `doorstop_config.ini`.
3. Extract the release ZIP into the game folder, or copy only `CloudMeadow.CreativeMode.dll` into `Cloud Meadow/BepInEx/plugins/`.
4. Start the game normally through Steam and press **F6** after a save has loaded.

Do not place the DLL in `Cloud Meadow_Data`, and do not copy source `.cs` files into `BepInEx/plugins`.

### Developers (this source archive)

Extract the `CloudeMeadowCreativeMode-main` folder directly into the game folder, so these two paths exist side by side:

```text
Cloud Meadow/Cloud Meadow_Data/
Cloud Meadow/CloudeMeadowCreativeMode-main/
```

Install BepInEx as above, then run `./build-release.ps1` from the source folder. Copy `bin/CloudMeadow.CreativeMode.dll` to `BepInEx/plugins/` before launching the game.

## Hotkeys

- **F6** - Toggle Creative Editor
- **F7** - Unlock Gallery
- **F8** - Refresh scan

## Build

```powershell
.\build-release.ps1
```

## Notes

- Targets Unity Mono / .NET 3.5
- References resolved against local game install
- `bin/`, `obj/`, `.vs/`, logs excluded from repo

## Changelog

### v6.0.2
- Stopped automatic ability-state repair from rewriting ability slots during UI refresh or game load; the guarded repair is now a manual action.
- Fixed stat-point editing when the `statPoints` backing field is declared on a base type.
- The stat editor now reports the game-enforced range when a requested value is capped.
- Expanded trait discovery to include singleton and separate species-trait definitions, including special traits such as Nine Lives.

### v6.0.1
- Bloodline traits properly detected and displayed with [Bloodline] tag
- Trait sorting: Bloodline → Rare → Uncommon → Common → Negative
- Filter button to show only Bloodline traits
- Custom cursor rendered above menu
- Adaptive UI layout (160px sidebar)
- Fixed Gender Swap using native game method
- Fixed quest auto-discovery
- Fixed build script error handling
