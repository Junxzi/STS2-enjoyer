using PartyRace.Core.Core;
using PartyRace.Core.Domain;

namespace PartyRace.Core.Compatibility;

public sealed record CompatibilityReport(
    bool IsCompatible,
    IReadOnlyList<PartyRaceErrorCode> Errors,
    IReadOnlyList<string> Warnings)
{
    public static CompatibilityReport Compatible(IReadOnlyList<string>? warnings = null)
    {
        return new CompatibilityReport(true, [], warnings ?? []);
    }
}

public sealed class CompatibilityService
{
    public CompatibilityReport CheckJoin(RaceRoom room, RacePlayer player)
    {
        List<PartyRaceErrorCode> errors = [];
        List<string> warnings = [];

        if (!string.Equals(player.ModVersion, room.Config.PartyRaceModVersion, StringComparison.Ordinal))
        {
            errors.Add(PartyRaceErrorCode.ModVersionMismatch);
        }

        if (player.ProtocolVersion != room.Config.ProtocolVersion)
        {
            errors.Add(PartyRaceErrorCode.ProtocolMismatch);
        }

        if (!string.Equals(player.GameBuild, room.Config.GameBuild, StringComparison.Ordinal))
        {
            errors.Add(PartyRaceErrorCode.GameBuildMismatch);
        }

        if (!string.Equals(player.GameplayModHash, room.Config.GameplayModHash, StringComparison.Ordinal))
        {
            if (room.Config.ModPolicy == ModPolicy.StrictGameplay)
            {
                errors.Add(PartyRaceErrorCode.GameplayModMismatch);
            }
            else
            {
                warnings.Add("Gameplay mod hash differs. Cosmetic-only policy must be verified by the host.");
            }
        }

        return new CompatibilityReport(errors.Count == 0, errors, warnings);
    }
}
