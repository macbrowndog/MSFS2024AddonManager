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
        var communityEntries = new List<CommunityPackageEntry>();
        foreach (string communityPath in GetCommunityFolders(
                     settings.CommunityFolder,
                     settings.Community2024Folder))
        {
            foreach (string enabledPath in EnumerateDirectoriesSafely(communityPath))
            {
                communityEntries.Add(new CommunityPackageEntry(
                    enabledPath,
                    communityPath,
                    GetCanonicalLinkTarget(enabledPath)));
            }
        }

        IReadOnlyDictionary<string, IReadOnlyList<string>> enabledPathsBySourcePath =
            IndexEnabledCommunityPathsBySource(communityEntries);

        var addons = new Dictionary<string, Addon>(StringComparer.OrdinalIgnoreCase);
        foreach (string libraryPath in settings.AddonLibraries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(libraryPath))
            {
                continue;
            }

            foreach (string addonPath in EnumerateAddonPackageDirectories(
                         libraryPath,
                         cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string canonicalAddonPath = AddonIdentity.CanonicalizePath(addonPath);
                enabledPathsBySourcePath.TryGetValue(
                    canonicalAddonPath,
                    out IReadOnlyList<string>? enabledCommunityPaths);
                Addon addon = ReadAddon(
                    addonPath,
                    libraryPath,
                    enabledCommunityPaths ?? [],
                    true,
                    canonicalAddonPath);
                addons.TryAdd(canonicalAddonPath, addon);
            }
        }

        var managedSourcePaths = addons.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (CommunityPackageEntry entry in communityEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.LinkTargetPath is not null &&
                managedSourcePaths.Contains(entry.LinkTargetPath))
            {
                continue;
            }

            if (!Directory.Exists(entry.InstalledPath))
            {
                continue;
            }

            string canonicalInstalledPath = AddonIdentity.CanonicalizePath(
                entry.InstalledPath);
            addons.TryAdd(
                canonicalInstalledPath,
                ReadAddon(
                    entry.InstalledPath,
                    entry.CommunityPath,
                    [entry.CommunityPath],
                    false,
                    entry.LinkTargetPath ?? canonicalInstalledPath));
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
        CommunityFolderSummary[] communityFolders = GetCommunityFolders(
                settings.CommunityFolder,
                settings.Community2024Folder)
            .Select(path => new CommunityFolderSummary(
                path,
                Path.GetFileName(path),
                EnumerateDirectoriesSafely(path).Count()))
            .ToArray();
        bool communityAvailable = communityFolders.Length > 0;
        int communityItems = communityFolders.Sum(folder => folder.ItemCount);
        IReadOnlyDictionary<string, int> enabledByCategory = addons
            .Where(addon => addon.IsEnabled)
            .GroupBy(addon => addon.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        return new ScanSummary
        {
            CommunityAvailable = communityAvailable,
            ConfiguredLibraries = settings.AddonLibraries.Count,
            AvailableLibraries = availableLibraries,
            TotalAddons = addons.Count,
            EnabledAddons = addons.Count(addon => addon.IsEnabled),
            DisabledAddons = addons.Count(addon => !addon.IsEnabled),
            CommunityItems = communityItems,
            CommunityFolders = communityFolders,
            EnabledByCategory = enabledByCategory,
            CompletedAt = DateTimeOffset.Now
        };
    }

    private static Addon ReadAddon(
        string addonPath,
        string libraryPath,
        IReadOnlyList<string> enabledCommunityPaths,
        bool isManagedLibraryAddon,
        string? canonicalSourcePath = null)
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
            PackageIdentity = AddonIdentity.CreatePackageIdentity(
                folderName,
                name,
                author,
                contentType),
            CanonicalPath = canonicalSourcePath ??
                AddonIdentity.CanonicalizePath(addonPath),
            Category = InferCategory(contentType, folderName),
            Version = version,
            Author = author,
            ThumbnailPath = FindThumbnailPath(addonPath),
            EnabledCommunityPaths = enabledCommunityPaths.ToArray(),
            IsManagedLibraryAddon = isManagedLibraryAddon
        };
    }

    private static string? FindThumbnailPath(string addonPath)
    {
        string[] supportedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
        var searchRoots = new List<(string Path, SearchOption SearchOption)>
        {
            (addonPath, SearchOption.TopDirectoryOnly)
        };
        string contentInfoPath = Path.Combine(addonPath, "ContentInfo");
        if (Directory.Exists(contentInfoPath))
        {
            searchRoots.Insert(0, (contentInfoPath, SearchOption.AllDirectories));
        }

        foreach ((string searchRoot, SearchOption searchOption) in searchRoots)
        {
            try
            {
                string? thumbnail = Directory
                    .EnumerateFiles(searchRoot, "Thumbnail*", searchOption)
                    .FirstOrDefault(path =>
                        supportedExtensions.Contains(
                            Path.GetExtension(path),
                            StringComparer.OrdinalIgnoreCase) ||
                        string.IsNullOrEmpty(Path.GetExtension(path)));

                if (thumbnail is not null)
                {
                    return thumbnail;
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                // A thumbnail is optional; package discovery should continue.
            }
        }

        return null;
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

    private static IEnumerable<string> EnumerateAddonPackageDirectories(
        string libraryPath,
        CancellationToken cancellationToken)
    {
        const int maximumDepth = 12;
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((libraryPath, 0));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string currentPath, int depth) = pending.Dequeue();

            if (depth > 0 && IsAddonPackageRoot(currentPath))
            {
                yield return currentPath;
                continue;
            }

            if (depth >= maximumDepth)
            {
                continue;
            }

            foreach (string childPath in EnumerateDirectoriesSafely(currentPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsReparsePoint(childPath))
                {
                    pending.Enqueue((childPath, depth + 1));
                }
            }
        }
    }

    private static bool IsAddonPackageRoot(string path)
    {
        return File.Exists(Path.Combine(path, "manifest.json")) ||
               File.Exists(Path.Combine(path, "layout.json"));
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static string? GetCanonicalLinkTarget(string path)
    {
        try
        {
            var info = new DirectoryInfo(path);
            string? linkTarget = info.LinkTarget;
            if (linkTarget is null)
            {
                return null;
            }

            string resolvedTarget = Path.IsPathRooted(linkTarget)
                ? linkTarget
                : Path.Combine(info.Parent?.FullName ?? string.Empty, linkTarget);
            return AddonIdentity.CanonicalizePath(resolvedTarget);
        }
        catch (Exception exception) when (
            exception is IOException or
                UnauthorizedAccessException or
                ArgumentException or
                NotSupportedException)
        {
            return null;
        }
    }

    internal static IReadOnlyDictionary<string, IReadOnlyList<string>>
        IndexEnabledCommunityPathsBySource(
            IEnumerable<CommunityPackageEntry> communityEntries) =>
        communityEntries
            .Where(entry => entry.LinkTargetPath is not null)
            .GroupBy(
                entry => entry.LinkTargetPath!,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(entry => entry.CommunityPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> GetCommunityFolders(
        string configuredPath,
        string? community2024Path = null)
    {
        var emittedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            if (!string.IsNullOrWhiteSpace(community2024Path) &&
                Directory.Exists(community2024Path))
            {
                yield return Path.GetFullPath(community2024Path);
            }

            yield break;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(configuredPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException)
        {
            yield break;
        }

        if (Directory.Exists(fullPath))
        {
            emittedPaths.Add(fullPath);
            yield return fullPath;
        }

        if (!string.IsNullOrWhiteSpace(community2024Path))
        {
            string explicit2024Path = Path.GetFullPath(community2024Path);
            if (Directory.Exists(explicit2024Path) &&
                emittedPaths.Add(explicit2024Path))
            {
                yield return explicit2024Path;
            }

            yield break;
        }

        string? parent = Path.GetDirectoryName(fullPath);
        string folderName = Path.GetFileName(fullPath);
        if (parent is null)
        {
            yield break;
        }

        string? siblingName = folderName.Equals(
            "Community",
            StringComparison.OrdinalIgnoreCase)
            ? "Community2024"
            : folderName.Equals(
                "Community2024",
                StringComparison.OrdinalIgnoreCase)
                ? "Community"
                : null;

        if (siblingName is null)
        {
            yield break;
        }

        string siblingPath = Path.Combine(parent, siblingName);
        if (Directory.Exists(siblingPath))
        {
            yield return siblingPath;
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

    public IReadOnlyList<CommunityFolderSummary> CommunityFolders { get; init; } = [];

    public IReadOnlyDictionary<string, int> EnabledByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset CompletedAt { get; init; }
}

public sealed record CommunityFolderSummary(
    string Path,
    string Name,
    int ItemCount);

internal sealed record CommunityPackageEntry(
    string InstalledPath,
    string CommunityPath,
    string? LinkTargetPath);
