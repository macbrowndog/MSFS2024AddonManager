using System.Text.Json;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class AddonScanner
{
    public Task<IReadOnlyList<Addon>> FindAddonsAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<Addon>>(
            () => FindAddons(settings, cancellationToken),
            cancellationToken);
    }

    public Task<ScanSummary> ScanAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Scan(settings, cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<Addon> FindAddons(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var communityNames = new HashSet<string>(
            EnumerateDirectoriesSafely(settings.CommunityFolder)
                .Select(Path.GetFileName)
                .Where(folderName => !string.IsNullOrWhiteSpace(folderName))
                .Select(folderName => folderName!),
            StringComparer.OrdinalIgnoreCase);

        var addons = new Dictionary<string, Addon>(StringComparer.OrdinalIgnoreCase);
        foreach (string libraryPath in settings.AddonLibraries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(libraryPath))
            {
                continue;
            }

            foreach (string addonPath in EnumerateDirectoriesSafely(libraryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string folderName = Path.GetFileName(addonPath);
                Addon addon = ReadAddon(addonPath, libraryPath, communityNames.Contains(folderName));
                addons.TryAdd(folderName, addon);
            }
        }

        return addons.Values
            .OrderBy(addon => addon.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static ScanSummary Scan(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Addon> addons = FindAddons(settings, cancellationToken);
        int availableLibraries = settings.AddonLibraries.Count(Directory.Exists);
        bool communityAvailable = Directory.Exists(settings.CommunityFolder);
        int communityItems = communityAvailable
            ? EnumerateDirectoriesSafely(settings.CommunityFolder).Count()
            : 0;

        return new ScanSummary
        {
            CommunityAvailable = communityAvailable,
            ConfiguredLibraries = settings.AddonLibraries.Count,
            AvailableLibraries = availableLibraries,
            TotalAddons = addons.Count,
            EnabledAddons = addons.Count(addon => addon.IsEnabled),
            DisabledAddons = addons.Count(addon => !addon.IsEnabled),
            CommunityItems = communityItems,
            CompletedAt = DateTimeOffset.Now
        };
    }

    private static Addon ReadAddon(string addonPath, string libraryPath, bool isEnabled)
    {
        string folderName = Path.GetFileName(addonPath);
        string name = folderName;
        string version = "Unknown";
        string author = "Unknown";
        string contentType = string.Empty;
        string manifestPath = Path.Combine(addonPath, "manifest.json");

        if (File.Exists(manifestPath))
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(
                    File.ReadAllText(manifestPath),
                    new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    });

                JsonElement root = document.RootElement;
                name = GetString(root, "title") ?? name;
                version = GetString(root, "package_version") ?? version;
                author = GetString(root, "creator") ?? author;
                contentType = GetString(root, "content_type") ?? string.Empty;
            }
            catch (JsonException)
            {
                // Keep folder-derived values when third-party metadata is malformed.
            }
            catch (IOException)
            {
                // The package may be in use; it can still be listed by folder name.
            }
        }

        return new Addon
        {
            Name = name,
            FolderName = folderName,
            Path = addonPath,
            LibraryPath = libraryPath,
            Category = InferCategory(contentType, folderName),
            Version = version,
            Author = author,
            IsEnabled = isEnabled
        };
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out JsonElement value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string InferCategory(string contentType, string folderName)
    {
        string value = $"{contentType} {folderName}".ToLowerInvariant();

        if (value.Contains("aircraft") || value.Contains("plane"))
        {
            return "Aircraft";
        }

        if (value.Contains("airport"))
        {
            return "Airports";
        }

        if (value.Contains("livery"))
        {
            return "Liveries";
        }

        if (value.Contains("scenery"))
        {
            return "Scenery";
        }

        if (value.Contains("utility") || value.Contains("tool"))
        {
            return "Utilities";
        }

        return "Other";
    }

    private static IEnumerable<string> EnumerateDirectoriesSafely(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return [];
        }

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
