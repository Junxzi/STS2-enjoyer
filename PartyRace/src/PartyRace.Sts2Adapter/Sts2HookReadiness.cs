namespace PartyRace.Sts2Adapter;

public sealed record Sts2HookReadiness(
    bool HasModsDirectory,
    bool HasHarmony,
    bool HasSteamworks,
    bool HasModFileIoInterface,
    bool HasNetGameService,
    bool HasRunLobbyListeners)
{
    public bool CanStartAdapterWork =>
        HasModsDirectory &&
        HasHarmony &&
        HasSteamworks &&
        HasModFileIoInterface &&
        HasNetGameService &&
        HasRunLobbyListeners;
}

public sealed class Sts2HookProbe
{
    private static readonly string[] RequiredTypeNames =
    [
        "MegaCrit.Sts2.Core.Modding.IModManagerFileIo",
        "MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.IRunLobbyListener",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.IStartRunLobbyListener",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.ILoadRunLobbyListener"
    ];

    public Sts2HookReadiness Probe(Sts2GameInfo gameInfo)
    {
        string sts2AssemblyPath = Path.Combine(gameInfo.DataPath, "sts2.dll");
        Type[] readableTypes = ReadLoadableTypes(sts2AssemblyPath);
        HashSet<string> names = readableTypes.Select(type => type.FullName ?? type.Name).ToHashSet(StringComparer.Ordinal);

        return new Sts2HookReadiness(
            Directory.Exists(gameInfo.ModsPath),
            File.Exists(Path.Combine(gameInfo.DataPath, "0Harmony.dll")),
            File.Exists(Path.Combine(gameInfo.DataPath, "Steamworks.NET.dll")),
            names.Contains(RequiredTypeNames[0]),
            names.Contains(RequiredTypeNames[1]),
            names.Contains(RequiredTypeNames[2]) &&
            names.Contains(RequiredTypeNames[3]) &&
            names.Contains(RequiredTypeNames[4]));
    }

    private static Type[] ReadLoadableTypes(string assemblyPath)
    {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }
}
