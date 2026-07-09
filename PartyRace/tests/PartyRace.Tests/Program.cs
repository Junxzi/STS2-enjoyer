using PartyRace.Core.Compatibility;
using PartyRace.Core.Core;
using PartyRace.Core.Domain;
using PartyRace.Core.Hub;
using PartyRace.Core.Network;
using PartyRace.Core.Progress;
using PartyRace.Core.RunSession;
using PartyRace.Core.UI;
using PartyRace.Sts2Adapter;

List<(string Name, Action Test)> tests =
[
    ("MVP room can start with two ready teams and shared seed", TestMvpRoomStart),
    ("Race config hash mismatch blocks start", TestHashMismatchBlocksStart),
    ("Compatibility mismatch rejects join", TestCompatibilityRejectsJoin),
    ("Progress updates drive overlay rows", TestProgressOverlay),
    ("Result calculator ranks victory before deeper death", TestResultRanking),
    ("Seed injection failure returns manual fallback", TestSeedFallback),
    ("Heartbeat status warns and disconnects", TestHeartbeatStatus),
    ("Party Race network codec round-trips core messages", TestPartyRaceNetworkCodec),
    ("Local STS2 install exposes adapter hook prerequisites", TestLocalSts2HookProbe)
];

int passed = 0;
foreach ((string name, Action test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
        passed++;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine(exception);
        Environment.ExitCode = 1;
        break;
    }
}

if (Environment.ExitCode == 0)
{
    Console.WriteLine($"{passed}/{tests.Count} tests passed.");
}

static RaceRoomManager CreateManager(ManualClock clock)
{
    return new RaceRoomManager(clock, new RaceConfigValidator(), new SeedService(), new CompatibilityService());
}

static RaceConfig CreateConfig(string seed = "K9M2AB8Q4Z7L")
{
    RaceConfig config = RaceConfig.Default() with
    {
        RunSeed = seed,
        GameBuild = "ea-0.108.0",
        GameBranch = "default",
        GameplayModHash = "gameplay-A",
        PartyRaceModVersion = PartyRaceConstants.ModVersion,
        ProtocolVersion = PartyRaceConstants.ProtocolVersion
    };

    return new RaceConfigValidator().WithComputedHash(config);
}

static RacePlayer CreatePlayer(string id, string build = "ea-0.108.0", string gameplayHash = "gameplay-A")
{
    return new RacePlayer
    {
        PlayerId = id,
        DisplayName = id,
        ModVersion = PartyRaceConstants.ModVersion,
        ProtocolVersion = PartyRaceConstants.ProtocolVersion,
        GameBuild = build,
        GameplayModHash = gameplayHash
    };
}

static RaceRoom CreateReadyTwoTeamRoom(ManualClock clock)
{
    RaceRoomManager manager = CreateManager(clock);
    RaceRoom room = manager.CreateRoom("room-1", "PoC Room", CreatePlayer("host"), CreateConfig());
    manager.JoinRoom(room, CreatePlayer("guest"));

    RaceTeam teamA = manager.CreateTeam(room, "host", "Team A");
    RaceTeam teamB = manager.CreateTeam(room, "guest", "Team B");
    manager.SetPlayerReady(room, "host", "ironclad", true);
    manager.SetPlayerReady(room, "guest", "silent", true);

    AssertEqual(TeamReadyState.Ready, teamA.ReadyState, "Team A should be ready.");
    AssertEqual(TeamReadyState.Ready, teamB.ReadyState, "Team B should be ready.");
    return room;
}

static void TestMvpRoomStart()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoom room = CreateReadyTwoTeamRoom(clock);

    RaceStartPlan plan = CreateManager(clock).StartRace(room);

    AssertEqual(RaceState.Running, room.State, "Race should enter running state.");
    AssertEqual("K9M2AB8Q4Z7L", plan.RunSeed, "Start plan should contain shared seed.");
    AssertEqual(2, plan.TeamIds.Count, "Both teams should receive run start.");
    AssertTrue(room.Teams.All(team => team.RunState == RunSessionState.Launching), "Teams should be launching.");
}

static void TestHashMismatchBlocksStart()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoom room = CreateReadyTwoTeamRoom(clock);
    room.Config = room.Config with { Ascension = 2 };

    PartyRaceException exception = AssertThrows<PartyRaceException>(() => CreateManager(clock).StartRace(room));
    AssertEqual(PartyRaceErrorCode.RaceConfigHashMismatch, exception.Code, "Hash mismatch should block start.");
}

static void TestCompatibilityRejectsJoin()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoomManager manager = CreateManager(clock);
    RaceRoom room = manager.CreateRoom("room-1", "PoC Room", CreatePlayer("host"), CreateConfig());

    PartyRaceException exception = AssertThrows<PartyRaceException>(() => manager.JoinRoom(room, CreatePlayer("bad-build", build: "ea-0.109.0")));
    AssertEqual(PartyRaceErrorCode.GameBuildMismatch, exception.Code, "Game build mismatch should reject join.");
}

static void TestProgressOverlay()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoom room = CreateReadyTwoTeamRoom(clock);
    ProgressTracker tracker = new(clock, new ChecksumBuilder());

    tracker.AcceptUpdate(room, new TeamProgress("team_01", 1, 7, RoomType.Monster, RacePhase.Combat, 0, 1, 1, 42_000, false, false, false, false, ""));
    tracker.AcceptUpdate(room, new TeamProgress("team_02", 1, 5, RoomType.Event, RacePhase.EventChoice, 0, 1, 1, 40_000, false, false, false, false, ""));

    IReadOnlyList<OverlayRow> rows = new OverlayPresenter().BuildRows(room);

    AssertEqual(2, rows.Count, "Overlay should have both teams.");
    AssertEqual("Team A", rows[0].TeamName, "Higher floor should rank first.");
    AssertEqual(7, rows[0].Floor, "Overlay should expose floor.");
}

static void TestResultRanking()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoom room = CreateReadyTwoTeamRoom(clock);
    ProgressTracker tracker = new(clock, new ChecksumBuilder());

    tracker.AcceptUpdate(room, new TeamProgress("team_01", 1, 10, RoomType.Death, RacePhase.Dead, 0, 0, 1, 60_000, false, true, false, false, ""));
    tracker.AcceptUpdate(room, new TeamProgress("team_02", 1, 3, RoomType.Victory, RacePhase.Victory, 1, 1, 1, 90_000, true, false, false, false, ""));

    IReadOnlyList<RaceResultRow> rows = new RaceResultCalculator().Calculate(room);

    AssertEqual("Team B", rows[0].TeamName, "Victory should outrank a deeper death.");
    AssertEqual(ResultState.Victory, rows[0].ResultState, "Winner should be victory.");
}

static void TestSeedFallback()
{
    RunSessionCoordinator coordinator = new(new FailingSeedInjector(), new SuccessfulRunLauncher());
    RunLaunchResult result = coordinator.PrepareTeamRun(new RaceTeam { TeamId = "team_01", TeamName = "Team A", TeamLeaderPlayerId = "host" }, CreateConfig());

    AssertTrue(!result.Succeeded, "Launch should not succeed.");
    AssertTrue(result.RequiresManualFallback, "Seed failure should require manual fallback.");
}

static void TestHeartbeatStatus()
{
    ManualClock clock = new(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));
    RaceRoom room = CreateReadyTwoTeamRoom(clock);
    ProgressTracker tracker = new(clock, new ChecksumBuilder());
    tracker.AcceptUpdate(room, new TeamProgress("team_01", 1, 1, RoomType.Start, RacePhase.Map, 0, 1, 1, 0, false, false, false, false, ""));

    clock.Advance(TimeSpan.FromSeconds(11));
    TeamConnectionStatus warning = tracker.GetConnectionStatuses(room).Single(status => status.TeamId == "team_01");
    AssertTrue(warning.ShouldWarn, "Heartbeat should warn after timeout.");
    AssertTrue(!warning.IsDisconnected, "Heartbeat should not disconnect before disconnect threshold.");

    clock.Advance(TimeSpan.FromSeconds(20));
    TeamConnectionStatus disconnected = tracker.GetConnectionStatuses(room).Single(status => status.TeamId == "team_01");
    AssertTrue(disconnected.IsDisconnected, "Heartbeat should disconnect after threshold.");
}

static void TestPartyRaceNetworkCodec()
{
    DateTimeOffset sentAt = new(2026, 7, 9, 0, 0, 0, TimeSpan.Zero);
    RaceMessage[] messages =
    [
        new ReadyUpdateMessage("room-1", "host", sentAt, "host", true, "ironclad"),
        new RaceStartMessage("room-1", "host", sentAt, "K9M2AB8Q4Z7L", "ABCD-1234-FEED"),
        new TeamProgressUpdateMessage(
            "room-1",
            "host",
            sentAt,
            new TeamProgress("team_01", 1, 4, RoomType.Elite, RacePhase.Combat, 0, 1, 1, 12_345, false, false, false, false, "checksum"))
    ];

    foreach (RaceMessage message in messages)
    {
        EncodedPartyRaceMessage encoded = PartyRaceMessageCodec.Encode(message);
        RaceMessage decoded = PartyRaceMessageCodec.Decode(encoded);

        AssertEqual(message.GetType(), decoded.GetType(), "Decoded message type should match.");
        AssertEqual(message.RoomId, decoded.RoomId, "Room id should round-trip.");
        AssertEqual(message.SenderPlayerId, decoded.SenderPlayerId, "Sender should round-trip.");
        AssertEqual(message.SentAt, decoded.SentAt, "Sent timestamp should round-trip.");
    }
}

static void TestLocalSts2HookProbe()
{
    Sts2GameInfo info;
    try
    {
        info = Sts2GameInfo.FromKnownInstall();
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("SKIP local STS2 install not found.");
        return;
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine("SKIP local STS2 install not found.");
        return;
    }

    if (!Directory.Exists(info.InstallPath))
    {
        Console.WriteLine("SKIP local STS2 install not found.");
        return;
    }

    Sts2HookReadiness readiness = new Sts2HookProbe().Probe(info);

    AssertEqual("v0.107.1", info.Version, "Observed local STS2 version should match release_info.json.");
    AssertTrue(readiness.HasModsDirectory, "STS2 install should have a mods directory.");
    AssertTrue(readiness.HasHarmony, "STS2 install should include Harmony.");
    AssertTrue(readiness.HasSteamworks, "STS2 install should include Steamworks.NET.");
    AssertTrue(readiness.HasModFileIoInterface, "STS2 assembly should expose mod file I/O interface.");
    AssertTrue(readiness.HasNetGameService, "STS2 assembly should expose network game service.");
    AssertTrue(readiness.HasRunLobbyListeners, "STS2 assembly should expose run lobby listeners.");
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

sealed class FailingSeedInjector : ISeedInjector
{
    public SeedInjectionResult TryInjectSeed(string runSeed)
    {
        return SeedInjectionResult.Failure("STS2 seed hook unavailable.");
    }
}

sealed class SuccessfulRunLauncher : IRunLauncher
{
    public RunLaunchResult TryLaunch(RaceTeam team, RaceConfig config)
    {
        return RunLaunchResult.Success();
    }
}
