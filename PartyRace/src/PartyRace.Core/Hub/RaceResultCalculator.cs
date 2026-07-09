using PartyRace.Core.Domain;

namespace PartyRace.Core.Hub;

public sealed class RaceResultCalculator
{
    public IReadOnlyList<RaceResultRow> Calculate(RaceRoom room)
    {
        List<RaceResultRow> rows = [];

        foreach (RaceTeam team in room.Teams)
        {
            TeamProgress? progress = team.LatestProgress ?? room.ProgressByTeam.GetValueOrDefault(team.TeamId);
            ResultState resultState = progress?.ToResultState() ?? ResultState.Dnf;
            int act = progress?.Act ?? 0;
            int floor = progress?.Floor ?? 0;
            long elapsedMs = progress?.ElapsedMs ?? 0;
            bool warning = resultState is ResultState.Disconnected or ResultState.Dnf or ResultState.Disqualified;

            rows.Add(new RaceResultRow(0, team.TeamId, team.TeamName, resultState, act, floor, elapsedMs, warning));
        }

        List<RaceResultRow> sorted = rows
            .OrderBy(row => ResultPriority(row.ResultState))
            .ThenByDescending(row => row.Act)
            .ThenByDescending(row => row.Floor)
            .ThenBy(row => row.ElapsedMs == 0 ? long.MaxValue : row.ElapsedMs)
            .ThenBy(row => row.TeamName, StringComparer.Ordinal)
            .Select((row, index) => row with { Rank = index + 1 })
            .ToList();

        return sorted;
    }

    private static int ResultPriority(ResultState state)
    {
        return state switch
        {
            ResultState.Victory => 0,
            ResultState.Death => 1,
            ResultState.Retired => 2,
            ResultState.Running => 3,
            ResultState.Disconnected => 4,
            ResultState.Dnf => 5,
            ResultState.Disqualified => 6,
            _ => 9
        };
    }
}
