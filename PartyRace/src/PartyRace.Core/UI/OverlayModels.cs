using PartyRace.Core.Domain;

namespace PartyRace.Core.UI;

public sealed record OverlayRow(
    int Rank,
    string TeamName,
    int Act,
    int Floor,
    RoomType RoomType,
    RacePhase Phase,
    long ElapsedMs,
    ResultState ResultState,
    bool IsWarning);

public sealed class OverlayPresenter
{
    public IReadOnlyList<OverlayRow> BuildRows(RaceRoom room)
    {
        return room.Teams
            .Select(team =>
            {
                TeamProgress? progress = team.LatestProgress ?? room.ProgressByTeam.GetValueOrDefault(team.TeamId);
                return new
                {
                    Team = team,
                    Progress = progress,
                    Result = progress?.ToResultState() ?? ResultState.Running
                };
            })
            .OrderByDescending(item => item.Progress?.Act ?? 0)
            .ThenByDescending(item => item.Progress?.Floor ?? 0)
            .ThenBy(item => item.Progress?.ElapsedMs ?? long.MaxValue)
            .ThenBy(item => item.Team.TeamName, StringComparer.Ordinal)
            .Select((item, index) => new OverlayRow(
                index + 1,
                item.Team.TeamName,
                item.Progress?.Act ?? 0,
                item.Progress?.Floor ?? 0,
                item.Progress?.RoomType ?? RoomType.Unknown,
                item.Progress?.Phase ?? RacePhase.Unknown,
                item.Progress?.ElapsedMs ?? 0,
                item.Result,
                item.Result is ResultState.Disconnected or ResultState.Dnf or ResultState.Disqualified))
            .ToList();
    }

    public IReadOnlySet<string> AllowedFairRaceFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "team_name",
        "rank",
        "act",
        "floor",
        "room_type",
        "phase",
        "elapsed_time",
        "result_state"
    };
}
