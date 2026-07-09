using PartyRace.Core.Core;

namespace PartyRace.Core.Domain;

public sealed class RaceRoom
{
    public required string RoomId { get; init; }
    public required string RoomName { get; set; }
    public required string HostPlayerId { get; init; }
    public RaceState State { get; set; } = RaceState.Lobby;
    public DateTimeOffset CreatedAt { get; init; }
    public RaceConfig Config { get; set; } = RaceConfig.Default();
    public List<RaceTeam> Teams { get; } = [];
    public List<RacePlayer> Players { get; } = [];
    public Dictionary<string, TeamProgress> ProgressByTeam { get; } = new(StringComparer.Ordinal);
    public List<RaceEvent> EventLog { get; } = [];
    public List<RaceResultRow> ResultTable { get; set; } = [];
    public DateTimeOffset? StartedAt { get; set; }

    public RacePlayer GetPlayer(string playerId)
    {
        return Players.FirstOrDefault(player => player.PlayerId == playerId)
            ?? throw new PartyRaceException(PartyRaceErrorCode.UnknownPlayer, $"Unknown player '{playerId}'.");
    }

    public RaceTeam GetTeam(string teamId)
    {
        return Teams.FirstOrDefault(team => team.TeamId == teamId)
            ?? throw new PartyRaceException(PartyRaceErrorCode.UnknownTeam, $"Unknown team '{teamId}'.");
    }
}

public sealed class RaceTeam
{
    public required string TeamId { get; init; }
    public required string TeamName { get; set; }
    public required string TeamLeaderPlayerId { get; set; }
    public List<string> PlayerIds { get; } = [];
    public TeamReadyState ReadyState { get; set; } = TeamReadyState.NotReady;
    public RunSessionState RunState { get; set; } = RunSessionState.NotStarted;
    public TeamProgress? LatestProgress { get; set; }
}

public sealed class RacePlayer
{
    public required string PlayerId { get; init; }
    public required string DisplayName { get; set; }
    public string? TeamId { get; set; }
    public string? CharacterId { get; set; }
    public bool IsReady { get; set; }
    public bool IsSpectator { get; set; }
    public bool IsConnected { get; set; } = true;
    public required string ModVersion { get; init; }
    public required int ProtocolVersion { get; init; }
    public required string GameBuild { get; init; }
    public required string GameplayModHash { get; init; }
}

public sealed record RaceConfig(
    SeedMode SeedMode,
    string? RunSeed,
    int Ascension,
    int MaxTeams,
    int MaxPlayersPerTeam,
    VisibilityRule VisibilityRule,
    CharacterRule CharacterRule,
    ModPolicy ModPolicy,
    TimerPolicy TimerPolicy,
    string GameBuild,
    string GameBranch,
    string GameplayModHash,
    string PartyRaceModVersion,
    int ProtocolVersion,
    string RaceConfigHash)
{
    public static RaceConfig Default()
    {
        return new RaceConfig(
            SeedMode.SharedRandom,
            null,
            0,
            PartyRaceConstants.DefaultMaxTeams,
            PartyRaceConstants.DefaultMaxPlayersPerTeam,
            VisibilityRule.FairRace,
            CharacterRule.Any,
            ModPolicy.StrictGameplay,
            TimerPolicy.RealTime,
            "unknown",
            "unknown",
            "none",
            PartyRaceConstants.ModVersion,
            PartyRaceConstants.ProtocolVersion,
            string.Empty);
    }
}

public sealed record TeamProgress(
    string TeamId,
    int Act,
    int Floor,
    RoomType RoomType,
    RacePhase Phase,
    int BossesDefeated,
    int AlivePlayers,
    int TotalPlayers,
    long ElapsedMs,
    bool IsVictory,
    bool IsDead,
    bool IsRetired,
    bool IsDisconnected,
    string Checksum)
{
    public ResultState ToResultState()
    {
        if (IsVictory)
        {
            return ResultState.Victory;
        }

        if (IsDead)
        {
            return ResultState.Death;
        }

        if (IsRetired)
        {
            return ResultState.Retired;
        }

        if (IsDisconnected)
        {
            return ResultState.Disconnected;
        }

        return ResultState.Running;
    }
}

public sealed record RaceResultRow(
    int Rank,
    string TeamId,
    string TeamName,
    ResultState ResultState,
    int Act,
    int Floor,
    long ElapsedMs,
    bool HasWarning);

public sealed record RaceEvent(DateTimeOffset At, string Type, string Message);
