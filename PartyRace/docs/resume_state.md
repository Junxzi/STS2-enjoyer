# Party Race Resume State

Last updated: 2026-07-09

## Current status

Work resumed on macOS and live STS2 mod-loading plus a local Party Race
main-menu flow were confirmed.

Implemented locally:

- `PartyRace.Core`: engine-independent MVP race domain and services.
- `PartyRace.Sts2Adapter`: STS2 install/build probing and adapter boundary.
- `PartyRace.Mod`: local STS2 mod assembly with a main-menu Party Race button
  and an in-game local room proof UI.
- `PartyRace.Tools.Sts2Inspector`: metadata inspector for reading `sts2.dll` without runtime type-load failures.
- `PartyRace.Sts2Adapter`: STS2 `INetMessage` envelope and `INetGameService`
  transport adapter for Party Race messages.
- Regression tests: `9/9` passed after the network codec work.

Windows STS2 install previously observed:

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

- Re-run the Windows loader pass with the current deploy script.

macOS live-loader pass completed:

- Local install: `~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2`
- Data path: `SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64`
- Loader path confirmed by `godot.log`:
  `SlayTheSpire2.app/Contents/MacOS/mods/party_race/mod_manifest.json`
- `mods_enabled` must be true under `settings.save` -> `mod_settings`; the JSON key is `mods_enabled`, not `player_agreed_to_mod_loading`.
- Confirmed STS2 loaded `party_race.dll`, called `PartyRace.Mod.PartyRaceMod.Initialize`, and wrote:
  `~/Library/Application Support/PartyRace/party_race_mod_loaded.log`
- Latest confirmed initializer and menu-hook lines:
  - `[2026-07-09T18:28:25.1180340+09:00] Party Race 0.1.0 initializer called.`
  - `[2026-07-09T18:28:25.4974310+09:00] Installed main menu patch.`
  - `[2026-07-09T18:28:36.8854980+09:00] Added Party Race main menu button.`
  - `[2026-07-09T18:32:54.0663870+09:00] Party Race main menu button pressed.`
- Visual verification completed on macOS: the `Party Race` button appeared on
  the STS2 main menu and clicking it wrote the button-pressed log line.
- `PartyRace.Core.dll` must be copied beside `party_race.dll`; STS2 did not
  automatically resolve that dependency from the mod directory. The mod now
  installs an `AssemblyResolve` handler that loads dependencies from the
  deployed mod folder.
- Latest confirmed local UI flow:
  - `[2026-07-09T18:48:13.4329680+09:00] Resolved dependency from mod directory: PartyRace.Core.dll`
  - `[2026-07-09T18:49:07.2951030+09:00] Party Race main menu button pressed.`
  - `[2026-07-09T18:49:17.2532640+09:00] Created local Party Race room seed=5G8RHXWVWSDG.`
  - `[2026-07-09T18:49:24.2611020+09:00] Host ready changed ready=True.`
  - `[2026-07-09T18:49:32.6958160+09:00] Added ready demo rival.`
  - `[2026-07-09T18:49:42.8975840+09:00] Local Party Race started seed=5G8RHXWVWSDG hash=B7C1-4B67-208F.`
- Latest confirmed network-adapter setup:
  - `PartyRaceNetEnvelope` implements STS2 `INetMessage`.
  - `Sts2TransportAdapter` registers a handler through `INetGameService.RegisterMessageHandler<PartyRaceNetEnvelope>`.
  - The mod patches `StartRunLobby`, `LoadRunLobby`, and `RunLobby` constructors
    to capture the live `INetGameService` when an STS2 lobby exists.
  - Deployment now copies `PartyRace.Sts2Adapter.dll` beside `party_race.dll`.
  - Live startup confirmed the net-service capture patches were installed at
    `2026-07-09T19:01:46+09:00`.
  - A real Steam/STS2 online lobby was not created during this pass; that is the
    next verification step and has external lobby/network side effects.

## Exact next commands

```powershell
PartyRace\scripts\deploy-local.ps1 -LaunchSteam

Test-Path "$env:LOCALAPPDATA\PartyRace\party_race_mod_loaded.log"
Get-Content -Tail 10 "$env:APPDATA\SlayTheSpire2\logs\godot.log"
```

The deploy script intentionally copies `PartyRace.Core.dll` beside
`party_race.dll`; the mod initializer references core constants, so deploying
only the renamed mod DLL can leave the loader without a required dependency.

On macOS:

```bash
PartyRace/scripts/deploy-local-macos.sh
open 'steam://rungameid/2868840'
tail -n 20 "$HOME/Library/Application Support/PartyRace/party_race_mod_loaded.log"
rg -n "party_race|RUNNING MODDED" "$HOME/Library/Application Support/SlayTheSpire2/logs/godot.log"
```

The current macOS menu proof adds a `Party Race` button to `NMainMenu._Ready`
through Harmony. It opens a local room UI that can create a room, ready the host,
add a ready demo rival, and produce a `RaceStartPlan`. Networking, real lobby
identity, seed injection, and run launch are still not wired into the UI, but
the STS2 `INetGameService` capture and Party Race network envelope are now in
place.

## GitHub status

- Repository exists at `junxzi/STS2-enjoyer`.
- Local clone is on `master` tracking `origin/master`.
- Latest observed commit after clone: `2bcd3b2 Add Party Race mod scaffold`.

To push follow-up work later:

```powershell
gh auth status
git status
git push origin master
```
