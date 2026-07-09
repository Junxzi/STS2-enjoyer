using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using PartyRace.Core.Core;
using PartyRace.Core.Network;

namespace PartyRace.Mod;

public sealed class PartyRaceNetEnvelope : INetMessage
{
    public int ProtocolVersion;
    public int Kind;
    public string PayloadJson = string.Empty;
    public bool BroadcastAfterReceive;
    public bool BufferForLateJoiners;

    public bool ShouldBroadcast => BroadcastAfterReceive;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;
    public bool ShouldBuffer => BufferForLateJoiners;

    public PartyRaceNetEnvelope()
    {
    }

    public static PartyRaceNetEnvelope FromMessage(RaceMessage message, bool shouldBroadcast, bool shouldBuffer)
    {
        EncodedPartyRaceMessage encoded = PartyRaceMessageCodec.Encode(message);
        return new PartyRaceNetEnvelope
        {
            ProtocolVersion = PartyRaceConstants.ProtocolVersion,
            Kind = (int)encoded.Kind,
            PayloadJson = encoded.PayloadJson,
            BroadcastAfterReceive = shouldBroadcast,
            BufferForLateJoiners = shouldBuffer
        };
    }

    public RaceMessage ToMessage()
    {
        if (ProtocolVersion != PartyRaceConstants.ProtocolVersion)
        {
            throw new InvalidOperationException($"Unsupported Party Race protocol version {ProtocolVersion}.");
        }

        return PartyRaceMessageCodec.Decode(new EncodedPartyRaceMessage((PartyRaceMessageKind)Kind, PayloadJson));
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ProtocolVersion, 16);
        writer.WriteInt(Kind, 8);
        writer.WriteBool(BroadcastAfterReceive);
        writer.WriteBool(BufferForLateJoiners);
        writer.WriteString(PayloadJson);
    }

    public void Deserialize(PacketReader reader)
    {
        ProtocolVersion = reader.ReadInt(16);
        Kind = reader.ReadInt(8);
        BroadcastAfterReceive = reader.ReadBool();
        BufferForLateJoiners = reader.ReadBool();
        PayloadJson = reader.ReadString();
    }
}
