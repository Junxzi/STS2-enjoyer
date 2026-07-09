# Party Race

Party Race is a Slay the Spire 2 mod implementation scaffold for private same-seed team races.

The current code intentionally implements the engine-independent MVP core first:

- race room, team, ready, and start state management
- same-seed generation and race config hashing
- compatibility checks for mod version, protocol, game build, and gameplay mod hash
- progress updates, heartbeat status, overlay rows, and result ranking
- transport and run-launch abstractions for STS2-specific adapters

STS2 hook points such as menu injection, seed injection, co-op lobby creation, and floor/room detection should be implemented behind the interfaces in `PartyRace.Core.RunSession` and `PartyRace.Core.Network` after confirming the current game/mod API.

## Build

```powershell
dotnet build PartyRace\PartyRace.sln
```

## Tests

```powershell
dotnet run --project PartyRace\tests\PartyRace.Tests\PartyRace.Tests.csproj
```
