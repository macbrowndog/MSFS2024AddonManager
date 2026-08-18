using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using Xunit;

namespace MSFS2024AddonManager.Tests;

public sealed class AddonIdentityTests
{
    [Fact]
    public void PackageIdentity_IsStableAcrossCaseAndWhitespaceChanges()
    {
        string first = AddonIdentity.CreatePackageIdentity(
            "vendor-package",
            "Example Airport",
            "Example Studio",
            "SCENERY");
        string second = AddonIdentity.CreatePackageIdentity(
            " VENDOR-PACKAGE ",
            "example airport",
            "example studio",
            "scenery");

        Assert.Equal(first, second);
        Assert.StartsWith("sha256:", first);
    }

    [Fact]
    public void ProfileAssignments_DistinguishIdenticalFolderNamesBySourcePath()
    {
        Addon first = CreateAddon(
            @"D:\LibraryOne\duplicate",
            "First Package");
        Addon second = CreateAddon(
            @"E:\LibraryTwo\duplicate",
            "Second Package");
        var profile = new AddonProfile { Name = "Test" };

        ProfileAssignmentService.Toggle(profile, first);
        ProfileAssignmentService.Toggle(profile, second);

        Assert.True(ProfileAssignmentService.IsAssigned(profile, first));
        Assert.True(ProfileAssignmentService.IsAssigned(profile, second));
        Assert.Equal(2, profile.Addons.Count);

        ProfileAssignmentService.Toggle(profile, first);

        Assert.False(ProfileAssignmentService.IsAssigned(profile, first));
        Assert.True(ProfileAssignmentService.IsAssigned(profile, second));
        Assert.Single(profile.Addons);
        Assert.Equal(
            AddonIdentity.GetCanonicalPath(second),
            profile.Addons[0].SourcePath);
    }

    [Fact]
    public void SelectingAMovedPackage_ReplacesItsStaleSourcePath()
    {
        Addon previous = CreateAddon(@"D:\OldLibrary\package");
        Addon moved = CreateAddon(@"E:\NewLibrary\package");
        var profile = new AddonProfile { Name = "Test" };
        profile.Addons.Add(ProfileAssignmentService.CreateReference(previous));

        ProfileAssignmentService.Toggle(profile, moved);

        ProfileAddonReference reference = Assert.Single(profile.Addons);
        Assert.Equal(AddonIdentity.GetPackageIdentity(moved), reference.PackageIdentity);
        Assert.Equal(AddonIdentity.GetCanonicalPath(moved), reference.SourcePath);
        Assert.True(ProfileAssignmentService.IsAssigned(profile, moved));
        Assert.False(ProfileAssignmentService.IsAssigned(profile, previous));
    }

    [Fact]
    public void LegacyAssignment_MigratesWhenTheFolderNameIsUnique()
    {
        Addon addon = CreateAddon(@"D:\Library\unique");
        var profile = new AddonProfile
        {
            Name = "Legacy",
            AddonFolderNames = ["unique"]
        };

        int migrated = ProfileAssignmentService.MigrateLegacyAssignments(
            profile,
            [addon]);

        Assert.Equal(1, migrated);
        Assert.Empty(profile.AddonFolderNames);
        ProfileAddonReference reference = Assert.Single(profile.Addons);
        Assert.Equal(AddonIdentity.GetPackageIdentity(addon), reference.PackageIdentity);
        Assert.Equal(AddonIdentity.GetCanonicalPath(addon), reference.SourcePath);
    }

    [Fact]
    public void LegacyAssignment_RemainsUnchangedWhenTheFolderNameIsAmbiguous()
    {
        Addon first = CreateAddon(@"D:\LibraryOne\duplicate");
        Addon second = CreateAddon(@"E:\LibraryTwo\duplicate");
        var profile = new AddonProfile
        {
            Name = "Legacy",
            AddonFolderNames = ["duplicate"]
        };

        int migrated = ProfileAssignmentService.MigrateLegacyAssignments(
            profile,
            [first, second]);

        Assert.Equal(0, migrated);
        Assert.Equal(["duplicate"], profile.AddonFolderNames);
        Assert.Empty(profile.Addons);
    }

    [Fact]
    public void LegacyProfile_IsPersistedWithStableReferenceAfterMigration()
    {
        using var directory = new TemporaryDirectory();
        string profilesPath = Path.Combine(directory.Path, "profiles.json");
        File.WriteAllText(
            profilesPath,
            """
            {
              "Profiles": [
                {
                  "Name": "Legacy",
                  "AddonFolderNames": ["unique"]
                }
              ]
            }
            """);
        var service = new ProfileService(profilesPath);
        ProfileCollection collection = service.Load();
        Addon addon = CreateAddon(@"D:\Library\unique");

        int migrated = ProfileAssignmentService.MigrateLegacyAssignments(
            collection.Profiles.Single(),
            [addon]);
        service.Save(collection);
        ProfileCollection reloaded = service.Load();

        Assert.Equal(1, migrated);
        AddonProfile profile = Assert.Single(reloaded.Profiles);
        Assert.Empty(profile.AddonFolderNames);
        ProfileAddonReference reference = Assert.Single(profile.Addons);
        Assert.Equal(AddonIdentity.GetPackageIdentity(addon), reference.PackageIdentity);
        Assert.Equal(AddonIdentity.GetCanonicalPath(addon), reference.SourcePath);
    }

    [Fact]
    public void CommunityIndex_AssociatesDuplicateNamesByLinkTarget()
    {
        string firstSource = AddonIdentity.CanonicalizePath(
            @"D:\LibraryOne\duplicate");
        string secondSource = AddonIdentity.CanonicalizePath(
            @"E:\LibraryTwo\duplicate");
        CommunityPackageEntry[] entries =
        [
            new(
                @"C:\MSFS\Community\duplicate",
                @"C:\MSFS\Community",
                firstSource),
            new(
                @"C:\MSFS\Community2024\duplicate",
                @"C:\MSFS\Community2024",
                secondSource)
        ];

        IReadOnlyDictionary<string, IReadOnlyList<string>> index =
            AddonScanner.IndexEnabledCommunityPathsBySource(entries);

        Assert.Equal(
            [@"C:\MSFS\Community"],
            index[firstSource]);
        Assert.Equal(
            [@"C:\MSFS\Community2024"],
            index[secondSource]);
    }

    private static Addon CreateAddon(
        string path,
        string title = "Duplicate Package") => new()
        {
            Name = title,
            FolderName = Path.GetFileName(path),
            Path = path,
            CanonicalPath = AddonIdentity.CanonicalizePath(path),
            PackageIdentity = AddonIdentity.CreatePackageIdentity(
            Path.GetFileName(path),
            title,
            "Example Studio",
            "SCENERY"),
            LibraryPath = Path.GetDirectoryName(path) ?? string.Empty
        };

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory(
                "MSFS2024AddonManager.IdentityTests-").FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
