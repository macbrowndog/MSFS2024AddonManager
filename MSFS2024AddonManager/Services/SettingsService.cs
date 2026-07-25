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

    private readonly string settingsPath;

    public SettingsService()
    {
        string settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFS2024AddonManager");

        settingsPath = Path.Combine(settingsFolder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(settingsPath),
                JsonOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string? folder = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public string? DetectCommunityFolder()
    {
        foreach (string configPath in GetUserConfigCandidates())
        {
            string? packagesPath = ReadInstalledPackagesPath(configPath);
            if (string.IsNullOrWhiteSpace(packagesPath))
            {
                continue;
            }

            string communityPath = Path.Combine(packagesPath, "Community");
            if (Directory.Exists(communityPath))
            {
                return communityPath;
            }
        }

        return GetCommunityCandidates().FirstOrDefault(Directory.Exists);
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

    private static IEnumerable<string> GetCommunityCandidates()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        yield return Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.Limitless_8wekyb3d8bbwe",
            "LocalCache",
            "Packages",
            "Community");

        yield return Path.Combine(
            roamingAppData,
            "Microsoft Flight Simulator 2024",
            "Packages",
            "Community");
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
