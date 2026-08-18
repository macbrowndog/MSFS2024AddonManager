using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using Xunit;

namespace MSFS2024AddonManager.Tests;

public sealed class LoggingAndDiagnosticsTests
{
    [Fact]
    public void Logger_PersistsExceptionAndIncidentDetails()
    {
        using var directory = new TemporaryDirectory();
        string logPath = Path.Combine(directory.Path, "application.log");
        var logger = new RollingFileLogger(logPath, 64 * 1024, 3);

        logger.Write(
            "ERROR",
            "ABC123",
            "Unexpected UI-thread exception.",
            new InvalidOperationException("Example failure"));

        string log = File.ReadAllText(logPath);
        Assert.Contains("[ERROR] [incident:ABC123]", log);
        Assert.Contains("Unexpected UI-thread exception.", log);
        Assert.Contains("InvalidOperationException: Example failure", log);
        Assert.Contains("Application:", log);
    }

    [Fact]
    public void Logger_RollsAndRetainsConfiguredFileCount()
    {
        using var directory = new TemporaryDirectory();
        string logPath = Path.Combine(directory.Path, "application.log");
        var logger = new RollingFileLogger(logPath, maxBytes: 200, retainedFileCount: 3);

        for (int index = 1; index <= 4; index++)
        {
            logger.Write("INFO", null, $"entry-{index}-{new string('x', 210)}", null);
        }

        Assert.True(File.Exists(logPath));
        Assert.True(File.Exists(Path.Combine(directory.Path, "application.1.log")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "application.2.log")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "application.3.log")));

        string retained = logger.ReadRecentEntries(10_000);
        Assert.DoesNotContain("entry-1-", retained);
        Assert.Contains("entry-2-", retained);
        Assert.Contains("entry-3-", retained);
        Assert.Contains("entry-4-", retained);
    }

    [Fact]
    public void PathRedactor_RemovesKnownDriveUncAndUserPaths()
    {
        string userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Documents",
            "private-addon");
        const string libraryPath = @"E:\Libraries\Private Addons";
        const string uncPath = @"\\server-name\private-share\addons";
        var redactor = new PathRedactor([libraryPath, uncPath]);
        string input = $"User: '{userPath}' Library: {libraryPath} Network: \"{uncPath}\"";

        string redacted = redactor.Redact(input);

        Assert.DoesNotContain(userPath, redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-addon", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(libraryPath, redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("server-name", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-share", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[REDACTED_PATH]", redacted);
    }

    [Fact]
    public void ExportedDiagnostics_RedactReportAndLogPaths()
    {
        const string libraryPath = @"E:\Libraries\Customer Name\Addon";
        const string uncPath = @"\\nas01\private-addons\package\manifest.json";
        var report = new DiagnosticReport
        {
            Items =
            [
                new DiagnosticItem(
                    DiagnosticSeverity.Error,
                    "Package manifest",
                    $"Could not read '{uncPath}'.",
                    libraryPath)
            ]
        };
        var service = new ScanDiagnosticsService(() =>
            $"Failure reading {libraryPath}{Environment.NewLine}at {uncPath}{Environment.NewLine}");

        string export = service.FormatReport(report);

        Assert.DoesNotContain(libraryPath, export, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nas01", export, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-addons", export, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Privacy: absolute filesystem paths are redacted", export);
        Assert.Contains("Recent application log (paths redacted)", export);
        Assert.Contains("[REDACTED_PATH]", export);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory(
                "MSFS2024AddonManager.LoggingTests-").FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
