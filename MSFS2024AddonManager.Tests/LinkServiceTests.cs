using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using Xunit;

namespace MSFS2024AddonManager.Tests;

public sealed class LinkServiceTests
{
    [Fact]
    public void Enable_RefusesToOverwriteARealFolder()
    {
        using var source = new TemporaryDirectory();
        using var community = new TemporaryDirectory();
        string collisionPath = Directory.CreateDirectory(
            Path.Combine(community.Path, source.Name)).FullName;
        var addon = CreateAddon(source.Path);

        LinkOperationResult result = CreateService(
            new PhysicalTestLinkFileSystem()).Enable(addon, community.Path);

        Assert.False(result.Success);
        Assert.Contains("real file or folder", result.Message);
        Assert.True(Directory.Exists(collisionPath));
    }

    [Fact]
    public void Disable_RefusesToDeleteALinkPointingToAnotherAddon()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string selectedSource = @"D:\Addons\shared-name";
        string otherSource = @"E:\Addons\shared-name";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(selectedSource);
        fileSystem.AddLink(Path.Combine(community, "shared-name"), otherSource);

        LinkOperationResult result = CreateService(fileSystem).Disable(
            CreateAddon(selectedSource),
            community);

        Assert.False(result.Success);
        Assert.Contains("different addon", result.Message);
        Assert.Empty(fileSystem.DeletedDirectories);
    }

    [Fact]
    public void Disable_RemovesABrokenLinkWhenItsRecordedTargetMatches()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string unavailableSource = @"R:\Disconnected\package";
        string linkPath = Path.Combine(community, "package");
        fileSystem.AddDirectory(community);
        fileSystem.AddLink(linkPath, unavailableSource);

        LinkOperationResult result = CreateService(fileSystem).Disable(
            CreateAddon(unavailableSource),
            community);

        Assert.True(result.Success);
        Assert.Contains(fileSystem.Normalize(linkPath), fileSystem.DeletedDirectories);
    }

    [Fact]
    public void Enable_RefusesDuplicateFolderNamesFromDifferentLibraries()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string firstSource = @"D:\LibraryOne\duplicate";
        string secondSource = @"E:\LibraryTwo\duplicate";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(firstSource);
        fileSystem.AddDirectory(secondSource);
        LinkService service = CreateService(fileSystem);

        LinkOperationResult firstResult = service.Enable(
            CreateAddon(firstSource),
            community);
        LinkOperationResult secondResult = service.Enable(
            CreateAddon(secondSource),
            community);

        Assert.True(firstResult.Success);
        Assert.False(secondResult.Success);
        Assert.Contains("different symbolic link", secondResult.Message);
        Assert.Single(fileSystem.CreatedLinks);
    }

    [Fact]
    public void Enable_PreservesAUncSourceTarget()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string uncSource = @"\\server\msfs-addons\scenery\package";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(uncSource);

        LinkOperationResult result = CreateService(fileSystem).Enable(
            CreateAddon(uncSource),
            community);

        Assert.True(result.Success);
        Assert.Equal(
            uncSource,
            fileSystem.CreatedLinks.Single().TargetPath);
    }

    [Fact]
    public void Enable_PreservesARemovableDriveSourceTarget()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string removableSource = @"R:\MSFS Addons\aircraft\package";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(removableSource);

        LinkOperationResult result = CreateService(fileSystem).Enable(
            CreateAddon(removableSource),
            community);

        Assert.True(result.Success);
        Assert.Equal(
            removableSource,
            fileSystem.CreatedLinks.Single().TargetPath);
    }

    [Fact]
    public void Enable_RejectsAnUnavailableSourceLibrary()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string unavailableSource = @"R:\Disconnected\package";
        fileSystem.AddDirectory(community);

        LinkOperationResult result = CreateService(fileSystem).Enable(
            CreateAddon(unavailableSource),
            community);

        Assert.False(result.Success);
        Assert.Contains("source addon folder no longer exists", result.Message);
        Assert.Empty(fileSystem.CreatedLinks);
    }

    [Fact]
    public void Enable_IsBlockedWhileMsfs2024IsRunning()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string source = @"D:\Addons\package";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(source);

        LinkOperationResult result = CreateService(
            fileSystem,
            simulatorRunning: true).Enable(CreateAddon(source), community);

        Assert.False(result.Success);
        Assert.Contains("Flight Simulator 2024 is running", result.Message);
        Assert.Empty(fileSystem.CreatedLinks);
    }

    [Fact]
    public void Disable_IsBlockedWhileMsfs2024IsRunning()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string source = @"D:\Addons\package";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(source);
        fileSystem.AddLink(Path.Combine(community, "package"), source);

        LinkOperationResult result = CreateService(
            fileSystem,
            simulatorRunning: true).Disable(CreateAddon(source), community);

        Assert.False(result.Success);
        Assert.Contains("Flight Simulator 2024 is running", result.Message);
        Assert.Empty(fileSystem.DeletedDirectories);
    }

    [Fact]
    public void Enable_IsBlockedWhenTheSimulatorStateCannotBeVerified()
    {
        var fileSystem = new TestLinkFileSystem();
        string community = @"C:\MSFS\Community";
        string source = @"D:\Addons\package";
        fileSystem.AddDirectory(community);
        fileSystem.AddDirectory(source);
        var detector = new TestSimulatorProcessDetector(
            SimulatorProcessState.Unknown);

        LinkOperationResult result = new LinkService(
            fileSystem,
            detector).Enable(CreateAddon(source), community);

        Assert.False(result.Success);
        Assert.Contains("could not verify", result.Message);
        Assert.Empty(fileSystem.CreatedLinks);
    }

    private static LinkService CreateService(
        ILinkFileSystem fileSystem,
        bool simulatorRunning = false) =>
        new(
            fileSystem,
            new TestSimulatorProcessDetector(
                simulatorRunning
                    ? SimulatorProcessState.Running
                    : SimulatorProcessState.NotRunning));

    private static Addon CreateAddon(string path) => new()
    {
        Name = Path.GetFileName(path),
        FolderName = Path.GetFileName(path),
        Path = path,
        LibraryPath = Path.GetDirectoryName(path) ?? string.Empty
    };

    private sealed class TestLinkFileSystem : ILinkFileSystem
    {
        private readonly HashSet<string> directories =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> links =
            new(StringComparer.OrdinalIgnoreCase);

        public List<(string LinkPath, string TargetPath)> CreatedLinks { get; } = [];

        public List<string> DeletedDirectories { get; } = [];

        public bool DirectoryExists(string path) =>
            directories.Contains(Normalize(path)) || links.ContainsKey(Normalize(path));

        public bool FileExists(string path) => false;

        public string? GetDirectoryLinkTarget(string path) =>
            links.GetValueOrDefault(Normalize(path));

        public void CreateDirectorySymbolicLink(string path, string targetPath)
        {
            string normalizedPath = Normalize(path);
            links.Add(normalizedPath, targetPath);
            CreatedLinks.Add((normalizedPath, targetPath));
        }

        public void DeleteDirectory(string path)
        {
            string normalizedPath = Normalize(path);
            links.Remove(normalizedPath);
            DeletedDirectories.Add(normalizedPath);
        }

        public void AddDirectory(string path) => directories.Add(Normalize(path));

        public void AddLink(string path, string targetPath) =>
            links.Add(Normalize(path), Normalize(targetPath));

        public string Normalize(string path) => Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed class PhysicalTestLinkFileSystem : ILinkFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string path) => File.Exists(path);

        public string? GetDirectoryLinkTarget(string path) =>
            new DirectoryInfo(path).LinkTarget;

        public void CreateDirectorySymbolicLink(string path, string targetPath) =>
            Directory.CreateSymbolicLink(path, targetPath);

        public void DeleteDirectory(string path) => Directory.Delete(path);
    }

    private sealed class TestSimulatorProcessDetector(SimulatorProcessState state)
        : ISimulatorProcessDetector
    {
        public SimulatorProcessState GetState() => state;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory("MSFS2024AddonManager.Tests-").FullName;
        }

        public string Path { get; }

        public string Name => System.IO.Path.GetFileName(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
