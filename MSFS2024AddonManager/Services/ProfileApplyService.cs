using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class ProfileApplyService(IAddonLinkService linkService)
{
    private readonly IAddonLinkService linkService = linkService ??
        throw new ArgumentNullException(nameof(linkService));

    public ProfileApplyPlan BuildPlan(
        AddonProfile profile,
        IReadOnlyList<Addon> addons,
        string communityFolder)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(addons);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(communityFolder))
        {
            errors.Add("Configure the default Community folder in Settings first.");
            return new ProfileApplyPlan(profile, communityFolder, [], errors);
        }

        string targetCommunity;
        try
        {
            targetCommunity = NormalizePath(communityFolder);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException)
        {
            errors.Add("The configured default Community folder path is invalid.");
            return new ProfileApplyPlan(profile, communityFolder, [], errors);
        }

        Addon[] managedAddons = addons
            .Where(addon => addon.IsManagedLibraryAddon)
            .ToArray();
        var assignedAddons = new Dictionary<string, Addon>(
            StringComparer.OrdinalIgnoreCase);

        foreach (ProfileAddonReference reference in profile.Addons)
        {
            string referencePath;
            try
            {
                referencePath = AddonIdentity.CanonicalizePath(reference.SourcePath);
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException)
            {
                errors.Add(
                    $"Assigned addon '{reference.DisplayName}' has an invalid stored source path.");
                continue;
            }

            Addon[] identityMatches = managedAddons
                .Where(addon => AddonIdentity.GetPackageIdentity(addon).Equals(
                    reference.PackageIdentity,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Addon? exactMatch = identityMatches.FirstOrDefault(addon =>
                AddonIdentity.GetCanonicalPath(addon).Equals(
                    referencePath,
                    StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
            {
                assignedAddons.TryAdd(referencePath, exactMatch);
                continue;
            }

            string displayName = string.IsNullOrWhiteSpace(reference.DisplayName)
                ? reference.FolderName
                : reference.DisplayName;
            if (identityMatches.Length == 1)
            {
                errors.Add(
                    $"Assigned addon '{displayName}' moved from '{reference.SourcePath}' to '{identityMatches[0].Path}'. Reassign it to confirm the new source.");
            }
            else if (identityMatches.Length > 1)
            {
                errors.Add(
                    $"Assigned addon '{displayName}' is available in multiple locations but not at its stored source path.");
            }
            else
            {
                errors.Add(
                    $"Assigned addon '{displayName}' is unavailable at '{reference.SourcePath}'.");
            }
        }

        HashSet<string> legacyAssignedNames = profile.AddonFolderNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        ILookup<string, Addon> managedByFolderName = managedAddons
            .ToLookup(addon => addon.FolderName, StringComparer.OrdinalIgnoreCase);

        foreach (string assignedName in legacyAssignedNames.OrderBy(
                     name => name,
                     StringComparer.CurrentCultureIgnoreCase))
        {
            Addon[] matches = managedByFolderName[assignedName].ToArray();
            if (matches.Length == 0)
            {
                errors.Add(
                    $"Assigned addon '{assignedName}' is unavailable. Reconnect its library or remove it from the profile.");
            }
            else if (matches.Length > 1)
            {
                errors.Add(
                    $"Assigned addon '{assignedName}' is ambiguous because multiple libraries contain that folder name.");
            }
            else
            {
                Addon addon = matches[0];
                assignedAddons.TryAdd(AddonIdentity.GetCanonicalPath(addon), addon);
            }
        }

        if (errors.Count > 0)
        {
            return new ProfileApplyPlan(profile, targetCommunity, [], errors.Distinct().ToArray());
        }

        var operations = new List<ProfileApplyOperation>();
        foreach (Addon addon in assignedAddons.Values)
        {
            if (!IsEnabledAt(addon, targetCommunity))
            {
                operations.Add(new ProfileApplyOperation(
                    ProfileApplyOperationType.Enable,
                    addon));
            }
        }

        HashSet<string> assignedPaths = assignedAddons.Keys.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        foreach (Addon addon in addons.Where(addon =>
                     addon.IsManagedLibraryAddon &&
                     IsEnabledAt(addon, targetCommunity) &&
                     !assignedPaths.Contains(AddonIdentity.GetCanonicalPath(addon))))
        {
            operations.Add(new ProfileApplyOperation(
                ProfileApplyOperationType.Disable,
                addon));
        }

        ProfileApplyOperation[] orderedOperations = operations
            .OrderBy(operation =>
                operation.Type == ProfileApplyOperationType.Disable ? 0 : 1)
            .ThenBy(
                operation => operation.Addon.Name,
                StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new ProfileApplyPlan(
            profile,
            targetCommunity,
            orderedOperations,
            []);
    }

    public ProfileApplyResult Apply(ProfileApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.CanApply)
        {
            return ProfileApplyResult.Failed(
                "The profile plan contains validation errors and was not applied.");
        }

        var completed = new List<ProfileApplyOperation>();
        foreach (ProfileApplyOperation operation in plan.Operations)
        {
            LinkOperationResult operationResult = Execute(operation, plan.CommunityFolder);
            if (!operationResult.Success)
            {
                return RollBack(
                    plan,
                    completed,
                    operation,
                    operationResult.Message);
            }

            completed.Add(operation);
        }

        return ProfileApplyResult.Succeeded(
            plan.Operations.Count == 0
                ? "The Community folder already matches this profile."
                : $"Profile applied successfully. {plan.Operations.Count} change(s) completed.",
            plan.Operations.Count);
    }

    private ProfileApplyResult RollBack(
        ProfileApplyPlan plan,
        IReadOnlyList<ProfileApplyOperation> completed,
        ProfileApplyOperation failedOperation,
        string failureMessage)
    {
        var rollbackFailures = new List<string>();
        foreach (ProfileApplyOperation operation in completed.Reverse())
        {
            ProfileApplyOperation inverse = operation.Inverse();
            LinkOperationResult rollbackResult = Execute(inverse, plan.CommunityFolder);
            if (!rollbackResult.Success)
            {
                rollbackFailures.Add(
                    $"{inverse.Type} {inverse.Addon.Name}: {rollbackResult.Message}");
            }
        }

        string message =
            $"Could not {failedOperation.Type.ToString().ToLowerInvariant()} '{failedOperation.Addon.Name}': {failureMessage}";
        if (completed.Count == 0)
        {
            return ProfileApplyResult.Failed(message);
        }

        if (rollbackFailures.Count == 0)
        {
            return ProfileApplyResult.Failed(
                $"{message} All earlier changes were rolled back.",
                rollbackAttempted: true,
                rollbackSucceeded: true,
                completed.Count);
        }

        return ProfileApplyResult.Failed(
            $"{message} Rollback was incomplete: {string.Join("; ", rollbackFailures)}",
            rollbackAttempted: true,
            rollbackSucceeded: false,
            completed.Count);
    }

    private LinkOperationResult Execute(
        ProfileApplyOperation operation,
        string communityFolder) =>
        operation.Type == ProfileApplyOperationType.Enable
            ? linkService.Enable(operation.Addon, communityFolder)
            : linkService.Disable(operation.Addon, communityFolder);

    private static bool IsEnabledAt(Addon addon, string communityFolder) =>
        addon.EnabledCommunityPaths.Any(path =>
        {
            try
            {
                return string.Equals(
                    NormalizePath(path),
                    communityFolder,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException)
            {
                return false;
            }
        });

    private static string NormalizePath(string path) => Path.GetFullPath(path)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

public sealed record ProfileApplyPlan(
    AddonProfile Profile,
    string CommunityFolder,
    IReadOnlyList<ProfileApplyOperation> Operations,
    IReadOnlyList<string> Errors)
{
    public bool CanApply => Errors.Count == 0;

    public int EnableCount => Operations.Count(operation =>
        operation.Type == ProfileApplyOperationType.Enable);

    public int DisableCount => Operations.Count(operation =>
        operation.Type == ProfileApplyOperationType.Disable);
}

public sealed record ProfileApplyOperation(
    ProfileApplyOperationType Type,
    Addon Addon)
{
    public ProfileApplyOperation Inverse() => new(
        Type == ProfileApplyOperationType.Enable
            ? ProfileApplyOperationType.Disable
            : ProfileApplyOperationType.Enable,
        Addon);
}

public enum ProfileApplyOperationType
{
    Enable,
    Disable
}

public sealed record ProfileApplyResult(
    bool Success,
    string Message,
    bool RollbackAttempted,
    bool RollbackSucceeded,
    int CompletedOperationCount)
{
    public static ProfileApplyResult Succeeded(string message, int completedCount) =>
        new(true, message, false, true, completedCount);

    public static ProfileApplyResult Failed(
        string message,
        bool rollbackAttempted = false,
        bool rollbackSucceeded = false,
        int completedCount = 0) =>
        new(false, message, rollbackAttempted, rollbackSucceeded, completedCount);
}
