using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Sts2Adapter;

namespace PartyRace.Mod;

internal static class PartyRaceSts2Context
{
    private static Sts2TransportAdapter? s_transport;

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

        s_transport?.Dispose();
        NetService = netService;
        LastCaptureSource = source;
        s_transport = new Sts2TransportAdapter(netService);
        s_transport.MessageReceived += (message, senderId) =>
            PartyRaceLog.Append($"Received Party Race net message kind={message.GetType().Name} sender={senderId} room={message.RoomId}.");

        PartyRaceLog.Append($"Captured STS2 net service from {source}: type={netService.Type} id={netService.NetId} lobby={TryGetLobbyId(netService)}.");
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
