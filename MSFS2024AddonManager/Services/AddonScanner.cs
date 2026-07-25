using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class AddonScanner
{
    public Task<ScanSummary> ScanAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Scan(settings, cancellationToken), cancellationToken);
    }

    private static ScanSummary Scan(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var addonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int availableLibraries = 0;

        foreach (string libraryPath in settings.AddonLibraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(libraryPath))
            {
                continue;
            }

            availableLibraries++;
            foreach (string directory in EnumerateDirectoriesSafely(libraryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                addonNames.Add(Path.GetFileName(directory));
            }
        }

        var communityItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool communityAvailable = Directory.Exists(settings.CommunityFolder);
        if (communityAvailable)
        {
            foreach (string directory in EnumerateDirectoriesSafely(settings.CommunityFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                communityItems.Add(Path.GetFileName(directory));
            }
        }

        int enabledAddons = addonNames.Count(communityItems.Contains);

        return new ScanSummary
        {
            CommunityAvailable = communityAvailable,
            ConfiguredLibraries = settings.AddonLibraries.Count,
            AvailableLibraries = availableLibraries,
            TotalAddons = addonNames.Count,
            EnabledAddons = enabledAddons,
            DisabledAddons = addonNames.Count - enabledAddons,
            CommunityItems = communityItems.Count,
            CompletedAt = DateTimeOffset.Now
        };
    }

    private static IEnumerable<string> EnumerateDirectoriesSafely(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}

public sealed class ScanSummary
{
    public bool CommunityAvailable { get; init; }

    public int ConfiguredLibraries { get; init; }

    public int AvailableLibraries { get; init; }

    public int TotalAddons { get; init; }

    public int EnabledAddons { get; init; }

    public int DisabledAddons { get; init; }

    public int CommunityItems { get; init; }

    public DateTimeOffset CompletedAt { get; init; }
}
