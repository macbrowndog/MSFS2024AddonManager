namespace MSFS2024AddonManager.Models;

public sealed class AddonProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public List<string> AddonFolderNames { get; set; } = [];
}

public sealed class ProfileCollection
{
    public Guid? ActiveProfileId { get; set; }

    public List<AddonProfile> Profiles { get; set; } = [];
}
