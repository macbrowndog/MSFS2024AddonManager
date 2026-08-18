using System.Text.Json;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using Xunit;

namespace MSFS2024AddonManager.Tests;

public sealed class AtomicPersistenceTests
{
    [Fact]
    public void SettingsSave_RetainsPreviousVersionAsValidatedBackup()
    {
        using var directory = new TemporaryDirectory();
        string settingsPath = Path.Combine(directory.Path, "settings.json");
        var service = new SettingsService(settingsPath);

        service.Save(new AppSettings { CommunityFolder = "first" });
        service.Save(new AppSettings { CommunityFolder = "second" });

        AppSettings current = service.Load();
        AppSettings backup = Deserialize<AppSettings>($"{settingsPath}.bak");
        Assert.Equal("second", current.CommunityFolder);
        Assert.Equal("first", backup.CommunityFolder);
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void ProfileSave_CreatesValidatedBackupOnFirstSave()
    {
        using var directory = new TemporaryDirectory();
        string profilesPath = Path.Combine(directory.Path, "profiles.json");
        var service = new ProfileService(profilesPath);
        var collection = new ProfileCollection
        {
            Profiles = [new AddonProfile { Name = "Touring" }]
        };

        service.Save(collection);

        ProfileCollection backup = Deserialize<ProfileCollection>($"{profilesPath}.bak");
        Assert.Equal("Touring", Assert.Single(backup.Profiles).Name);
    }

    [Fact]
    public void SettingsLoad_RecoversCorruptPrimaryFromBackup()
    {
        using var directory = new TemporaryDirectory();
        string settingsPath = Path.Combine(directory.Path, "settings.json");
        var service = new SettingsService(settingsPath);
        service.Save(new AppSettings { CommunityFolder = "known-good" });
        File.WriteAllText(settingsPath, "{ corrupt");

        AppSettings recovered = service.Load();

        Assert.Equal("known-good", recovered.CommunityFolder);
        Assert.Equal("known-good", Deserialize<AppSettings>(settingsPath).CommunityFolder);
        Assert.Equal("known-good", Deserialize<AppSettings>($"{settingsPath}.bak").CommunityFolder);
    }

    [Fact]
    public void ProfileLoad_RecoversMissingPrimaryFromBackup()
    {
        using var directory = new TemporaryDirectory();
        string profilesPath = Path.Combine(directory.Path, "profiles.json");
        var service = new ProfileService(profilesPath);
        service.Save(new ProfileCollection
        {
            Profiles = [new AddonProfile { Name = "Bush flying" }]
        });
        File.Delete(profilesPath);

        ProfileCollection recovered = service.Load();

        Assert.Equal("Bush flying", Assert.Single(recovered.Profiles).Name);
        Assert.True(File.Exists(profilesPath));
    }

    [Fact]
    public void SettingsLoad_WhenPrimaryAndBackupAreCorrupt_ThrowsAndPreservesBoth()
    {
        using var directory = new TemporaryDirectory();
        string settingsPath = Path.Combine(directory.Path, "settings.json");
        string backupPath = $"{settingsPath}.bak";
        var service = new SettingsService(settingsPath);
        const string corruptPrimary = "{ primary-corrupt";
        const string corruptBackup = "{ backup-corrupt";
        File.WriteAllText(settingsPath, corruptPrimary);
        File.WriteAllText(backupPath, corruptBackup);

        JsonPersistenceException error = Assert.Throws<JsonPersistenceException>(service.Load);

        Assert.Equal(settingsPath, error.PrimaryPath);
        Assert.Equal(backupPath, error.BackupPath);
        Assert.Equal(corruptPrimary, File.ReadAllText(settingsPath));
        Assert.Equal(corruptBackup, File.ReadAllText(backupPath));
    }

    [Fact]
    public void ProfileLoad_WhenPrimaryIsCorruptAndBackupMissing_DoesNotResetProfiles()
    {
        using var directory = new TemporaryDirectory();
        string profilesPath = Path.Combine(directory.Path, "profiles.json");
        var service = new ProfileService(profilesPath);
        const string corruptProfiles = "not-json";
        File.WriteAllText(profilesPath, corruptProfiles);

        Assert.Throws<JsonPersistenceException>(service.Load);
        Assert.Equal(corruptProfiles, File.ReadAllText(profilesPath));
    }

    private static T Deserialize<T>(string path)
        where T : class
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ??
            throw new InvalidOperationException($"'{path}' deserialized to null.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory(
                "MSFS2024AddonManager.PersistenceTests-").FullName;
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
