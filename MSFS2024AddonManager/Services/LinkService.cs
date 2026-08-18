using System.ComponentModel;
using System.Diagnostics;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class LinkService : IAddonLinkService
{
    private readonly ILinkFileSystem fileSystem;
    private readonly ISimulatorProcessDetector simulatorProcessDetector;

    public LinkService()
        : this(new PhysicalLinkFileSystem(), new Msfs2024ProcessDetector())
    {
    }

    public LinkService(ILinkFileSystem fileSystem)
        : this(fileSystem, new Msfs2024ProcessDetector())
    {
    }

    public LinkService(
        ILinkFileSystem fileSystem,
        ISimulatorProcessDetector simulatorProcessDetector)
    {
        this.fileSystem = fileSystem ??
            throw new ArgumentNullException(nameof(fileSystem));
        this.simulatorProcessDetector = simulatorProcessDetector ??
            throw new ArgumentNullException(nameof(simulatorProcessDetector));
    }

    public LinkOperationResult Enable(Addon addon, string communityFolder)
    {
        LinkOperationResult? simulatorStateFailure = ValidateSimulatorState();
        if (simulatorStateFailure is not null)
        {
            return simulatorStateFailure;
        }

        string? validationError = ValidatePaths(addon, communityFolder);
        if (validationError is not null)
        {
            return LinkOperationResult.Failed(validationError);
        }

        string linkPath = GetLinkPath(communityFolder, addon.FolderName);
        string? existingLinkTarget = GetLinkTarget(linkPath);

        if (existingLinkTarget is not null)
        {
            string resolvedTarget = ResolveLinkTarget(linkPath, existingLinkTarget);
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

        if (fileSystem.DirectoryExists(linkPath) || fileSystem.FileExists(linkPath))
        {
            return LinkOperationResult.Failed(
                "A real file or folder already exists in the selected Community folder with this name. Nothing was overwritten.",
                linkPath);
        }

        try
        {
            fileSystem.CreateDirectorySymbolicLink(linkPath, addon.Path);
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
        LinkOperationResult? simulatorStateFailure = ValidateSimulatorState();
        if (simulatorStateFailure is not null)
        {
            return simulatorStateFailure;
        }

        string? validationError = ValidateCommunityFolder(communityFolder);
        if (validationError is not null)
        {
            return LinkOperationResult.Failed(validationError);
        }

        string linkPath = GetLinkPath(communityFolder, addon.FolderName);
        string? existingLinkTarget = GetLinkTarget(linkPath);
        if (existingLinkTarget is null)
        {
            if (fileSystem.DirectoryExists(linkPath) || fileSystem.FileExists(linkPath))
            {
                return LinkOperationResult.Failed(
                    "This Community item is a real file or folder, not a symbolic link. Nothing was deleted.",
                    linkPath);
            }

            return LinkOperationResult.Succeeded(
                "This addon is already disabled.",
                linkPath);
        }

        string resolvedTarget = ResolveLinkTarget(linkPath, existingLinkTarget);
        if (!PathsEqual(resolvedTarget, addon.Path))
        {
            return LinkOperationResult.Failed(
                "The symbolic link points to a different addon. Nothing was deleted.",
                linkPath);
        }

        try
        {
            fileSystem.DeleteDirectory(linkPath);
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

    private string? ValidatePaths(Addon addon, string communityFolder)
    {
        if (!fileSystem.DirectoryExists(addon.Path))
        {
            return "The source addon folder no longer exists.";
        }

        return ValidateCommunityFolder(communityFolder);
    }

    private LinkOperationResult? ValidateSimulatorState() =>
        simulatorProcessDetector.GetState() switch
        {
            SimulatorProcessState.NotRunning => null,
            SimulatorProcessState.Running => LinkOperationResult.Failed(
                "Microsoft Flight Simulator 2024 is running. Close it before enabling or disabling addons."),
            _ => LinkOperationResult.Failed(
                "The manager could not verify whether Microsoft Flight Simulator 2024 is running. Close MSFS and try again.")
        };

    private string? ValidateCommunityFolder(string communityFolder)
    {
        if (string.IsNullOrWhiteSpace(communityFolder))
        {
            return "Select the MSFS 2024 Community folder in Settings first.";
        }

        return fileSystem.DirectoryExists(communityFolder)
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

    private string? GetLinkTarget(string linkPath)
    {
        try
        {
            return fileSystem.GetDirectoryLinkTarget(linkPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ResolveLinkTarget(
        string linkPath,
        string linkTarget)
    {
        if (Path.IsPathRooted(linkTarget))
        {
            return Path.GetFullPath(linkTarget);
        }

        return Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(linkPath) ?? string.Empty,
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

public interface IAddonLinkService
{
    LinkOperationResult Enable(Addon addon, string communityFolder);

    LinkOperationResult Disable(Addon addon, string communityFolder);
}

public interface ILinkFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    string? GetDirectoryLinkTarget(string path);

    void CreateDirectorySymbolicLink(string path, string targetPath);

    void DeleteDirectory(string path);
}

public interface ISimulatorProcessDetector
{
    SimulatorProcessState GetState();
}

public enum SimulatorProcessState
{
    Unknown,
    NotRunning,
    Running
}

internal sealed class PhysicalLinkFileSystem : ILinkFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public string? GetDirectoryLinkTarget(string path) =>
        new DirectoryInfo(path).LinkTarget;

    public void CreateDirectorySymbolicLink(string path, string targetPath) =>
        Directory.CreateSymbolicLink(path, targetPath);

    public void DeleteDirectory(string path) => Directory.Delete(path);
}

internal sealed class Msfs2024ProcessDetector : ISimulatorProcessDetector
{
    private const string ProcessName = "FlightSimulator2024";

    public SimulatorProcessState GetState()
    {
        Process[] processes = [];
        try
        {
            processes = Process.GetProcessesByName(ProcessName);
            return processes.Length > 0
                ? SimulatorProcessState.Running
                : SimulatorProcessState.NotRunning;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                Win32Exception or
                NotSupportedException)
        {
            return SimulatorProcessState.Unknown;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
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
