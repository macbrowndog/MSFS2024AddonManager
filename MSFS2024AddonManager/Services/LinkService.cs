using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class LinkService
{
    public LinkOperationResult Enable(Addon addon, string communityFolder)
    {
        string? validationError = ValidatePaths(addon, communityFolder);
        if (validationError is not null)
        {
            return LinkOperationResult.Failed(validationError);
        }

        string linkPath = GetLinkPath(communityFolder, addon.FolderName);
        DirectoryInfo linkInfo = new(linkPath);
        string? existingLinkTarget = GetLinkTarget(linkInfo);

        if (existingLinkTarget is not null)
        {
            string resolvedTarget = ResolveLinkTarget(linkInfo, existingLinkTarget);
            if (PathsEqual(resolvedTarget, addon.Path))
            {
                return LinkOperationResult.Succeeded(
                    "This addon is already enabled.",
                    linkPath);
            }

            return LinkOperationResult.Failed(
                "A different symbolic link already uses this package name. Nothing was changed.",
                linkPath);
        }

        if (Directory.Exists(linkPath) || File.Exists(linkPath))
        {
            return LinkOperationResult.Failed(
                "A real file or folder already exists in the selected Community folder with this name. Nothing was overwritten.",
                linkPath);
        }

        try
        {
            Directory.CreateSymbolicLink(linkPath, addon.Path);
            return LinkOperationResult.Succeeded(
                "Addon enabled with a directory symbolic link.",
                linkPath);
        }
        catch (UnauthorizedAccessException)
        {
            return LinkOperationResult.Failed(
                "Windows denied symbolic-link creation. Enable Windows Developer Mode or run the manager as administrator, then try again.",
                linkPath);
        }
        catch (Exception exception) when (
            exception is IOException or NotSupportedException)
        {
            return LinkOperationResult.Failed(
                $"The symbolic link could not be created: {exception.Message}",
                linkPath);
        }
    }

    public LinkOperationResult Disable(Addon addon, string communityFolder)
    {
        string? validationError = ValidateCommunityFolder(communityFolder);
        if (validationError is not null)
        {
            return LinkOperationResult.Failed(validationError);
        }

        string linkPath = GetLinkPath(communityFolder, addon.FolderName);
        DirectoryInfo linkInfo = new(linkPath);
        string? existingLinkTarget = GetLinkTarget(linkInfo);
        if (existingLinkTarget is null)
        {
            if (Directory.Exists(linkPath) || File.Exists(linkPath))
            {
                return LinkOperationResult.Failed(
                    "This Community item is a real file or folder, not a symbolic link. Nothing was deleted.",
                    linkPath);
            }

            return LinkOperationResult.Succeeded(
                "This addon is already disabled.",
                linkPath);
        }

        string resolvedTarget = ResolveLinkTarget(linkInfo, existingLinkTarget);
        if (!PathsEqual(resolvedTarget, addon.Path))
        {
            return LinkOperationResult.Failed(
                "The symbolic link points to a different addon. Nothing was deleted.",
                linkPath);
        }

        try
        {
            Directory.Delete(linkPath);
            return LinkOperationResult.Succeeded(
                "Addon disabled. The source addon folder was not moved or deleted.",
                linkPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return LinkOperationResult.Failed(
                $"The symbolic link could not be removed: {exception.Message}",
                linkPath);
        }
    }

    private static string? ValidatePaths(Addon addon, string communityFolder)
    {
        if (!Directory.Exists(addon.Path))
        {
            return "The source addon folder no longer exists.";
        }

        return ValidateCommunityFolder(communityFolder);
    }

    private static string? ValidateCommunityFolder(string communityFolder)
    {
        if (string.IsNullOrWhiteSpace(communityFolder))
        {
            return "Select the MSFS 2024 Community folder in Settings first.";
        }

        return Directory.Exists(communityFolder)
            ? null
            : "The configured Community folder does not exist or is unavailable.";
    }

    private static string GetLinkPath(string communityFolder, string folderName)
    {
        string communityFullPath = Path.GetFullPath(communityFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string linkPath = Path.GetFullPath(Path.Combine(communityFullPath, folderName));
        string? parentPath = Path.GetDirectoryName(linkPath);

        if (!PathsEqual(parentPath ?? string.Empty, communityFullPath))
        {
            throw new InvalidOperationException(
                "The addon folder name would create a path outside the selected Community folder.");
        }

        return linkPath;
    }

    private static string? GetLinkTarget(DirectoryInfo linkInfo)
    {
        try
        {
            return linkInfo.LinkTarget;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ResolveLinkTarget(
        DirectoryInfo linkInfo,
        string linkTarget)
    {
        if (Path.IsPathRooted(linkTarget))
        {
            return Path.GetFullPath(linkTarget);
        }

        return Path.GetFullPath(Path.Combine(
            linkInfo.Parent?.FullName ?? string.Empty,
            linkTarget));
    }

    private static bool PathsEqual(string first, string second)
    {
        return string.Equals(
            Path.GetFullPath(first)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(second)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record LinkOperationResult(
    bool Success,
    string Message,
    string LinkPath)
{
    public static LinkOperationResult Succeeded(
        string message,
        string linkPath = "") => new(true, message, linkPath);

    public static LinkOperationResult Failed(
        string message,
        string linkPath = "") => new(false, message, linkPath);
}
