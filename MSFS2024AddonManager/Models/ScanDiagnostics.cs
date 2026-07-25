namespace MSFS2024AddonManager.Models;

public enum DiagnosticSeverity
{
    Information,
    Success,
    Warning,
    Error
}

public sealed record DiagnosticItem(
    DiagnosticSeverity Severity,
    string Check,
    string Result,
    string Path);

public sealed class DiagnosticReport
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public List<DiagnosticItem> Items { get; init; } = [];

    public int PackageFolders { get; set; }

    public int ValidManifests { get; set; }

    public int InvalidManifests { get; set; }

    public int CommunityLinks { get; set; }
}
