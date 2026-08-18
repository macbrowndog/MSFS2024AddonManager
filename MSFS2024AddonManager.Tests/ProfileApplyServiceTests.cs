using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using Xunit;

namespace MSFS2024AddonManager.Tests;

public sealed class ProfileApplyServiceTests
{
    private const string Community = @"C:\MSFS\Community";

    [Fact]
    public void BuildPlan_EnablesAssignedAndDisablesUnassignedManagedAddons()
    {
        Addon assigned = CreateAddon("assigned", @"D:\Library\assigned");
        Addon unassigned = CreateAddon(
            "unassigned",
            @"D:\Library\unassigned",
            [Community]);
        Addon unmanaged = CreateAddon(
            "community-only",
            @"C:\MSFS\Community\community-only",
            [Community],
            isManaged: false);
        AddonProfile profile = CreateProfile("assigned");
        var service = new ProfileApplyService(new RecordingLinkService());

        ProfileApplyPlan plan = service.BuildPlan(
            profile,
            [assigned, unassigned, unmanaged],
            Community);

        Assert.True(plan.CanApply);
        Assert.Collection(
            plan.Operations,
            operation =>
            {
                Assert.Equal(ProfileApplyOperationType.Disable, operation.Type);
                Assert.Same(unassigned, operation.Addon);
            },
            operation =>
            {
                Assert.Equal(ProfileApplyOperationType.Enable, operation.Type);
                Assert.Same(assigned, operation.Addon);
            });
        Assert.DoesNotContain(plan.Operations, operation =>
            ReferenceEquals(operation.Addon, unmanaged));
    }

    [Fact]
    public void BuildPlan_RejectsAnUnavailableAssignedAddon()
    {
        AddonProfile profile = CreateProfile("disconnected-package");
        var service = new ProfileApplyService(new RecordingLinkService());

        ProfileApplyPlan plan = service.BuildPlan(profile, [], Community);

        Assert.False(plan.CanApply);
        Assert.Contains(plan.Errors, error => error.Contains(
            "unavailable",
            StringComparison.OrdinalIgnoreCase));
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void BuildPlan_RejectsDuplicateAssignedFolderNames()
    {
        Addon first = CreateAddon("duplicate", @"D:\LibraryOne\duplicate");
        Addon second = CreateAddon("duplicate", @"E:\LibraryTwo\duplicate");
        AddonProfile profile = CreateProfile("duplicate");
        var service = new ProfileApplyService(new RecordingLinkService());

        ProfileApplyPlan plan = service.BuildPlan(
            profile,
            [first, second],
            Community);

        Assert.False(plan.CanApply);
        Assert.Contains(plan.Errors, error => error.Contains(
            "duplicate",
            StringComparison.OrdinalIgnoreCase));
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void BuildPlan_DistinguishesDuplicateFolderNamesByStableReference()
    {
        Addon first = CreateAddon("duplicate", @"D:\LibraryOne\duplicate");
        Addon second = CreateAddon(
            "duplicate",
            @"E:\LibraryTwo\duplicate",
            [Community]);
        AddonProfile profile = CreateProfile();
        profile.Addons.Add(ProfileAssignmentService.CreateReference(first));
        var service = new ProfileApplyService(new RecordingLinkService());

        ProfileApplyPlan plan = service.BuildPlan(
            profile,
            [first, second],
            Community);

        Assert.True(plan.CanApply);
        Assert.Collection(
            plan.Operations,
            operation =>
            {
                Assert.Equal(ProfileApplyOperationType.Disable, operation.Type);
                Assert.Same(second, operation.Addon);
            },
            operation =>
            {
                Assert.Equal(ProfileApplyOperationType.Enable, operation.Type);
                Assert.Same(first, operation.Addon);
            });
    }

    [Fact]
    public void BuildPlan_RejectsAStoredReferenceWhenItsPackageMoved()
    {
        Addon previous = CreateAddon("package", @"D:\OldLibrary\package");
        Addon moved = CreateAddon("package", @"E:\NewLibrary\package");
        var profile = new AddonProfile { Name = "Test" };
        profile.Addons.Add(ProfileAssignmentService.CreateReference(previous));
        var service = new ProfileApplyService(new RecordingLinkService());

        ProfileApplyPlan plan = service.BuildPlan(profile, [moved], Community);

        Assert.False(plan.CanApply);
        Assert.Contains(plan.Errors, error => error.Contains(
            "moved from",
            StringComparison.OrdinalIgnoreCase));
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void Apply_CompletesEveryPlannedOperation()
    {
        Addon first = CreateAddon("first", @"D:\Library\first");
        Addon second = CreateAddon("second", @"D:\Library\second", [Community]);
        ProfileApplyPlan plan = CreatePlan(
            new(ProfileApplyOperationType.Enable, first),
            new(ProfileApplyOperationType.Disable, second));
        var links = new RecordingLinkService();

        ProfileApplyResult result = new ProfileApplyService(links).Apply(plan);

        Assert.True(result.Success);
        Assert.Equal(2, result.CompletedOperationCount);
        Assert.False(result.RollbackAttempted);
        Assert.Equal(
            ["Enable:first", "Disable:second"],
            links.Calls);
    }

    [Fact]
    public void Apply_RollsBackCompletedOperationsInReverseOrderAfterFailure()
    {
        Addon first = CreateAddon("first", @"D:\Library\first");
        Addon second = CreateAddon("second", @"D:\Library\second");
        Addon third = CreateAddon("third", @"D:\Library\third", [Community]);
        ProfileApplyPlan plan = CreatePlan(
            new(ProfileApplyOperationType.Enable, first),
            new(ProfileApplyOperationType.Enable, second),
            new(ProfileApplyOperationType.Disable, third));
        var links = new RecordingLinkService(failingCalls: [3]);

        ProfileApplyResult result = new ProfileApplyService(links).Apply(plan);

        Assert.False(result.Success);
        Assert.True(result.RollbackAttempted);
        Assert.True(result.RollbackSucceeded);
        Assert.Equal(
            [
                "Enable:first",
                "Enable:second",
                "Disable:third",
                "Disable:second",
                "Disable:first"
            ],
            links.Calls);
    }

    [Fact]
    public void Apply_ReportsAnIncompleteRollback()
    {
        Addon first = CreateAddon("first", @"D:\Library\first");
        Addon second = CreateAddon("second", @"D:\Library\second", [Community]);
        ProfileApplyPlan plan = CreatePlan(
            new(ProfileApplyOperationType.Enable, first),
            new(ProfileApplyOperationType.Disable, second));
        var links = new RecordingLinkService(failingCalls: [2, 3]);

        ProfileApplyResult result = new ProfileApplyService(links).Apply(plan);

        Assert.False(result.Success);
        Assert.True(result.RollbackAttempted);
        Assert.False(result.RollbackSucceeded);
        Assert.Contains("Rollback was incomplete", result.Message);
    }

    private static ProfileApplyPlan CreatePlan(
        params ProfileApplyOperation[] operations) =>
        new(new AddonProfile { Name = "Test" }, Community, operations, []);

    private static AddonProfile CreateProfile(params string[] folderNames) => new()
    {
        Name = "Test",
        AddonFolderNames = [.. folderNames]
    };

    private static Addon CreateAddon(
        string folderName,
        string path,
        IReadOnlyList<string>? enabledPaths = null,
        bool isManaged = true) => new()
        {
            Name = folderName,
            FolderName = folderName,
            Path = path,
            LibraryPath = Path.GetDirectoryName(path) ?? string.Empty,
            EnabledCommunityPaths = enabledPaths ?? [],
            IsManagedLibraryAddon = isManaged
        };

    private sealed class RecordingLinkService(IEnumerable<int>? failingCalls = null)
        : IAddonLinkService
    {
        private readonly HashSet<int> failingCalls = failingCalls?.ToHashSet() ?? [];
        private int callCount;

        public List<string> Calls { get; } = [];

        public LinkOperationResult Enable(Addon addon, string communityFolder) =>
            Record(ProfileApplyOperationType.Enable, addon);

        public LinkOperationResult Disable(Addon addon, string communityFolder) =>
            Record(ProfileApplyOperationType.Disable, addon);

        private LinkOperationResult Record(
            ProfileApplyOperationType type,
            Addon addon)
        {
            callCount++;
            Calls.Add($"{type}:{addon.Name}");
            return failingCalls.Contains(callCount)
                ? LinkOperationResult.Failed("Injected failure")
                : LinkOperationResult.Succeeded("Completed");
        }
    }
}
