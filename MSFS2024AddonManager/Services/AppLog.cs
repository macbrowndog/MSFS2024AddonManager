using System.Diagnostics;
using System.Text;

namespace MSFS2024AddonManager.Services;

internal static class AppLog
{
    private const int ExportCharacterLimit = 128 * 1024;
    private static readonly Lazy<RollingFileLogger> Logger = new(CreateLogger);

    public static string LogPath => Logger.Value.LogPath;

    public static void Information(string message)
    {
        Logger.Value.Write("INFO", null, message, null);
    }

    public static string UnexpectedException(string context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string incidentId = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        Logger.Value.Write("ERROR", incidentId, context, exception);
        return incidentId;
    }

    public static string ReadRecentEntries()
    {
        return Logger.Value.ReadRecentEntries(ExportCharacterLimit);
    }

    private static RollingFileLogger CreateLogger()
    {
        string logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFS2024AddonManager",
            "logs");

        return new RollingFileLogger(
            Path.Combine(logFolder, "application.log"),
            maxBytes: 1024 * 1024,
            retainedFileCount: 5);
    }
}

internal sealed class RollingFileLogger
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly long maxBytes;
    private readonly int retainedFileCount;
    private readonly object syncRoot = new();

    public RollingFileLogger(
        string logPath,
        long maxBytes,
        int retainedFileCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(retainedFileCount, 1);

        LogPath = Path.GetFullPath(logPath);
        this.maxBytes = maxBytes;
        this.retainedFileCount = retainedFileCount;
    }

    public string LogPath { get; }

    public string ReadRecentEntries(int maximumCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCharacters);

        lock (syncRoot)
        {
            try
            {
                var content = new StringBuilder();
                for (int index = retainedFileCount - 1; index >= 1; index--)
                {
                    AppendFile(content, GetArchivePath(index));
                }

                AppendFile(content, LogPath);
                return content.Length <= maximumCharacters
                    ? content.ToString()
                    : content.ToString(content.Length - maximumCharacters, maximumCharacters);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                Trace.WriteLine($"Could not read application log: {exception}");
                return string.Empty;
            }
        }
    }

    public void Write(
        string level,
        string? incidentId,
        string message,
        Exception? exception)
    {
        string entry = FormatEntry(level, incidentId, message, exception);
        byte[] entryBytes = Utf8WithoutBom.GetBytes(entry);

        lock (syncRoot)
        {
            try
            {
                string? directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                try
                {
                    RollIfNeeded(entryBytes.Length);
                }
                catch (Exception rollError) when (
                    rollError is IOException or UnauthorizedAccessException)
                {
                    // Retaining the new error is more important than enforcing
                    // the size limit when an archive is temporarily locked.
                    Trace.WriteLine($"Could not roll application log: {rollError}");
                }

                using var stream = new FileStream(
                    LogPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read);
                stream.Write(entryBytes);
                stream.Flush(flushToDisk: true);
            }
            catch (Exception writeError) when (
                writeError is IOException or UnauthorizedAccessException)
            {
                Trace.WriteLine($"Could not write application log: {writeError}");
                Trace.WriteLine(entry);
            }
        }
    }

    private static void AppendFile(StringBuilder content, string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        content.Append(File.ReadAllText(path));
    }

    private static string FormatEntry(
        string level,
        string? incidentId,
        string message,
        Exception? exception)
    {
        var entry = new StringBuilder();
        entry.Append(DateTimeOffset.Now.ToString("O"))
            .Append(" [").Append(level).Append(']');
        if (!string.IsNullOrWhiteSpace(incidentId))
        {
            entry.Append(" [incident:").Append(incidentId).Append(']');
        }

        entry.Append(' ').AppendLine(message);
        if (exception is not null)
        {
            entry.Append("Application: ").AppendLine(Application.ProductVersion);
            entry.Append("Windows: ").AppendLine(Environment.OSVersion.ToString());
            entry.Append(".NET: ").AppendLine(Environment.Version.ToString());
            entry.AppendLine(exception.ToString());
        }

        return entry.AppendLine().ToString();
    }

    private string GetArchivePath(int index)
    {
        string? directory = Path.GetDirectoryName(LogPath);
        string fileName = Path.GetFileNameWithoutExtension(LogPath);
        string extension = Path.GetExtension(LogPath);
        return Path.Combine(directory ?? string.Empty, $"{fileName}.{index}{extension}");
    }

    private void RollIfNeeded(int incomingByteCount)
    {
        var currentLog = new FileInfo(LogPath);
        if (!currentLog.Exists || currentLog.Length == 0 ||
            currentLog.Length + incomingByteCount <= maxBytes)
        {
            return;
        }

        int archiveCount = retainedFileCount - 1;
        for (int index = archiveCount; index >= 1; index--)
        {
            string source = index == 1 ? LogPath : GetArchivePath(index - 1);
            if (!File.Exists(source))
            {
                continue;
            }

            File.Move(source, GetArchivePath(index), overwrite: true);
        }
    }
}
