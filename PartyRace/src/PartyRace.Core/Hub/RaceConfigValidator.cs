using System.Security.Cryptography;
using System.Text;
using PartyRace.Core.Core;
using PartyRace.Core.Domain;

namespace PartyRace.Core.Hub;

public sealed class RaceConfigValidator
{
    public RaceConfig WithComputedHash(RaceConfig config)
    {
        return config with { RaceConfigHash = ComputeHash(config) };
    }

    public string ComputeHash(RaceConfig config)
    {
        string canonical = string.Join("|",
            config.RunSeed ?? string.Empty,
            config.SeedMode,
            config.GameBuild,
            config.GameBranch,
            config.Ascension,
            config.CharacterRule,
            config.MaxPlayersPerTeam,
            config.VisibilityRule,
            config.ModPolicy,
            config.GameplayModHash,
            config.PartyRaceModVersion,
            config.ProtocolVersion);

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        string hex = Convert.ToHexString(digest);
        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}";
    }

    public void EnsureStartable(RaceRoom room)
    {
        if (room.State != RaceState.Lobby)
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidRoomState, "Race can only start from the lobby.");
        }

        if (room.Teams.Count < 2)
        {
            throw new PartyRaceException(PartyRaceErrorCode.TeamNotReady, "At least two teams are required.");
        }

        if (string.IsNullOrWhiteSpace(room.Config.RunSeed))
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidSeed, "A run seed is required.");
        }

        string expectedHash = ComputeHash(room.Config);
        if (!string.Equals(expectedHash, room.Config.RaceConfigHash, StringComparison.Ordinal))
        {
            throw new PartyRaceException(PartyRaceErrorCode.RaceConfigHashMismatch, "Race config hash does not match current settings.");
        }

        foreach (RaceTeam team in room.Teams)
        {
            if (team.PlayerIds.Count == 0 || team.ReadyState != TeamReadyState.Ready)
            {
                throw new PartyRaceException(PartyRaceErrorCode.TeamNotReady, $"Team '{team.TeamName}' is not ready.");
            }
        }
    }
}
