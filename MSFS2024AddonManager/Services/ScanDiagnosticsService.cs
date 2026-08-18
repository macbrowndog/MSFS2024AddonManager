using System.Text;
using System.Text.Json;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class ScanDiagnosticsService
{
    private readonly Func<string> recentLogProvider;

    public ScanDiagnosticsService()
        : this(AppLog.ReadRecentEntries)
    {
    }

    internal ScanDiagnosticsService(Func<string> recentLogProvider)
    {
        this.recentLogProvider = recentLogProvider;
    }

    public Task<DiagnosticReport> RunAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Run(settings, cancellationToken), cancellationToken);
    }

    public string FormatReport(DiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var redactor = new PathRedactor(report.Items.Select(item => item.Path));
        var output = new StringBuilder();
        output.AppendLine("MSFS 2024 Addons Manager - Scan Diagnostics");
        output.AppendLine($"Created: {report.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");
        output.AppendLine($"Application version: {Application.ProductVersion}");
        output.AppendLine($"Windows: {Environment.OSVersion}");
        output.AppendLine($".NET: {Environment.Version}");
        output.AppendLine();
        output.AppendLine($"Package folders: {report.PackageFolders}");
        output.AppendLine($"Valid manifests: {report.ValidManifests}");
        output.AppendLine($"Invalid manifests: {report.InvalidManifests}");
        output.AppendLine($"Community links: {report.CommunityLinks}");
        output.AppendLine("Privacy: absolute filesystem paths are redacted from this export.");
        output.AppendLine();

        foreach (DiagnosticItem item in report.Items)
        {
            output.Append('[').Append(item.Severity.ToString().ToUpperInvariant()).Append("] ");
            output.Append(redactor.Redact(item.Check))
                .Append(": ")
                .AppendLine(redactor.Redact(item.Result));
            if (!string.IsNullOrWhiteSpace(item.Path))
            {
                output.Append("  Path: ").AppendLine(redactor.Redact(item.Path));
            }
        }

        string recentLog = recentLogProvider();
        if (!string.IsNullOrWhiteSpace(recentLog))
        {
            output.AppendLine();
            output.AppendLine("Recent application log (paths redacted)");
            output.AppendLine("---------------------------------------");
            output.Append(redactor.Redact(recentLog));
            if (!recentLog.EndsWith('\n'))
            {
                output.AppendLine();
            }
        }

        return output.ToString();
    }

    private static DiagnosticReport Run(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var report = new DiagnosticReport();
        string[] communityFolders = AddonScanner
            .GetCommunityFolders(
                settings.CommunityFolder,
                settings.Community2024Folder)
            .ToArray();
        if (communityFolders.Length == 0)
        {
            CheckCommunityFolder(settings.CommunityFolder, report, cancellationToken);
        }
        else
        {
            foreach (string communityFolder in communityFolders)
            {
                CheckCommunityFolder(communityFolder, report, cancellationToken);
            }
        }

        if (settings.AddonLibraries.Count == 0)
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Warning,
                "Addon libraries",
                "No addon libraries are configured.",
                string.Empty));
        }

        foreach (string libraryPath in settings.AddonLibraries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckLibrary(libraryPath, report, cancellationToken);
        }

        return report;
    }

    private static void CheckCommunityFolder(
        string path,
        DiagnosticReport report,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Warning,
                "Community folder",
                "Not configured.",
                string.Empty));
            return;
        }

        if (!Directory.Exists(path))
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Error,
                "Community folder",
                "The configured folder does not exist or is unavailable.",
                path));
            return;
        }

        try
        {
            DirectoryInfo[] entries = new DirectoryInfo(path).GetDirectories();
            int links = entries.Count(entry =>
                !string.IsNullOrWhiteSpace(entry.LinkTarget));
            report.CommunityLinks += links;
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Success,
                $"Community folder ({Path.GetFileName(path)})",
                $"Available. {entries.Length} folders, {links} symbolic links.",
                path));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Error,
                "Community folder",
                $"Could not read the folder: {exception.Message}",
                path));
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void CheckLibrary(
        string path,
        DiagnosticReport report,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Error,
                "Addon library",
                "The configured library does not exist or is unavailable.",
                path));
            return;
        }

        DirectoryInfo[] packages;
        try
        {
            packages = new DirectoryInfo(path).GetDirectories();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            report.Items.Add(new DiagnosticItem(
                DiagnosticSeverity.Error,
                "Addon library",
                $"Could not read the library: {exception.Message}",
                path));
            return;
        }

        report.PackageFolders += packages.Length;
        int valid = 0;
        int invalid = 0;
        int missing = 0;

        foreach (DirectoryInfo package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string manifestPath = Path.Combine(package.FullName, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                missing++;
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(
                    File.ReadAllText(manifestPath),
                    new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    });
                valid++;
            }
            catch (Exception exception) when (exception is JsonException or IOException)
            {
                invalid++;
                report.Items.Add(new DiagnosticItem(
                    DiagnosticSeverity.Warning,
                    "Package manifest",
                    $"Could not parse manifest.json: {exception.Message}",
                    manifestPath));
            }
        }

        report.ValidManifests += valid;
        report.InvalidManifests += invalid;
        DiagnosticSeverity severity = invalid > 0
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Success;
        report.Items.Add(new DiagnosticItem(
            severity,
            "Addon library",
            $"{packages.Length} packages; {valid} valid manifests; {invalid} invalid; {missing} without manifests.",
            path));
    }
}
