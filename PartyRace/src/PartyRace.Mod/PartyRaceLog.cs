namespace PartyRace.Mod;

internal static class PartyRaceLog
{
    public static void Append(string message)
    {
        string logDirectory = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "PartyRace");
        Directory.CreateDirectory(logDirectory);

        string logPath = Path.Combine(logDirectory, "party_race_mod_loaded.log");
        File.AppendAllText(
            logPath,
            $"[{DateTimeOffset.Now:O}] {message}{System.Environment.NewLine}");
    }
}
