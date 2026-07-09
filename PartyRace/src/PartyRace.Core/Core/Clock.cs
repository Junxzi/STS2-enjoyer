namespace PartyRace.Core.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class ManualClock(DateTimeOffset initialTime) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = initialTime;

    public void Advance(TimeSpan interval)
    {
        UtcNow += interval;
    }
}
