using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public static class ProfileAssignmentService
{
    public static bool IsAssigned(AddonProfile profile, Addon addon)
    {
        string packageIdentity = AddonIdentity.GetPackageIdentity(addon);
        string canonicalPath = AddonIdentity.GetCanonicalPath(addon);
        return profile.Addons.Any(reference =>
                   ReferenceMatches(reference, packageIdentity, canonicalPath)) ||
               profile.AddonFolderNames.Contains(
                   addon.FolderName,
                   StringComparer.OrdinalIgnoreCase);
    }

    public static void Toggle(AddonProfile profile, Addon addon)
    {
        string packageIdentity = AddonIdentity.GetPackageIdentity(addon);
        string canonicalPath = AddonIdentity.GetCanonicalPath(addon);
        bool wasAssigned = IsAssigned(profile, addon);

        profile.Addons.RemoveAll(reference =>
            ReferenceMatches(reference, packageIdentity, canonicalPath));
        profile.AddonFolderNames.RemoveAll(folderName => folderName.Equals(
            addon.FolderName,
            StringComparison.OrdinalIgnoreCase));

        if (!wasAssigned)
        {
            // Explicitly selecting a package at a new location confirms the
            // rebind and replaces any stale path for the same package.
            profile.Addons.RemoveAll(reference =>
                reference.PackageIdentity.Equals(
                    packageIdentity,
                    StringComparison.OrdinalIgnoreCase));
            profile.Addons.Add(CreateReference(addon));
        }
    }

    public static int MigrateLegacyAssignments(
        AddonProfile profile,
        IReadOnlyList<Addon> addons)
    {
        int migratedCount = 0;
        foreach (string folderName in profile.AddonFolderNames.ToArray())
        {
            Addon[] matches = addons
                .Where(addon => addon.IsManagedLibraryAddon &&
                    addon.FolderName.Equals(
                        folderName,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                continue;
            }

            Addon addon = matches[0];
            string packageIdentity = AddonIdentity.GetPackageIdentity(addon);
            string canonicalPath = AddonIdentity.GetCanonicalPath(addon);
            if (!profile.Addons.Any(reference =>
                    ReferenceMatches(reference, packageIdentity, canonicalPath)))
            {
                profile.Addons.Add(CreateReference(addon));
            }

            profile.AddonFolderNames.RemoveAll(name => name.Equals(
                folderName,
                StringComparison.OrdinalIgnoreCase));
            migratedCount++;
        }

        return migratedCount;
    }

    public static ProfileAddonReference CreateReference(Addon addon) => new()
    {
        PackageIdentity = AddonIdentity.GetPackageIdentity(addon),
        SourcePath = AddonIdentity.GetCanonicalPath(addon),
        DisplayName = addon.Name,
        FolderName = addon.FolderName
    };

    public static bool ReferenceMatches(
        ProfileAddonReference reference,
        string packageIdentity,
        string canonicalPath)
    {
        string referencePath;
        try
        {
            referencePath = AddonIdentity.CanonicalizePath(reference.SourcePath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException)
        {
            return false;
        }

        return reference.PackageIdentity.Equals(
                   packageIdentity,
                   StringComparison.OrdinalIgnoreCase) &&
               referencePath.Equals(
                   canonicalPath,
                   StringComparison.OrdinalIgnoreCase);
    }
}
