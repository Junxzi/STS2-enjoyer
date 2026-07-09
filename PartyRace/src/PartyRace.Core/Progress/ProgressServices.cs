using System.Security.Cryptography;
using System.Text;
using PartyRace.Core.Core;
using PartyRace.Core.Domain;

namespace PartyRace.Core.Progress;

public sealed class ChecksumBuilder
{
    public string Build(TeamProgress progress)
    {
        string canonical = string.Join("|",
            progress.TeamId,
            progress.Act,
            progress.Floor,
            progress.RoomType,
            progress.Phase,
            progress.BossesDefeated,
            progress.AlivePlayers,
            progress.TotalPlayers,
            progress.ElapsedMs,
            progress.IsVictory,
            progress.IsDead,
            progress.IsRetired,
            progress.IsDisconnected);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..6];
    }
}

public sealed class ProgressTracker(IClock clock, ChecksumBuilder checksumBuilder)
{
    private readonly Dictionary<string, DateTimeOffset> _lastHeartbeatByTeam = new(StringComparer.Ordinal);

    public TeamProgress AcceptUpdate(RaceRoom room, TeamProgress update)
    {
        RaceTeam team = room.GetTeam(update.TeamId);
        TeamProgress normalized = update with { Checksum = checksumBuilder.Build(update) };
        team.LatestProgress = normalized;
        team.RunState = normalized.ToResultState() == ResultState.Running ? RunSessionState.Running : RunSessionState.Finished;
        room.ProgressByTeam[update.TeamId] = normalized;
        _lastHeartbeatByTeam[update.TeamId] = clock.UtcNow;
        return normalized;
    }

    public IReadOnlyList<TeamConnectionStatus> GetConnectionStatuses(RaceRoom room)
    {
        List<TeamConnectionStatus> statuses = [];

        foreach (RaceTeam team in room.Teams)
        {
            if (!_lastHeartbeatByTeam.TryGetValue(team.TeamId, out DateTimeOffset lastHeartbeat))
            {
                statuses.Add(new TeamConnectionStatus(team.TeamId, false, true, TimeSpan.MaxValue));
                continue;
            }

            TimeSpan age = clock.UtcNow - lastHeartbeat;
            statuses.Add(new TeamConnectionStatus(
                team.TeamId,
                age.TotalMilliseconds >= PartyRaceConstants.DisconnectedMs,
                age.TotalMilliseconds >= PartyRaceConstants.TimeoutWarningMs,
                age));
        }

        return statuses;
    }
}

public sealed record TeamConnectionStatus(string TeamId, bool IsDisconnected, bool ShouldWarn, TimeSpan LastHeartbeatAge);
