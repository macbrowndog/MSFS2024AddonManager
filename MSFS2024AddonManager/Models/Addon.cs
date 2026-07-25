namespace MSFS2024AddonManager.Models;

public sealed class Addon
{
    public required string Name { get; init; }

    public required string FolderName { get; init; }

    public required string Path { get; init; }

    public required string LibraryPath { get; init; }

    public string Category { get; init; } = "Other";

    public string Version { get; init; } = "Unknown";

    public string Author { get; init; } = "Unknown";

    public bool IsEnabled { get; init; }
}
