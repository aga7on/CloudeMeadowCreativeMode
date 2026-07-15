# Cloud Meadow Creative Mode

Universal in-game Creative Editor for **Cloud Meadow Beta 0.2.6.1**. Built as a BepInEx 5 plugin for the Unity Mono version of the game.

## Features

- Native adaptive uGUI editor opened with **F6**.
- Player, party, monster, genealogy, trait and pigment editors.
- Inventory/container search, categories, batch selection and item inspector.
- Egg, incubator, farm, crop and building tools.
- Canonical quest list with status filters, search, dependency inspector, Safe Jump, Repair and restricted restart operations.
- World, calendar, migration, dungeon, combat, gallery and relationship tools.
- Movement cheats, presets and resource tools.
- Transaction history and Undo for scalar state, quest logs and quest steps.
- Automatic save backups before destructive confirmed actions.
- Compatibility diagnostics, error center and exportable reports.

## Requirements

- Cloud Meadow **Beta 0.2.6.1** (Steam, Windows).
- BepInEx **5.4.23.x** configured for the game.

## Installation

1. Download `CloudMeadowCreativeMode-v6.0.0.zip` from Releases.
2. Extract it into the Cloud Meadow game directory.
3. Confirm this file exists:
   `BepInEx/plugins/CloudMeadowCreativeMode/CloudMeadow.CreativeMode.dll`
4. Start the game and press **F6**.

Back up important saves before extensive editing. The mod also keeps up to ten automatic backups under its plugin directory.

## Controls

| Key | Action |
|---|---|
| F6 | Toggle Creative Editor |
| F7 | Unlock gallery shortcut |
| F8 | Refresh runtime scan |

Hotkeys can be changed in the generated BepInEx configuration file.

## Build

The repository must be located directly inside the Cloud Meadow game directory so local references resolve:

```powershell
.\build-release.ps1
```

Output: `bin/CloudMeadow.CreativeMode.dll`.

## Support data

When reporting a problem, attach:

- `BepInEx/LogOutput.log`;
- `%USERPROFILE%/AppData/LocalLow/Team Nimbus/Cloud Meadow/output_log.txt`;
- the Compatibility report from the mod's Diagnostics page.

## Compatibility

This release targets game version **Beta 0.2.6.1**. After future game updates, check Diagnostics before editing a save.
