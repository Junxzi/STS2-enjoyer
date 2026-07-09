using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Core.Domain;
using PartyRace.Core.Core;
using PartyRace.Core.Network;
using System.Collections;
using System.Reflection;

namespace PartyRace.Mod;

internal static class PartyRaceSts2Context
{
    private static MessageHandlerDelegate<PartyRaceNetEnvelope>? s_messageHandler;

    public static event Action<RaceMessage, ulong>? MessageReceived;
    public static event Action<string, string>? Sts2RunBegan;

    public static INetGameService? NetService { get; private set; }
    public static object? NetServiceOwner { get; private set; }
    public static object? CurrentRunState { get; private set; }
    public static bool IsPartyRaceRunArmed { get; private set; }
    public static string? ArmedRunSeed { get; private set; }
    public static string LastCaptureSource { get; private set; } = "none";
    public static string LastRunStateCaptureSource { get; private set; } = "none";
    public static string LocalPlayerId => NetService?.NetId.ToString() ?? "local_host";
    public static string LocalLobbyId => NetService is null ? "local" : TryGetLobbyId(NetService);
    public static bool IsHost => NetService?.Type == NetGameType.Host;

    public static string StatusText
    {
        get
        {
            if (NetService is null)
            {
                return "STS2 network: not captured";
            }

            string lobbyId = TryGetLobbyId(NetService);
            return $"STS2 network: {NetService.Type} id={NetService.NetId} connected={NetService.IsConnected} lobby={lobbyId}";
        }
    }

    public static void CaptureNetService(INetGameService netService, object owner, string source)
    {
        if (ReferenceEquals(NetService, netService))
        {
            NetServiceOwner = owner;
            LastCaptureSource = source;
            return;
        }

        UnregisterMessageHandler();
        NetService = netService;
        NetServiceOwner = owner;
        LastCaptureSource = source;

        PartyRaceLog.Append($"Captured STS2 net service from {source}: type={netService.Type} id={netService.NetId} lobby={TryGetLobbyId(netService)}.");

        try
        {
            s_messageHandler = HandleEnvelope;
            netService.RegisterMessageHandler(s_messageHandler);
            PartyRaceLog.Append("Registered Party Race STS2 net message handler.");
            SendHello(netService);
        }
        catch (Exception exception)
        {
            s_messageHandler = null;
            PartyRaceLog.Append($"Captured net service, but failed to register message handler: {exception}");
        }
    }

    public static void CaptureRunState(object? runState, string source)
    {
        if (runState is null)
        {
            return;
        }

        if (ReferenceEquals(CurrentRunState, runState))
        {
            LastRunStateCaptureSource = source;
            return;
        }

        CurrentRunState = runState;
        LastRunStateCaptureSource = source;
        PartyRaceLog.Append($"Captured STS2 run state from {source}: {runState.GetType().FullName}.");
    }

    public static bool TryBuildLocalProgress(string teamId, long elapsedMs, out TeamProgress progress, out string detail)
    {
        progress = new TeamProgress(teamId, 0, 0, RoomType.Unknown, RacePhase.Unknown, 0, 1, 1, elapsedMs, false, false, false, false, "");

        object? runState = CurrentRunState;
        if (runState is null)
        {
            detail = "STS2 run state is not captured.";
            return false;
        }

        try
        {
            int currentActIndex = Math.Max(0, TryGetIntProperty(runState, "CurrentActIndex") ?? 0);
            int act = currentActIndex + 1;
            int floor = Math.Max(
                TryGetIntProperty(runState, "TotalFloor") ?? 0,
                TryGetIntProperty(runState, "ActFloor") ?? 0);
            object? currentRoom = TryGetPropertyValue(runState, "CurrentRoom");
            string roomTypeName = TryGetPropertyValue(currentRoom, "RoomType")?.ToString() ?? "Unknown";
            RoomType roomType = MapRoomType(roomTypeName);
            RacePhase phase = MapRacePhase(roomType);
            bool isGameOver = TryGetBoolProperty(runState, "IsGameOver") == true;
            (int alivePlayers, int totalPlayers) = CountPlayers(runState);
            bool isDead = isGameOver && alivePlayers == 0;
            bool isVictory = roomType == RoomType.Victory;

            progress = new TeamProgress(
                teamId,
                act,
                floor,
                isVictory ? RoomType.Victory : isDead ? RoomType.Death : roomType,
                isVictory ? RacePhase.Victory : isDead ? RacePhase.Dead : phase,
                0,
                alivePlayers,
                totalPlayers,
                elapsedMs,
                isVictory,
                isDead,
                false,
                false,
                "");

            detail = $"act={progress.Act} floor={progress.Floor} room={progress.RoomType} phase={progress.Phase} alive={progress.AlivePlayers}/{progress.TotalPlayers}.";
            return true;
        }
        catch (Exception exception)
        {
            detail = $"{exception.GetType().Name}: {exception.Message}";
            PartyRaceLog.Append($"Failed to build Party Race local progress: {exception}");
            return false;
        }
    }

    public static bool TrySetLobbySeed(string runSeed, out string detail)
    {
        if (NetService is null)
        {
            detail = "STS2 network is not captured.";
            PartyRaceLog.Append($"Skipped STS2 lobby seed sync seed={runSeed}: {detail}");
            return false;
        }

        if (NetService.Type != NetGameType.Host)
        {
            detail = $"Only the host can set the STS2 lobby seed. Current role is {NetService.Type}.";
            PartyRaceLog.Append($"Skipped STS2 lobby seed sync seed={runSeed}: {detail}");
            return false;
        }

        if (NetServiceOwner is null)
        {
            detail = "STS2 lobby owner is not captured.";
            PartyRaceLog.Append($"Skipped STS2 lobby seed sync seed={runSeed}: {detail}");
            return false;
        }

        Type ownerType = NetServiceOwner.GetType();
        MethodInfo? setSeedMethod = ownerType.GetMethod(
            "SetSeed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        if (setSeedMethod is null)
        {
            detail = $"Captured STS2 lobby owner '{ownerType.FullName}' does not expose SetSeed(string).";
            PartyRaceLog.Append($"Skipped STS2 lobby seed sync seed={runSeed}: {detail}");
            return false;
        }

        try
        {
            setSeedMethod.Invoke(NetServiceOwner, [runSeed]);
            detail = $"STS2 lobby seed synced to {runSeed}.";
            PartyRaceLog.Append(detail);
            return true;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            if (exception.InnerException is NotImplementedException &&
                exception.InnerException.Message.Contains("standard mode", StringComparison.OrdinalIgnoreCase))
            {
                detail = "STS2 standard mode does not allow manual seed sync. Start the STS2 run normally; Party Race will follow the final STS2 seed.";
                PartyRaceLog.Append($"Skipped STS2 lobby seed sync seed={runSeed}: {detail}");
                return false;
            }

            detail = $"{exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
            PartyRaceLog.Append($"Failed to sync STS2 lobby seed seed={runSeed}: {exception.InnerException}");
            return false;
        }
        catch (Exception exception)
        {
            detail = $"{exception.GetType().Name}: {exception.Message}";
            PartyRaceLog.Append($"Failed to sync STS2 lobby seed seed={runSeed}: {exception}");
            return false;
        }
    }

    public static void ObserveSts2RunBegin(string seed, string source)
    {
        PartyRaceLog.Append($"Observed STS2 BeginRun seed={seed} source={source}.");
        Sts2RunBegan?.Invoke(seed, source);
    }

    public static void ArmPartyRaceRun(string seed)
    {
        IsPartyRaceRunArmed = true;
        ArmedRunSeed = seed;
        PartyRaceLog.Append($"Armed Party Race local run launch seed={seed}.");
    }

    public static void ClearPartyRaceRunArm(string reason)
    {
        if (!IsPartyRaceRunArmed && string.IsNullOrWhiteSpace(ArmedRunSeed))
        {
            return;
        }

        string seed = ArmedRunSeed ?? "unknown";
        IsPartyRaceRunArmed = false;
        ArmedRunSeed = null;
        PartyRaceLog.Append($"Cleared Party Race local run launch arm seed={seed}: {reason}.");
    }

    private static void HandleEnvelope(PartyRaceNetEnvelope envelope, ulong senderId)
    {
        try
        {
            RaceMessage message = envelope.ToMessage();
            PartyRaceLog.Append($"Received Party Race net message kind={message.GetType().Name} sender={senderId} room={message.RoomId}.");
            if (message is PartyRaceHelloMessage hello)
            {
                PartyRaceLog.Append($"Party Race hello details sender={hello.SenderPlayerId} netType={hello.NetGameType} lobby={hello.LobbyId} gameBuild={hello.GameBuild} mod={hello.ModVersion} protocol={hello.ProtocolVersion}.");
            }

            MessageReceived?.Invoke(message, senderId);
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to read Party Race net message from sender={senderId}: {exception}");
        }
    }

    private static void SendHello(INetGameService netService)
    {
        string lobbyId = TryGetLobbyId(netService);
        PartyRaceHelloMessage hello = new(
            RoomId: $"sts2:{lobbyId}",
            SenderPlayerId: netService.NetId.ToString(),
            SentAt: DateTimeOffset.UtcNow,
            NetGameType: netService.Type.ToString(),
            LobbyId: lobbyId,
            GameBuild: "v0.107.1",
            ModVersion: PartyRaceConstants.ModVersion,
            ProtocolVersion: PartyRaceConstants.ProtocolVersion);

        bool isHost = netService.Type == NetGameType.Host;
        PartyRaceNetEnvelope envelope = PartyRaceNetEnvelope.FromMessage(
            hello,
            shouldBroadcast: isHost,
            shouldBuffer: isHost);

        netService.SendMessage(envelope);
        PartyRaceLog.Append($"Sent Party Race hello netType={hello.NetGameType} lobby={hello.LobbyId} broadcast={envelope.ShouldBroadcast} buffer={envelope.ShouldBuffer}.");
    }

    private static object? TryGetPropertyValue(object? target, string propertyName)
    {
        if (target is null)
        {
            return null;
        }

        try
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(target);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryGetIntProperty(object target, string propertyName)
    {
        object? value = TryGetPropertyValue(target, propertyName);
        return value is int intValue ? intValue : null;
    }

    private static bool? TryGetBoolProperty(object target, string propertyName)
    {
        object? value = TryGetPropertyValue(target, propertyName);
        return value is bool boolValue ? boolValue : null;
    }

    private static (int AlivePlayers, int TotalPlayers) CountPlayers(object runState)
    {
        if (TryGetPropertyValue(runState, "Players") is not IEnumerable players)
        {
            return (1, 1);
        }

        int total = 0;
        int alive = 0;
        foreach (object? player in players)
        {
            if (player is null)
            {
                continue;
            }

            total++;
            object? creature = TryGetPropertyValue(player, "Creature");
            if (TryGetBoolProperty(creature ?? player, "IsAlive") != false)
            {
                alive++;
            }
        }

        return total == 0 ? (1, 1) : (Math.Max(0, alive), total);
    }

    private static RoomType MapRoomType(string roomTypeName)
    {
        return roomTypeName switch
        {
            "Monster" => RoomType.Monster,
            "Elite" => RoomType.Elite,
            "Boss" => RoomType.Boss,
            "Event" => RoomType.Event,
            "Shop" => RoomType.Shop,
            "RestSite" => RoomType.Rest,
            "Treasure" => RoomType.Treasure,
            _ => RoomType.Unknown
        };
    }

    private static RacePhase MapRacePhase(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Monster or RoomType.Elite or RoomType.Boss => RacePhase.Combat,
            RoomType.Event => RacePhase.EventChoice,
            RoomType.Shop => RacePhase.Shopping,
            RoomType.Rest => RacePhase.Resting,
            RoomType.Treasure or RoomType.BossTreasure => RacePhase.Reward,
            _ => RacePhase.Map
        };
    }

    public static bool SendToHost(RaceMessage message)
    {
        return Send(message, shouldBroadcast: false, shouldBuffer: false);
    }

    public static bool BroadcastFromHost(RaceMessage message, bool shouldBuffer = true)
    {
        return Send(message, shouldBroadcast: true, shouldBuffer: shouldBuffer);
    }

    public static bool Send(RaceMessage message, bool shouldBroadcast, bool shouldBuffer)
    {
        if (NetService is null)
        {
            PartyRaceLog.Append($"Skipped Party Race net send kind={message.GetType().Name}: STS2 net service is not captured.");
            return false;
        }

        try
        {
            PartyRaceNetEnvelope envelope = PartyRaceNetEnvelope.FromMessage(message, shouldBroadcast, shouldBuffer);
            NetService.SendMessage(envelope);
            PartyRaceLog.Append($"Sent Party Race net message kind={message.GetType().Name} room={message.RoomId} broadcast={shouldBroadcast} buffer={shouldBuffer}.");
            return true;
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to send Party Race net message kind={message.GetType().Name}: {exception}");
            return false;
        }
    }

    private static void UnregisterMessageHandler()
    {
        if (NetService is null || s_messageHandler is null)
        {
            return;
        }

        try
        {
            NetService.UnregisterMessageHandler(s_messageHandler);
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to unregister previous Party Race message handler: {exception}");
        }

        s_messageHandler = null;
    }

    private static string TryGetLobbyId(INetGameService netService)
    {
        try
        {
            return netService.GetRawLobbyIdentifier() ?? "null";
        }
        catch (Exception exception)
        {
            return $"unavailable:{exception.GetType().Name}";
        }
    }
}
