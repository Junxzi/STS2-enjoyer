using System.Text.Json;
using System.Runtime.InteropServices;

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
        string? installPath = null,
        string? appManifestPath = null,
        string? dataPath = null,
        string? modsPath = null)
    {
        installPath ??= GetDefaultInstallPath();
        appManifestPath ??= GetDefaultAppManifestPath();
        dataPath ??= GetDefaultDataPath(installPath);
        modsPath ??= GetDefaultModsPath(installPath);

        string releaseInfoPath = GetReleaseInfoPath(installPath);
        using JsonDocument releaseInfo = JsonDocument.Parse(File.ReadAllText(releaseInfoPath));
        JsonElement root = releaseInfo.RootElement;

        string buildId = File.Exists(appManifestPath)
            ? ReadAcfValue(File.ReadAllText(appManifestPath), "buildid") ?? "unknown"
            : "unknown";

        return new Sts2GameInfo(
            installPath,
            dataPath,
            modsPath,
            root.GetProperty("version").GetString() ?? "unknown",
            root.GetProperty("branch").GetString() ?? "unknown",
            root.GetProperty("commit").GetString() ?? "unknown",
            buildId,
            root.GetProperty("main_assembly_hash").GetInt32());
    }

    private static string GetDefaultInstallPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Steam",
                "steamapps",
                "common",
                "Slay the Spire 2");
        }

        return @"D:\SteamLibrary\steamapps\common\Slay the Spire 2";
    }

    private static string GetDefaultAppManifestPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Steam",
                "steamapps",
                "appmanifest_2868840.acf");
        }

        return @"D:\SteamLibrary\steamapps\appmanifest_2868840.acf";
    }

    private static string GetDefaultDataPath(string installPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(installPath, "data_sts2_windows_x86_64");
        }

        string resourcesPath = Path.Combine(
            installPath,
            "SlayTheSpire2.app",
            "Contents",
            "Resources");
        string architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "arm64"
            : "x86_64";
        return Path.Combine(resourcesPath, $"data_sts2_macos_{architecture}");
    }

    private static string GetDefaultModsPath(string installPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(installPath, "mods");
        }

        return Path.Combine(
            installPath,
            "SlayTheSpire2.app",
            "Contents",
            "MacOS",
            "mods");
    }

    private static string GetReleaseInfoPath(string installPath)
    {
        string directPath = Path.Combine(installPath, "release_info.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        return Path.Combine(
            installPath,
            "SlayTheSpire2.app",
            "Contents",
            "Resources",
            "release_info.json");
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
