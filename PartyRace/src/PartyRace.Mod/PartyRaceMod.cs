using MegaCrit.Sts2.Core.Modding;
using PartyRace.Core.Core;

namespace PartyRace.Mod;

[ModInitializer("Initialize")]
public static class PartyRaceMod
{
    public static void Initialize()
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PartyRace");
        Directory.CreateDirectory(logDirectory);

        string logPath = Path.Combine(logDirectory, "party_race_mod_loaded.log");
        File.AppendAllText(
            logPath,
            $"[{DateTimeOffset.Now:O}] {PartyRaceConstants.ModName} {PartyRaceConstants.ModVersion} initializer called.{Environment.NewLine}");
    }
}
