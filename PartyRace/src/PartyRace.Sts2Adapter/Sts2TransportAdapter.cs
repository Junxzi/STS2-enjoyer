using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Core.Network;

namespace PartyRace.Sts2Adapter;

public sealed class Sts2TransportAdapter(INetGameService netGameService) : IPartyRaceTransport
{
    public INetGameService NetGameService { get; } = netGameService;

    public void SendToHost(RaceMessage message)
    {
        throw new NotSupportedException("STS2 packet serialization is not wired yet. Confirm INetMessage/IPacketSerializable details in a live integration pass first.");
    }

    public void BroadcastFromHost(RaceMessage message)
    {
        throw new NotSupportedException("STS2 packet serialization is not wired yet. Confirm INetMessage/IPacketSerializable details in a live integration pass first.");
    }
}
