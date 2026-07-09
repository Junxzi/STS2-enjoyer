namespace PartyRace.Core.Core;

public interface IPartyRaceLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

public sealed class NullPartyRaceLogger : IPartyRaceLogger
{
    public static NullPartyRaceLogger Instance { get; } = new();

    private NullPartyRaceLogger()
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
