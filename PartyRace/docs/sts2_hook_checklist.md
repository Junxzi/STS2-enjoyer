# STS2 Hook Checklist

Observed local install:

- Steam AppID: `2868840`
- Install: `D:\SteamLibrary\steamapps\common\Slay the Spire 2`
- Game version: `v0.107.1`
- Branch: `v0.107.1`
- Commit: `59260271`
- Steam buildid: `23811903`
- Main assembly hash: `-1555940892`

Useful files:

- `SlayTheSpire2.exe`
- `SlayTheSpire2.pck`
- `release_info.json`
- `data_sts2_windows_x86_64\sts2.dll`
- `data_sts2_windows_x86_64\0Harmony.dll`
- `data_sts2_windows_x86_64\Steamworks.NET.dll`
- `mods\`

macOS observed install:

- Install: `~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2`
- App: `SlayTheSpire2.app`
- Release info: `SlayTheSpire2.app/Contents/Resources/release_info.json`
- Main assembly: `SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64/sts2.dll`
- Local mod loader directory: `SlayTheSpire2.app/Contents/MacOS/mods`
- Log: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`

Confirmed public API surface from `sts2.dll`:

- `MegaCrit.Sts2.Core.Modding.IModManagerFileIo`
  - `GetFilesAt(string)`
  - `GetDirectoriesAt(string)`
  - `FileExists(string)`
  - `DirectoryExists(string)`
- `MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService`
  - `NetId`
  - `IsConnected`
  - `IsGameLoading`
  - `SendMessage<T>(T, ulong)`
  - `SendMessage<T>(T)`
  - `Update()`
  - `SetGameLoading(bool)`
  - `SetBufferMessages(bool)`
  - `GetRawLobbyIdentifier()`
- `MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.IStartRunLobbyListener`
  - `AscensionChanged()`
  - `SeedChanged()`
  - `ModifiersChanged()`
  - `MaxAscensionChanged()`
- `MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.ILoadRunLobbyListener`
  - `PlayerConnected(ulong)`
  - `RemotePlayerDisconnected(ulong)`
  - `BeginRun()`
  - `PlayerReadyChanged(ulong)`
- `MegaCrit.Sts2.Core.Multiplayer.Serialization.INetMessage`
  - `ShouldBroadcast`
  - `ShouldBuffer`

Next live-game checks:

1. Determine the expected mod folder layout under `mods\`.
   - Confirmed local folder: `mods\party_race\mod_manifest.json`
   - Manifest keys use snake_case for multi-word fields: `has_dll`, `has_pck`, `affects_gameplay`, `min_game_version`.
2. Confirm how the game discovers a mod assembly and entrypoint.
   - macOS confirmed: local mods are read from the executable directory,
     `SlayTheSpire2.app/Contents/MacOS/mods`.
   - macOS confirmed: `settings.save` must contain
     `mod_settings.mods_enabled: true` before DLL initialization runs.
3. Confirm whether `PartyRaceNetEnvelope : INetMessage` can be serialized directly or needs `IPacketSerializable`.
   - Confirmed: `INetMessage` extends `IPacketSerializable` and requires
     `ShouldBroadcast`, `Mode`, `LogLevel`, `ShouldBuffer`,
     `Serialize(PacketWriter)`, and `Deserialize(PacketReader)`.
   - Implemented: `PartyRaceNetEnvelope` serializes protocol version, message
     kind, broadcast/buffer flags, and JSON payload.
   - Implemented: `Sts2TransportAdapter` registers/unregisters
     `PartyRaceNetEnvelope` handlers and sends through `INetGameService`.
4. Identify a stable menu/main scene hook for the Party Race menu.
   - macOS confirmed: Harmony postfix on
     `MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu._Ready` can add a
     Godot `Button` to the main menu.
   - Confirmed log evidence:
     `Installed main menu patch.`, `Added Party Race main menu button.`, and
     `Party Race main menu button pressed.`
   - macOS confirmed: the button can open an in-game `PartyRaceMenuView`.
     The local proof UI can create a room, ready the host, add a ready demo
     rival, and call `RaceRoomManager.StartRace`.
   - Runtime dependency note: deploy `PartyRace.Core.dll` beside
     `party_race.dll`; the mod installs an assembly resolver to load that DLL
     from the mod directory.
5. Identify the seed field/controller behind the start-run lobby.
6. Identify current run state source for act, floor, room type, phase, victory, death, and retire.
7. Confirm whether host/client identity can be derived from `INetGameService.NetId` and lobby listeners.
   - Capture hook installed for `StartRunLobby`, `LoadRunLobby`, and `RunLobby`.
   - Next live verification: create a real STS2 multiplayer lobby and confirm
     `Captured STS2 net service ... id=... lobby=...` appears in the Party Race
     log. This step creates/uses an online Steam/STS2 lobby.
