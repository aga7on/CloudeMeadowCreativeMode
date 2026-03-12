# CloudMeadow.CreativeMode

BepInEx plugin for Cloud Meadow.

## Build
1. Open this folder as a standalone repo.
2. Build `CreativeModePlugin.csproj` in `Release`.
3. Or run `build-release.ps1`.

## Install
Copy `CloudMeadow.CreativeMode.dll` to:

`BepInEx/plugins/CloudMeadowCreativeMode/`

## Notes
- Targets Unity Mono / .NET 3.5.
- References are resolved against the local game install.
- `bin/`, `obj/`, `.vs/`, logs and backup DLLs are excluded from repo.
