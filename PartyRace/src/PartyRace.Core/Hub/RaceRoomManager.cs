using PartyRace.Core.Compatibility;
using PartyRace.Core.Core;
using PartyRace.Core.Domain;

namespace PartyRace.Core.Hub;

public sealed class RaceRoomManager(
    IClock clock,
    RaceConfigValidator configValidator,
    SeedService seedService,
    CompatibilityService compatibilityService,
    IPartyRaceLogger? logger = null)
{
    private readonly IPartyRaceLogger _logger = logger ?? NullPartyRaceLogger.Instance;

    public RaceRoom CreateRoom(string roomId, string roomName, RacePlayer host, RaceConfig config)
    {
        seedService.Validate(config.RunSeed ?? "PENDING");
        RaceConfig hashedConfig = configValidator.WithComputedHash(config);

        RaceRoom room = new()
        {
            RoomId = roomId,
            RoomName = roomName,
            HostPlayerId = host.PlayerId,
            CreatedAt = clock.UtcNow,
            Config = hashedConfig
        };

        room.Players.Add(host);
        room.EventLog.Add(new RaceEvent(clock.UtcNow, "RoomCreated", $"Room '{roomName}' created by '{host.PlayerId}'."));
        _logger.Info($"[PartyRace] Room created id={roomId} host={host.PlayerId} max_teams={config.MaxTeams}");
        return room;
    }

    public void JoinRoom(RaceRoom room, RacePlayer player)
    {
        if (room.Players.Any(existing => existing.PlayerId == player.PlayerId))
        {
            throw new PartyRaceException(PartyRaceErrorCode.DuplicatePlayer, $"Player '{player.PlayerId}' is already in the room.");
        }

        if (room.Players.Count(playerInRoom => !playerInRoom.IsSpectator) >= PartyRaceConstants.DefaultMaxPlayers && room.State == RaceState.Lobby)
        {
            throw new PartyRaceException(PartyRaceErrorCode.RoomFull, "Race room is full.");
        }

        CompatibilityReport report = compatibilityService.CheckJoin(room, player);
        if (!report.IsCompatible)
        {
            throw new PartyRaceException(report.Errors[0], $"Player '{player.PlayerId}' is not compatible with this room.");
        }

        player.IsSpectator = room.State != RaceState.Lobby;
        room.Players.Add(player);
        room.EventLog.Add(new RaceEvent(clock.UtcNow, "PlayerJoined", $"Player '{player.PlayerId}' joined."));
    }

    public RaceTeam CreateTeam(RaceRoom room, string leaderPlayerId, string teamName)
    {
        EnsureLobby(room);

        if (room.Teams.Count >= room.Config.MaxTeams)
        {
            throw new PartyRaceException(PartyRaceErrorCode.TeamFull, "Maximum team count reached.");
        }

        RacePlayer leader = room.GetPlayer(leaderPlayerId);
        if (leader.IsSpectator)
        {
            throw new PartyRaceException(PartyRaceErrorCode.LateJoinPlayDenied, "Spectators cannot create teams.");
        }

        string teamId = CreateStableTeamId(room.Teams.Count + 1);
        RaceTeam team = new()
        {
            TeamId = teamId,
            TeamName = ValidateTeamName(teamName),
            TeamLeaderPlayerId = leaderPlayerId
        };

        room.Teams.Add(team);
        MovePlayerToTeam(room, leaderPlayerId, teamId);
        room.EventLog.Add(new RaceEvent(clock.UtcNow, "TeamCreated", $"Team '{team.TeamName}' created."));
        return team;
    }

    public void MovePlayerToTeam(RaceRoom room, string playerId, string teamId)
    {
        EnsureLobby(room);
        RacePlayer player = room.GetPlayer(playerId);
        RaceTeam destination = room.GetTeam(teamId);

        if (player.IsSpectator)
        {
            throw new PartyRaceException(PartyRaceErrorCode.LateJoinPlayDenied, "Spectators cannot join teams.");
        }

        if (!destination.PlayerIds.Contains(playerId, StringComparer.Ordinal) && destination.PlayerIds.Count >= room.Config.MaxPlayersPerTeam)
        {
            throw new PartyRaceException(PartyRaceErrorCode.TeamFull, $"Team '{destination.TeamName}' is full.");
        }

        if (player.TeamId is not null && !string.Equals(player.TeamId, teamId, StringComparison.Ordinal))
        {
            RaceTeam oldTeam = room.GetTeam(player.TeamId);
            oldTeam.PlayerIds.Remove(playerId);
            PromoteLeaderIfNeeded(room, oldTeam);
            oldTeam.ReadyState = TeamReadyState.NotReady;
        }

        if (!destination.PlayerIds.Contains(playerId, StringComparer.Ordinal))
        {
            destination.PlayerIds.Add(playerId);
        }

        player.TeamId = teamId;
        player.IsReady = false;
        destination.ReadyState = TeamReadyState.NotReady;
        room.EventLog.Add(new RaceEvent(clock.UtcNow, "PlayerMoved", $"Player '{playerId}' moved to '{teamId}'."));
    }

    public void SetPlayerReady(RaceRoom room, string playerId, string characterId, bool isReady)
    {
        EnsureLobby(room);
        RacePlayer player = room.GetPlayer(playerId);

        if (player.TeamId is null)
        {
            throw new PartyRaceException(PartyRaceErrorCode.NoTeamSelected, "Player must choose a team before readying.");
        }

        player.CharacterId = characterId;
        player.IsReady = isReady;
        RaceTeam team = room.GetTeam(player.TeamId);
        team.ReadyState = team.PlayerIds.Count > 0 && team.PlayerIds.All(id => room.GetPlayer(id).IsReady)
            ? TeamReadyState.Ready
            : TeamReadyState.NotReady;

        room.EventLog.Add(new RaceEvent(clock.UtcNow, "ReadyChanged", $"Player '{playerId}' ready={isReady}."));
    }

    public RaceStartPlan StartRace(RaceRoom room)
    {
        configValidator.EnsureStartable(room);
        room.State = RaceState.Countdown;

        DateTimeOffset startedAt = clock.UtcNow;
        room.StartedAt = startedAt;
        room.State = RaceState.Running;

        foreach (RaceTeam team in room.Teams)
        {
            team.RunState = RunSessionState.Launching;
        }

        room.EventLog.Add(new RaceEvent(clock.UtcNow, "RaceStarted", $"Race started seed={room.Config.RunSeed} hash={room.Config.RaceConfigHash}."));
        return new RaceStartPlan(room.RoomId, room.Config.RunSeed!, room.Config.RaceConfigHash, startedAt, room.Teams.Select(team => team.TeamId).ToArray());
    }

    public void RecordDisconnect(RaceRoom room, string playerId)
    {
        RacePlayer player = room.GetPlayer(playerId);
        player.IsConnected = false;

        if (player.TeamId is not null)
        {
            RaceTeam team = room.GetTeam(player.TeamId);
            if (string.Equals(team.TeamLeaderPlayerId, playerId, StringComparison.Ordinal))
            {
                PromoteLeaderIfNeeded(room, team);
            }
        }
    }

    private static void EnsureLobby(RaceRoom room)
    {
        if (room.State != RaceState.Lobby)
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidRoomState, "This action is only allowed in the lobby.");
        }
    }

    private static string ValidateTeamName(string teamName)
    {
        string trimmed = teamName.Trim();
        if (trimmed.Length is < 1 or > 24 || trimmed.Any(char.IsControl))
        {
            throw new ArgumentException("Team name must be 1-24 visible characters.", nameof(teamName));
        }

        return trimmed;
    }

    private static string CreateStableTeamId(int index)
    {
        return $"team_{index:00}";
    }

    private static void PromoteLeaderIfNeeded(RaceRoom room, RaceTeam team)
    {
        if (team.PlayerIds.Contains(team.TeamLeaderPlayerId, StringComparer.Ordinal))
        {
            return;
        }

        string? nextLeader = team.PlayerIds
            .Select(room.GetPlayer)
            .Where(player => player.IsConnected)
            .OrderBy(player => room.Players.IndexOf(player))
            .Select(player => player.PlayerId)
            .FirstOrDefault();

        if (nextLeader is null)
        {
            team.RunState = RunSessionState.Disconnected;
            return;
        }

        team.TeamLeaderPlayerId = nextLeader;
    }
}

public sealed record RaceStartPlan(string RoomId, string RunSeed, string RaceConfigHash, DateTimeOffset StartedAt, IReadOnlyList<string> TeamIds);
