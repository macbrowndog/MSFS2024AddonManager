namespace MSFS2024AddonManager.Models;

public sealed class AddonProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public List<ProfileAddonReference> Addons { get; set; } = [];

    // Retained for automatic migration from profiles created before stable
    // package references were introduced.
    public List<string> AddonFolderNames { get; set; } = [];

    public int AssignmentCount => Addons.Count + AddonFolderNames.Count;
}

public sealed class ProfileAddonReference
{
    public required string PackageIdentity { get; init; }

    public required string SourcePath { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string FolderName { get; init; } = string.Empty;
}

public sealed class ProfileCollection
{
    public Guid? ActiveProfileId { get; set; }

    public List<AddonProfile> Profiles { get; set; } = [];
}
