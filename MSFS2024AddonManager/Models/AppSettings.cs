namespace MSFS2024AddonManager.Models;

public sealed class AppSettings
{
    public string CommunityFolder { get; set; } = string.Empty;

    public List<string> AddonLibraries { get; set; } = [];

    public bool AutoDetectMsfs { get; set; } = true;

    public bool ScanOnStartup { get; set; } = true;
}
