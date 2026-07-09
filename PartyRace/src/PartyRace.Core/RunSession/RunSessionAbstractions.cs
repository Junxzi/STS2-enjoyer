using PartyRace.Core.Domain;

namespace PartyRace.Core.RunSession;

public interface ISeedInjector
{
    SeedInjectionResult TryInjectSeed(string runSeed);
}

public sealed record SeedInjectionResult(bool Succeeded, string? FailureReason = null)
{
    public static SeedInjectionResult Success() => new(true);

    public static SeedInjectionResult Failure(string reason) => new(false, reason);
}

public interface IRunLauncher
{
    RunLaunchResult TryLaunch(RaceTeam team, RaceConfig config);
}

public sealed record RunLaunchResult(bool Succeeded, bool RequiresManualFallback, string? FailureReason = null)
{
    public static RunLaunchResult Success() => new(true, false);

    public static RunLaunchResult ManualFallback(string reason) => new(false, true, reason);

    public static RunLaunchResult Failure(string reason) => new(false, false, reason);
}

public sealed class RunSessionCoordinator(ISeedInjector seedInjector, IRunLauncher runLauncher)
{
    public RunLaunchResult PrepareTeamRun(RaceTeam team, RaceConfig config)
    {
        SeedInjectionResult seedResult = seedInjector.TryInjectSeed(config.RunSeed ?? string.Empty);
        if (!seedResult.Succeeded)
        {
            return RunLaunchResult.ManualFallback(seedResult.FailureReason ?? "Seed injection failed.");
        }

        return runLauncher.TryLaunch(team, config);
    }
}
