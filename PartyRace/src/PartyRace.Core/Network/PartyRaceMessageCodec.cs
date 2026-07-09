using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyRace.Core.Network;

public enum PartyRaceMessageKind
{
    PartyRaceHello = 0,
    RoomJoin = 1,
    TeamUpdate = 2,
    ReadyUpdate = 3,
    RaceStart = 4,
    RunSessionStart = 5,
    LaunchReady = 6,
    TeamProgressUpdate = 7,
    RaceFinished = 8
}

public sealed record EncodedPartyRaceMessage(PartyRaceMessageKind Kind, string PayloadJson);

public static class PartyRaceMessageCodec
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static EncodedPartyRaceMessage Encode(RaceMessage message)
    {
        return message switch
        {
            PartyRaceHelloMessage typed => Encode(PartyRaceMessageKind.PartyRaceHello, typed),
            RoomJoinMessage typed => Encode(PartyRaceMessageKind.RoomJoin, typed),
            TeamUpdateMessage typed => Encode(PartyRaceMessageKind.TeamUpdate, typed),
            ReadyUpdateMessage typed => Encode(PartyRaceMessageKind.ReadyUpdate, typed),
            RaceStartMessage typed => Encode(PartyRaceMessageKind.RaceStart, typed),
            RunSessionStartMessage typed => Encode(PartyRaceMessageKind.RunSessionStart, typed),
            LaunchReadyMessage typed => Encode(PartyRaceMessageKind.LaunchReady, typed),
            TeamProgressUpdateMessage typed => Encode(PartyRaceMessageKind.TeamProgressUpdate, typed),
            RaceFinishedMessage typed => Encode(PartyRaceMessageKind.RaceFinished, typed),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().FullName, "Unsupported Party Race message type.")
        };
    }

    public static RaceMessage Decode(EncodedPartyRaceMessage encoded)
    {
        return encoded.Kind switch
        {
            PartyRaceMessageKind.PartyRaceHello => Decode<PartyRaceHelloMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.RoomJoin => Decode<RoomJoinMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.TeamUpdate => Decode<TeamUpdateMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.ReadyUpdate => Decode<ReadyUpdateMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.RaceStart => Decode<RaceStartMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.RunSessionStart => Decode<RunSessionStartMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.LaunchReady => Decode<LaunchReadyMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.TeamProgressUpdate => Decode<TeamProgressUpdateMessage>(encoded.PayloadJson),
            PartyRaceMessageKind.RaceFinished => Decode<RaceFinishedMessage>(encoded.PayloadJson),
            _ => throw new ArgumentOutOfRangeException(nameof(encoded), encoded.Kind, "Unsupported Party Race message kind.")
        };
    }

    private static EncodedPartyRaceMessage Encode<T>(PartyRaceMessageKind kind, T message)
        where T : RaceMessage
    {
        return new EncodedPartyRaceMessage(kind, JsonSerializer.Serialize(message, Options));
    }

    private static T Decode<T>(string payloadJson)
        where T : RaceMessage
    {
        return JsonSerializer.Deserialize<T>(payloadJson, Options)
            ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            IncludeFields = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
