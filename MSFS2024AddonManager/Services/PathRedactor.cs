using System.Text.RegularExpressions;

namespace MSFS2024AddonManager.Services;

internal sealed partial class PathRedactor
{
    private readonly IReadOnlyList<(string Path, string Replacement)> knownPaths;

    public PathRedactor(IEnumerable<string>? reportPaths = null)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (reportPaths is not null)
        {
            foreach (string path in reportPaths.Where(path =>
                         !string.IsNullOrWhiteSpace(path)))
            {
                AddReplacement(replacements, path, "[REDACTED_PATH]");
            }
        }

        knownPaths = replacements
            .OrderByDescending(item => item.Key.Length)
            .Select(item => (item.Key, item.Value))
            .ToArray();
    }

    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        string redacted = value;
        foreach ((string path, string replacement) in knownPaths)
        {
            redacted = redacted.Replace(
                path,
                replacement,
                StringComparison.OrdinalIgnoreCase);
        }

        redacted = QuotedAbsolutePathRegex().Replace(
            redacted,
            match => $"{match.Groups["quote"].Value}[REDACTED_PATH]{match.Groups["quote"].Value}");
        return UnquotedAbsolutePathRegex().Replace(redacted, "[REDACTED_PATH]");
    }

    private static void AddReplacement(
        IDictionary<string, string> replacements,
        string path,
        string replacement)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        replacements[path.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar)] = replacement;
    }

    [GeneratedRegex(
        "(?<quote>[\"'])(?:(?:[A-Za-z]:[\\\\/])|(?:\\\\\\\\))[^\r\n\"']+\\k<quote>",
        RegexOptions.CultureInvariant)]
    private static partial Regex QuotedAbsolutePathRegex();

    [GeneratedRegex(
        "(?<![A-Za-z0-9])(?:(?:[A-Za-z]:[\\\\/])|(?:\\\\\\\\))[^\r\n\t<>|\"?*]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex UnquotedAbsolutePathRegex();
}
