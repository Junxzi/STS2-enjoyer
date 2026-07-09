# Party Race Resume State

Last updated: 2026-07-09

## Current status

Work paused intentionally after the first live STS2 mod-loading pass.

Implemented locally:

- `PartyRace.Core`: engine-independent MVP race domain and services.
- `PartyRace.Sts2Adapter`: STS2 install/build probing and adapter boundary.
- `PartyRace.Mod`: minimal local STS2 mod assembly for loader verification.
- `PartyRace.Tools.Sts2Inspector`: metadata inspector for reading `sts2.dll` without runtime type-load failures.
- Regression tests: `8/8` passed before the STS2 live-loader work.

Local STS2 install:

- AppID: `2868840`
- Install path: `D:\SteamLibrary\steamapps\common\Slay the Spire 2`
- Version: `v0.107.1`
- Branch: `v0.107.1`
- Steam buildid: `23811903`

## Live loader findings

Confirmed:

- STS2 detects `mods\party_race\mod_manifest.json`.
- Manifest multi-word keys must be snake_case:
  - `has_dll`
  - `has_pck`
  - `affects_gameplay`
  - `min_game_version`
- When `has_dll` is true, STS2 expects the assembly filename to match the mod id:
  - expected: `mods\party_race\party_race.dll`
- STS2 successfully loaded the DLL after copying `PartyRace.Mod.dll` to `party_race.dll`.

Current loader error:

```text
Found mod initializer class of type PartyRace.Mod.PartyRaceMod,
but it does not contain the method PartyRace.Mod.PartyRaceMod.Initialize
declared in the ModInitializerAttribute.
```

Fix already made in source:

- `PartyRace/src/PartyRace.Mod/PartyRaceMod.cs`
- Changed `[ModInitializer("PartyRace.Mod.PartyRaceMod.Initialize")]` to `[ModInitializer("Initialize")]`.

Built successfully after the fix:

```powershell
dotnet build PartyRace\src\PartyRace.Mod\PartyRace.Mod.csproj
```

Not yet done after that build:

- Copy the rebuilt DLL to `D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\party_race\party_race.dll`.
- Restart STS2 through Steam.
- Confirm `%LOCALAPPDATA%\PartyRace\party_race_mod_loaded.log` is created.

## Exact next commands

```powershell
dotnet build PartyRace\PartyRace.sln

Copy-Item -Force `
  -Path 'PartyRace\src\PartyRace.Mod\bin\Debug\net9.0\PartyRace.Mod.dll' `
  -Destination 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\party_race\party_race.dll'

Copy-Item -Force `
  -Path 'PartyRace\src\PartyRace.Mod\bin\Debug\net9.0\PartyRace.Mod.pdb' `
  -Destination 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\party_race\party_race.pdb'

Start-Process 'steam://rungameid/2868840'

Test-Path "$env:LOCALAPPDATA\PartyRace\party_race_mod_loaded.log"
Get-Content -Tail 10 "$env:APPDATA\SlayTheSpire2\logs\godot.log"
```

## GitHub status

- Local git repo has no commits yet and is on `master`.
- No remote is configured.
- `gh` is installed: `gh version 2.93.0`.
- `gh auth status` currently fails because the token for `Junxzi` is invalid.

To create/push the GitHub repo later:

```powershell
gh auth login -h github.com
gh auth status
gh repo create STS2-enjoyer --private --source . --remote origin --push
```

Recommended first commit message:

```text
Add Party Race mod scaffold
```
