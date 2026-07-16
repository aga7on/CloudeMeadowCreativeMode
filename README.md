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

1. Close Cloud Meadow
2. Extract `BepInEx` folder to game root directory (`Cloud Meadow/`)
3. Launch game
4. Press **F6** to open Creative Editor

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

### v6.0.1
- Bloodline traits properly detected and displayed with [Bloodline] tag
- Trait sorting: Bloodline → Rare → Uncommon → Common → Negative
- Filter button to show only Bloodline traits
- Custom cursor rendered above menu
- Adaptive UI layout (160px sidebar)
- Fixed Gender Swap using native game method
- Fixed quest auto-discovery
- Fixed build script error handling
