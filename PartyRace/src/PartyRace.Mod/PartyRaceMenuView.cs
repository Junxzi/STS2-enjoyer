using Godot;
using PartyRace.Core.Compatibility;
using PartyRace.Core.Core;
using PartyRace.Core.Domain;
using PartyRace.Core.Hub;
using PartyRace.Core.Network;

namespace PartyRace.Mod;

public sealed partial class PartyRaceMenuView : Control
{
    private const string MenuNodeName = "PartyRaceMenuView";
    private const string RivalPlayerId = "demo_rival";

    private readonly ManualClock _clock = new(DateTimeOffset.UtcNow);
    private readonly SeedService _seedService = new();
    private readonly RaceConfigValidator _configValidator = new();
    private readonly RaceRoomManager _roomManager;

    private bool _isBuilt;
    private LineEdit? _roomNameInput;
    private LineEdit? _seedInput;
    private Label? _statusLabel;
    private Label? _teamsLabel;
    private Label? _eventsLabel;
    private Button? _readyButton;
    private Button? _addRivalButton;
    private Button? _startButton;
    private RaceRoom? _room;

    public PartyRaceMenuView()
    {
        _roomManager = new RaceRoomManager(
            _clock,
            _configValidator,
            _seedService,
            new CompatibilityService());
        PartyRaceSts2Context.MessageReceived += OnPartyRaceMessageReceived;
    }

    public static PartyRaceMenuView EnsureAttached(Control parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name.ToString() == MenuNodeName && child is PartyRaceMenuView existing)
            {
                return existing;
            }
        }

        PartyRaceMenuView view = new()
        {
            Name = MenuNodeName
        };
        parent.AddChild(view);
        return view;
    }

    public override void _Ready()
    {
        EnsureBuilt();
    }

    public void Open()
    {
        EnsureBuilt();
        Visible = true;
        if (_seedInput is not null && string.IsNullOrWhiteSpace(_seedInput.Text))
        {
            _seedInput.Text = _seedService.GenerateSharedRandomSeed();
        }

        Refresh("Party Race menu opened.");
    }

    private void EnsureBuilt()
    {
        if (_isBuilt)
        {
            return;
        }

        _isBuilt = true;
        Visible = false;
        Position = new Vector2(64, 56);
        Size = new Vector2(680, 500);
        ZIndex = 1100;
        MouseFilter = MouseFilterEnum.Stop;

        PanelContainer panel = new()
        {
            Position = Vector2.Zero,
            Size = Size,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(panel);

        AddLabel("Party Race", 24, 18, 620, 36);
        AddLabel("Local room proof. Networking capture is shown when an STS2 lobby exists.", 24, 58, 620, 28);

        AddLabel("Room", 24, 104, 120, 28);
        _roomNameInput = AddLineEdit("Local Party Race", 144, 100, 260, 36);

        AddLabel("Seed", 24, 150, 120, 28);
        _seedInput = AddLineEdit(_seedService.GenerateSharedRandomSeed(), 144, 146, 260, 36);

        Button createButton = AddButton("Create local room", 424, 100, 220, 36);
        createButton.Pressed += CreateLocalRoom;

        _readyButton = AddButton("Toggle ready", 424, 146, 220, 36);
        _readyButton.Pressed += ToggleHostReady;

        _addRivalButton = AddButton("Add demo rival", 424, 192, 220, 36);
        _addRivalButton.Pressed += AddDemoRival;

        _startButton = AddButton("Start race", 424, 238, 220, 36);
        _startButton.Pressed += StartRace;

        Button closeButton = AddButton("Close", 424, 424, 220, 36);
        closeButton.Pressed += () =>
        {
            Visible = false;
            PartyRaceLog.Append("Party Race menu closed.");
        };

        _statusLabel = AddLabel("Create a local room to begin.", 24, 204, 360, 56);
        _teamsLabel = AddLabel("Teams: none", 24, 276, 620, 78);
        _eventsLabel = AddLabel("Events: none", 24, 368, 360, 92);
        Refresh();
    }

    private Label AddLabel(string text, float x, float y, float width, float height)
    {
        Label label = new()
        {
            Text = text,
            Position = new Vector2(x, y),
            Size = new Vector2(width, height)
        };
        AddChild(label);
        return label;
    }

    private LineEdit AddLineEdit(string text, float x, float y, float width, float height)
    {
        LineEdit lineEdit = new()
        {
            Text = text,
            Position = new Vector2(x, y),
            Size = new Vector2(width, height)
        };
        AddChild(lineEdit);
        return lineEdit;
    }

    private Button AddButton(string text, float x, float y, float width, float height)
    {
        Button button = new()
        {
            Text = text,
            Position = new Vector2(x, y),
            Size = new Vector2(width, height),
            CustomMinimumSize = new Vector2(width, height),
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(button);
        return button;
    }

    private void CreateLocalRoom()
    {
        try
        {
            string seed = ReadSeed();
            RaceConfig config = RaceConfig.Default() with
            {
                RunSeed = seed,
                GameBuild = "local-sts2",
                GameBranch = "macos-local",
                GameplayModHash = "local-party-race",
                PartyRaceModVersion = PartyRaceConstants.ModVersion,
                ProtocolVersion = PartyRaceConstants.ProtocolVersion
            };

            string localPlayerId = PartyRaceSts2Context.LocalPlayerId;
            RacePlayer host = CreatePlayer(localPlayerId, PartyRaceSts2Context.IsHost ? "Host" : "Client");
            _room = _roomManager.CreateRoom(
                $"sts2:{PartyRaceSts2Context.LocalLobbyId}",
                ReadRoomName(),
                host,
                config);
            _roomManager.CreateTeam(_room, localPlayerId, PartyRaceSts2Context.IsHost ? "Host Team" : "Client Team");
            PartyRaceLog.Append($"Created local Party Race room seed={seed}.");
            PublishTeamUpdate();
            Refresh("Local room created.");
        }
        catch (Exception exception)
        {
            RefreshError("Could not create room", exception);
        }
    }

    private void ToggleHostReady()
    {
        if (_room is null)
        {
            Refresh("Create a room before readying.");
            return;
        }

        try
        {
            RacePlayer localPlayer = EnsureLocalPlayerInRoom();
            bool nextReady = !localPlayer.IsReady;
            _roomManager.SetPlayerReady(_room, localPlayer.PlayerId, localPlayer.CharacterId ?? "ironclad", nextReady);
            PartyRaceLog.Append($"Host ready changed ready={nextReady}.");
            PublishReadyUpdate(localPlayer.PlayerId, localPlayer.CharacterId ?? "ironclad", nextReady);
            PublishTeamUpdate();
            Refresh(nextReady ? "Host is ready." : "Host is not ready.");
        }
        catch (Exception exception)
        {
            RefreshError("Could not change ready state", exception);
        }
    }

    private void AddDemoRival()
    {
        if (_room is null)
        {
            Refresh("Create a room before adding a rival.");
            return;
        }

        try
        {
            if (_room.Players.Any(player => player.PlayerId == RivalPlayerId))
            {
                Refresh("Demo rival is already in the room.");
                return;
            }

            _roomManager.JoinRoom(_room, CreatePlayer(RivalPlayerId, "Demo Rival"));
            _roomManager.CreateTeam(_room, RivalPlayerId, "Rival Team");
            _roomManager.SetPlayerReady(_room, RivalPlayerId, "silent", true);
            PartyRaceLog.Append("Added ready demo rival.");
            PublishReadyUpdate(RivalPlayerId, "silent", isReady: true);
            PublishTeamUpdate();
            Refresh("Demo rival added and readied.");
        }
        catch (Exception exception)
        {
            RefreshError("Could not add demo rival", exception);
        }
    }

    private void StartRace()
    {
        if (_room is null)
        {
            Refresh("Create a room before starting.");
            return;
        }

        try
        {
            RaceStartPlan startPlan = _roomManager.StartRace(_room);
            PartyRaceLog.Append($"Local Party Race started seed={startPlan.RunSeed} hash={startPlan.RaceConfigHash}.");
            PublishRaceStart(startPlan);
            Refresh($"Race start plan created. Seed {startPlan.RunSeed}");
        }
        catch (Exception exception)
        {
            RefreshError("Could not start race", exception);
        }
    }

    private string ReadRoomName()
    {
        string? value = _roomNameInput?.Text?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Local Party Race" : value;
    }

    private string ReadSeed()
    {
        string? value = _seedInput?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = _seedService.GenerateSharedRandomSeed();
            if (_seedInput is not null)
            {
                _seedInput.Text = value;
            }
        }

        _seedService.Validate(value);
        return value;
    }

    private static RacePlayer CreatePlayer(string id, string displayName)
    {
        return new RacePlayer
        {
            PlayerId = id,
            DisplayName = displayName,
            ModVersion = PartyRaceConstants.ModVersion,
            ProtocolVersion = PartyRaceConstants.ProtocolVersion,
            GameBuild = "local-sts2",
            GameplayModHash = "local-party-race"
        };
    }

    private RacePlayer EnsureLocalPlayerInRoom()
    {
        if (_room is null)
        {
            throw new InvalidOperationException("Room is not created.");
        }

        string localPlayerId = PartyRaceSts2Context.LocalPlayerId;
        RacePlayer? localPlayer = _room.Players.FirstOrDefault(player => player.PlayerId == localPlayerId);
        if (localPlayer is null)
        {
            localPlayer = CreatePlayer(localPlayerId, PartyRaceSts2Context.IsHost ? "Host" : "Client");
            _roomManager.JoinRoom(_room, localPlayer);
        }

        if (localPlayer.TeamId is null)
        {
            _roomManager.CreateTeam(_room, localPlayerId, PartyRaceSts2Context.IsHost ? "Host Team" : "Client Team");
        }

        return localPlayer;
    }

    private void PublishReadyUpdate(string playerId, string characterId, bool isReady)
    {
        if (_room is null)
        {
            return;
        }

        ReadyUpdateMessage message = new(_room.RoomId, PartyRaceSts2Context.LocalPlayerId, DateTimeOffset.UtcNow, playerId, isReady, characterId);
        SendMessageForCurrentRole(message, shouldBuffer: true);
    }

    private void PublishTeamUpdate()
    {
        if (_room is null)
        {
            return;
        }

        TeamUpdateMessage message = new(_room.RoomId, PartyRaceSts2Context.LocalPlayerId, DateTimeOffset.UtcNow, _room.Teams.ToArray());
        SendMessageForCurrentRole(message, shouldBuffer: true);
    }

    private void PublishRaceStart(RaceStartPlan startPlan)
    {
        RaceStartMessage message = new(startPlan.RoomId, PartyRaceSts2Context.LocalPlayerId, DateTimeOffset.UtcNow, startPlan.RunSeed, startPlan.RaceConfigHash);
        SendMessageForCurrentRole(message, shouldBuffer: true);
    }

    private static void SendMessageForCurrentRole(RaceMessage message, bool shouldBuffer)
    {
        if (PartyRaceSts2Context.IsHost)
        {
            PartyRaceSts2Context.BroadcastFromHost(message, shouldBuffer);
        }
        else
        {
            PartyRaceSts2Context.SendToHost(message);
        }
    }

    private void OnPartyRaceMessageReceived(RaceMessage message, ulong senderId)
    {
        if (message.SenderPlayerId == PartyRaceSts2Context.LocalPlayerId)
        {
            return;
        }

        try
        {
            switch (message)
            {
                case TeamUpdateMessage teamUpdate:
                    ApplyTeamUpdate(teamUpdate, senderId);
                    break;
                case ReadyUpdateMessage readyUpdate:
                    ApplyReadyUpdate(readyUpdate, senderId);
                    break;
                case RaceStartMessage raceStart:
                    ApplyRaceStart(raceStart, senderId);
                    break;
            }
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to apply Party Race message kind={message.GetType().Name}: {exception}");
        }
    }

    private void ApplyTeamUpdate(TeamUpdateMessage message, ulong senderId)
    {
        RaceRoom room = EnsureNetworkRoom(message.RoomId, message.SenderPlayerId);
        room.Teams.Clear();
        foreach (RaceTeam incoming in message.Teams)
        {
            RaceTeam team = new()
            {
                TeamId = incoming.TeamId,
                TeamName = incoming.TeamName,
                TeamLeaderPlayerId = incoming.TeamLeaderPlayerId,
                ReadyState = incoming.ReadyState,
                RunState = incoming.RunState,
                LatestProgress = incoming.LatestProgress
            };

            foreach (string playerId in incoming.PlayerIds)
            {
                EnsureNetworkPlayer(room, playerId);
                team.PlayerIds.Add(playerId);
                room.GetPlayer(playerId).TeamId = team.TeamId;
            }

            room.Teams.Add(team);
        }

        room.EventLog.Add(new RaceEvent(_clock.UtcNow, "NetworkTeamUpdate", $"Team update received from {senderId}."));
        PartyRaceLog.Append($"Applied TeamUpdate from sender={senderId} teams={room.Teams.Count} room={message.RoomId}.");
        Refresh("Network team update received.");
    }

    private void ApplyReadyUpdate(ReadyUpdateMessage message, ulong senderId)
    {
        RaceRoom room = EnsureNetworkRoom(message.RoomId, message.SenderPlayerId);
        RacePlayer player = EnsureNetworkPlayer(room, message.PlayerId);
        player.CharacterId = message.CharacterId;
        player.IsReady = message.IsReady;
        if (player.TeamId is not null)
        {
            RaceTeam team = room.GetTeam(player.TeamId);
            team.ReadyState = team.PlayerIds.Count > 0 && team.PlayerIds.All(id => room.GetPlayer(id).IsReady)
                ? TeamReadyState.Ready
                : TeamReadyState.NotReady;
        }

        room.EventLog.Add(new RaceEvent(_clock.UtcNow, "NetworkReadyUpdate", $"Player '{message.PlayerId}' ready={message.IsReady} from {senderId}."));
        PartyRaceLog.Append($"Applied ReadyUpdate from sender={senderId} player={message.PlayerId} ready={message.IsReady} room={message.RoomId}.");
        Refresh("Network ready update received.");
    }

    private void ApplyRaceStart(RaceStartMessage message, ulong senderId)
    {
        RaceRoom room = EnsureNetworkRoom(message.RoomId, message.SenderPlayerId);
        room.State = RaceState.Running;
        room.StartedAt = _clock.UtcNow;
        room.EventLog.Add(new RaceEvent(_clock.UtcNow, "NetworkRaceStart", $"Race start received seed={message.RunSeed} from {senderId}."));
        PartyRaceLog.Append($"Applied RaceStart from sender={senderId} seed={message.RunSeed} hash={message.RaceConfigHash} room={message.RoomId}.");
        Refresh($"Network race start received. Seed {message.RunSeed}");
    }

    private RaceRoom EnsureNetworkRoom(string roomId, string hostPlayerId)
    {
        if (_room is not null && string.Equals(_room.RoomId, roomId, StringComparison.Ordinal))
        {
            return _room;
        }

        _room = new RaceRoom
        {
            RoomId = roomId,
            RoomName = "Network Party Race",
            HostPlayerId = hostPlayerId,
            CreatedAt = _clock.UtcNow,
            Config = RaceConfig.Default() with
            {
                GameBuild = "local-sts2",
                GameBranch = "network",
                GameplayModHash = "local-party-race",
                PartyRaceModVersion = PartyRaceConstants.ModVersion,
                ProtocolVersion = PartyRaceConstants.ProtocolVersion
            }
        };
        EnsureNetworkPlayer(_room, hostPlayerId);
        PartyRaceLog.Append($"Created network Party Race room mirror room={roomId} host={hostPlayerId}.");
        return _room;
    }

    private static RacePlayer EnsureNetworkPlayer(RaceRoom room, string playerId)
    {
        RacePlayer? player = room.Players.FirstOrDefault(existing => existing.PlayerId == playerId);
        if (player is not null)
        {
            return player;
        }

        player = CreatePlayer(playerId, playerId == PartyRaceSts2Context.LocalPlayerId ? "Local" : $"Player {playerId}");
        room.Players.Add(player);
        return player;
    }

    private void Refresh(string? status = null)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = status ?? BuildStatusText();
        }

        if (_teamsLabel is not null)
        {
            _teamsLabel.Text = BuildTeamsText();
        }

        if (_eventsLabel is not null)
        {
            _eventsLabel.Text = BuildEventsText();
        }

        bool hasRoom = _room is not null;
        if (_readyButton is not null)
        {
            _readyButton.Disabled = !hasRoom || _room!.State != RaceState.Lobby;
        }

        if (_addRivalButton is not null)
        {
            _addRivalButton.Disabled = !hasRoom || _room!.State != RaceState.Lobby;
        }

        if (_startButton is not null)
        {
            _startButton.Disabled = !hasRoom || _room!.State != RaceState.Lobby;
        }
    }

    private void RefreshError(string prefix, Exception exception)
    {
        string message = exception is PartyRaceException partyRaceException
            ? $"{prefix}: {partyRaceException.Code}"
            : $"{prefix}: {exception.Message}";
        PartyRaceLog.Append(message);
        Refresh(message);
    }

    private string BuildStatusText()
    {
        if (_room is null)
        {
            return $"No room. Create a local room to begin.{System.Environment.NewLine}{PartyRaceSts2Context.StatusText}";
        }

        return $"Room {_room.RoomName} | State {_room.State} | Hash {_room.Config.RaceConfigHash}{System.Environment.NewLine}{PartyRaceSts2Context.StatusText}";
    }

    private string BuildTeamsText()
    {
        if (_room is null || _room.Teams.Count == 0)
        {
            return "Teams: none";
        }

        IEnumerable<string> teams = _room.Teams.Select(team =>
        {
            string players = string.Join(", ", team.PlayerIds.Select(id =>
            {
                RacePlayer player = _room.GetPlayer(id);
                return player.IsReady ? $"{player.DisplayName}:ready" : $"{player.DisplayName}:not ready";
            }));
            return $"{team.TeamName} [{team.ReadyState}] {players}";
        });
        return $"Teams:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, teams)}";
    }

    private string BuildEventsText()
    {
        if (_room is null || _room.EventLog.Count == 0)
        {
            return "Events: none";
        }

        IEnumerable<string> events = _room.EventLog
            .TakeLast(4)
            .Select(entry => $"{entry.Type}: {entry.Message}");
        return $"Events:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, events)}";
    }
}
