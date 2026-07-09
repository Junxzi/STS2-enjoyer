namespace PartyRace.Core.Domain;

public enum RaceState
{
    Lobby,
    Countdown,
    Running,
    Paused,
    Finished,
    Cancelled
}

public enum TeamReadyState
{
    NotReady,
    Ready
}

public enum RunSessionState
{
    NotStarted,
    Launching,
    Running,
    Finished,
    Disconnected
}

public enum SeedMode
{
    SharedRandom,
    Manual,
    HiddenRandom,
    DailyStyle
}

public enum VisibilityRule
{
    Casual,
    FairRace,
    BlindRace,
    FullSpectator
}

public enum CharacterRule
{
    Any,
    UniquePerTeam,
    SameCharacter
}

public enum ModPolicy
{
    StrictGameplay,
    WarnCosmeticOnly
}

public enum TimerPolicy
{
    RealTime
}

public enum RoomType
{
    Unknown,
    Monster,
    Elite,
    Boss,
    Event,
    Shop,
    Rest,
    Treasure,
    BossTreasure,
    Start,
    Victory,
    Death
}

public enum RacePhase
{
    Unknown,
    Map,
    EnteringRoom,
    Combat,
    Reward,
    EventChoice,
    Shopping,
    Resting,
    Loading,
    Victory,
    Dead,
    Retired,
    Disconnected
}

public enum ResultState
{
    Running,
    Victory,
    Death,
    Retired,
    Dnf,
    Disqualified,
    Disconnected
}
