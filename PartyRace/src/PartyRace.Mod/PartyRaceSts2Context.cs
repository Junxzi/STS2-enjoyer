using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Core.Network;

namespace PartyRace.Mod;

internal static class PartyRaceSts2Context
{
    private static MessageHandlerDelegate<PartyRaceNetEnvelope>? s_messageHandler;

    public static INetGameService? NetService { get; private set; }
    public static string LastCaptureSource { get; private set; } = "none";

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
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to read Party Race net message from sender={senderId}: {exception}");
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
