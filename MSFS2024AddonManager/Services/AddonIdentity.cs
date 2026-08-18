using System.Security.Cryptography;
using System.Text;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public static class AddonIdentity
{
    public static string CreatePackageIdentity(
        string folderName,
        string title,
        string creator,
        string contentType)
    {
        string material = string.Join(
            '\n',
            NormalizeValue(folderName),
            NormalizeValue(title),
            NormalizeValue(creator),
            NormalizeValue(contentType));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

    public static string CanonicalizePath(string path) => Path.GetFullPath(path)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string GetCanonicalPath(Addon addon) =>
        string.IsNullOrWhiteSpace(addon.CanonicalPath)
            ? CanonicalizePath(addon.Path)
            : CanonicalizePath(addon.CanonicalPath);

    public static string GetPackageIdentity(Addon addon) =>
        string.IsNullOrWhiteSpace(addon.PackageIdentity)
            ? CreatePackageIdentity(
                addon.FolderName,
                addon.Name,
                addon.Author,
                addon.Category)
            : addon.PackageIdentity;

    private static string NormalizeValue(string value) =>
        value.Trim().ToUpperInvariant();
}
