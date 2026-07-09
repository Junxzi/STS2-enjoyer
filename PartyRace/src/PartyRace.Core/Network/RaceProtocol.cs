using PartyRace.Core.Domain;

namespace PartyRace.Core.Network;

public abstract record RaceMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt);

public sealed record RoomJoinMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, RacePlayer Player)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record TeamUpdateMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, IReadOnlyList<RaceTeam> Teams)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record ReadyUpdateMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, string PlayerId, bool IsReady, string CharacterId)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record RaceStartMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, string RunSeed, string RaceConfigHash)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record RunSessionStartMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, string TeamId, string RunSeed, string RaceConfigHash)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record LaunchReadyMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, string TeamId)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record TeamProgressUpdateMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, TeamProgress Progress)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public sealed record RaceFinishedMessage(string RoomId, string SenderPlayerId, DateTimeOffset SentAt, IReadOnlyList<RaceResultRow> Results)
    : RaceMessage(RoomId, SenderPlayerId, SentAt);

public interface IPartyRaceTransport
{
    void SendToHost(RaceMessage message);
    void BroadcastFromHost(RaceMessage message);
}

public sealed class InMemoryHostRelayTransport : IPartyRaceTransport
{
    public List<RaceMessage> HostInbox { get; } = [];
    public List<RaceMessage> Broadcasts { get; } = [];

    public void SendToHost(RaceMessage message)
    {
        HostInbox.Add(message);
    }

    public void BroadcastFromHost(RaceMessage message)
    {
        Broadcasts.Add(message);
    }
}
