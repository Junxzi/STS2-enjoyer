using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Core.Core;
using PartyRace.Core.Network;

namespace PartyRace.Mod;

internal static class PartyRaceSts2Context
{
    private static MessageHandlerDelegate<PartyRaceNetEnvelope>? s_messageHandler;

    public static event Action<RaceMessage, ulong>? MessageReceived;

    public static INetGameService? NetService { get; private set; }
    public static string LastCaptureSource { get; private set; } = "none";
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

    public static void CaptureNetService(INetGameService netService, string source)
    {
        if (ReferenceEquals(NetService, netService))
        {
            LastCaptureSource = source;
            return;
        }

        UnregisterMessageHandler();
        NetService = netService;
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
