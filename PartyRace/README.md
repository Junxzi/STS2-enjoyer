# Party Race

Party Race is a Slay the Spire 2 mod implementation scaffold for private same-seed team races.

The current code intentionally implements the engine-independent MVP core first:

- race room, team, ready, and start state management
- same-seed generation and race config hashing
- compatibility checks for mod version, protocol, game build, and gameplay mod hash
- progress updates, heartbeat status, overlay rows, and result ranking
- transport and run-launch abstractions for STS2-specific adapters

STS2 hook points such as menu injection, seed injection, co-op lobby creation, and floor/room detection should be implemented behind the interfaces in `PartyRace.Core.RunSession` and `PartyRace.Core.Network` after confirming the current game/mod API.

## Download

Windows test build:

[Download party_race_windows_mod.zip](https://github.com/junxzi/STS2-enjoyer/releases/latest/download/party_race_windows_mod.zip)

Install it by copying the included `party_race` folder to:

```text
<Slay the Spire 2>\mods\party_race
```

Expected layout:

```text
<Slay the Spire 2>\mods\party_race\mod_manifest.json
<Slay the Spire 2>\mods\party_race\party_race.dll
<Slay the Spire 2>\mods\party_race\PartyRace.Core.dll
<Slay the Spire 2>\mods\party_race\PartyRace.Sts2Adapter.dll
```

## Build

```powershell
dotnet build PartyRace\PartyRace.sln
```

## Deploy to local STS2 install

From the repository root on the Windows machine with STS2 installed:

```powershell
PartyRace\scripts\deploy-local.ps1 -LaunchSteam
```

If STS2 is installed somewhere else:

```powershell
PartyRace\scripts\deploy-local.ps1 -Sts2InstallPath 'E:\SteamLibrary\steamapps\common\Slay the Spire 2' -LaunchSteam
```

The script builds `PartyRace.Mod`, deploys `mod_manifest.json`, renames
`PartyRace.Mod.dll` to the loader-required `party_race.dll`, and copies
`PartyRace.Core.dll` beside it for runtime dependency resolution.

After the game starts, confirm the initializer ran:

```powershell
Test-Path "$env:LOCALAPPDATA\PartyRace\party_race_mod_loaded.log"
```

On macOS, install .NET 9 and deploy with:

```bash
PartyRace/scripts/deploy-local-macos.sh
open 'steam://rungameid/2868840'
```

The macOS loader reads local directory mods from:

```text
~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods
```

The game must also have local mod loading enabled through its mod warning / modding
settings. A successful load writes:

```bash
tail -n 20 "$HOME/Library/Application Support/PartyRace/party_race_mod_loaded.log"
```

The current proof-of-life mod installs a `Party Race` button on the STS2 main
menu. It opens a local room UI that can create a room, ready the host, add a
ready demo rival, and produce a local race start plan. Networking, seed
injection, and run launch are not wired into the UI yet.

The STS2 adapter now includes a `PartyRaceNetEnvelope : INetMessage` and a
`Sts2TransportAdapter` that can send/receive Party Race core messages through
`INetGameService`. The mod also patches STS2 lobby constructors so it can capture
the live `INetGameService` when an STS2 multiplayer lobby exists.

## Tests

```powershell
dotnet run --project PartyRace\tests\PartyRace.Tests\PartyRace.Tests.csproj
```

On macOS with Homebrew `dotnet@9`, the test project can be built and run with:

```bash
/opt/homebrew/opt/dotnet@9/bin/dotnet build PartyRace/tests/PartyRace.Tests/PartyRace.Tests.csproj -m:1 /p:Sts2DataPath="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64"
/opt/homebrew/opt/dotnet@9/bin/dotnet PartyRace/tests/PartyRace.Tests/bin/Debug/net9.0/PartyRace.Tests.dll
```
