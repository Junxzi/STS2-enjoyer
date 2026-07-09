namespace PartyRace.Core.Core;

public enum PartyRaceErrorCode
{
    ModNotInstalled,
    ModVersionMismatch,
    ProtocolMismatch,
    GameBuildMismatch,
    GameplayModMismatch,
    RoomFull,
    TeamFull,
    NoTeamSelected,
    TeamNotReady,
    SeedInjectionFailed,
    RunSessionCreateFailed,
    HubConnectionLost,
    TeamLeaderLost,
    ProgressSyncFailed,
    DesyncSuspected,
    DesyncConfirmed,
    ResultSubmitFailed,
    HostMigrationFailed,
    RaceConfigHashMismatch,
    LateJoinPlayDenied,
    InvalidRoomState,
    InvalidSeed,
    DuplicatePlayer,
    UnknownPlayer,
    UnknownTeam
}

public sealed class PartyRaceException(PartyRaceErrorCode code, string message) : InvalidOperationException(message)
{
    public PartyRaceErrorCode Code { get; } = code;
}
