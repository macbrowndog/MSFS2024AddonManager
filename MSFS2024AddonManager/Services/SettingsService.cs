using System.Text.Json;
using System.Text.RegularExpressions;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed partial class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AtomicJsonFileStore<AppSettings> store;

    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFS2024AddonManager",
            "settings.json"))
    {
    }

    internal SettingsService(string settingsPath)
    {
        store = new AtomicJsonFileStore<AppSettings>(settingsPath, JsonOptions);
    }

    public AppSettings Load()
    {
        return store.Load(
            static () => new AppSettings(),
            static settings => settings.AddonLibraries ??= []);
    }

    public void Save(AppSettings settings)
    {
        store.Save(settings);
    }

    public string? DetectCommunityFolder()
    {
        return DetectNamedCommunityFolder("Community") ??
               GetCommunityCandidates("Community").FirstOrDefault(Directory.Exists);
    }

    public string? DetectCommunity2024Folder()
    {
        return DetectNamedCommunityFolder("Community2024") ??
               GetCommunityCandidates("Community2024").FirstOrDefault(Directory.Exists);
    }

    private static string? DetectNamedCommunityFolder(string folderName)
    {
        foreach (string configPath in GetUserConfigCandidates())
        {
            string? packagesPath = ReadInstalledPackagesPath(configPath);
            if (string.IsNullOrWhiteSpace(packagesPath))
            {
                continue;
            }

            string communityPath = Path.Combine(packagesPath, folderName);
            if (Directory.Exists(communityPath))
            {
                return communityPath;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetUserConfigCandidates()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        yield return Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.Limitless_8wekyb3d8bbwe",
            "LocalCache",
            "UserCfg.opt");

        yield return Path.Combine(
            roamingAppData,
            "Microsoft Flight Simulator 2024",
            "UserCfg.opt");
    }

    private static IEnumerable<string> GetCommunityCandidates(string folderName)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        yield return Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.Limitless_8wekyb3d8bbwe",
            "LocalCache",
            "Packages",
            folderName);

        yield return Path.Combine(
            roamingAppData,
            "Microsoft Flight Simulator 2024",
            "Packages",
            folderName);
    }

    private static string? ReadInstalledPackagesPath(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            Match match = InstalledPackagesPathRegex().Match(File.ReadAllText(configPath));
            return match.Success ? match.Groups["path"].Value : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    [GeneratedRegex("InstalledPackagesPath\\s+\"(?<path>[^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex InstalledPackagesPathRegex();
}
