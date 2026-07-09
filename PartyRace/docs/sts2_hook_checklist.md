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
3. Confirm whether `PartyRaceNetEnvelope : INetMessage` can be serialized directly or needs `IPacketSerializable`.
   - Current build evidence: `INetMessage` also requires `Mode`, `LogLevel`, `Serialize(PacketWriter)`, and `Deserialize(PacketReader)`.
   - Do not send Party Race messages through `INetGameService` until the packet schema is confirmed.
4. Identify a stable menu/main scene hook for the Party Race menu.
5. Identify the seed field/controller behind the start-run lobby.
6. Identify current run state source for act, floor, room type, phase, victory, death, and retire.
7. Confirm whether host/client identity can be derived from `INetGameService.NetId` and lobby listeners.
