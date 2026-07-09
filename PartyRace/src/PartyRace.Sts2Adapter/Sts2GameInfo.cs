using System.Text.Json;

namespace PartyRace.Sts2Adapter;

public sealed record Sts2GameInfo(
    string InstallPath,
    string DataPath,
    string ModsPath,
    string Version,
    string Branch,
    string Commit,
    string BuildId,
    int MainAssemblyHash)
{
    public static Sts2GameInfo FromKnownInstall(
        string installPath = @"D:\SteamLibrary\steamapps\common\Slay the Spire 2",
        string appManifestPath = @"D:\SteamLibrary\steamapps\appmanifest_2868840.acf")
    {
        string releaseInfoPath = Path.Combine(installPath, "release_info.json");
        using JsonDocument releaseInfo = JsonDocument.Parse(File.ReadAllText(releaseInfoPath));
        JsonElement root = releaseInfo.RootElement;

        string buildId = File.Exists(appManifestPath)
            ? ReadAcfValue(File.ReadAllText(appManifestPath), "buildid") ?? "unknown"
            : "unknown";

        return new Sts2GameInfo(
            installPath,
            Path.Combine(installPath, "data_sts2_windows_x86_64"),
            Path.Combine(installPath, "mods"),
            root.GetProperty("version").GetString() ?? "unknown",
            root.GetProperty("branch").GetString() ?? "unknown",
            root.GetProperty("commit").GetString() ?? "unknown",
            buildId,
            root.GetProperty("main_assembly_hash").GetInt32());
    }

    private static string? ReadAcfValue(string acf, string key)
    {
        string needle = $"\"{key}\"";
        foreach (string line in acf.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith(needle, StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length >= 2 ? parts[^1] : null;
        }

        return null;
    }
}
