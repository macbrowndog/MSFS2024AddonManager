using System.Text.Json;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AtomicJsonFileStore<ProfileCollection> store;

    public ProfileService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFS2024AddonManager",
            "profiles.json"))
    {
    }

    internal ProfileService(string profilesPath)
    {
        store = new AtomicJsonFileStore<ProfileCollection>(profilesPath, JsonOptions);
    }

    public ProfileCollection Load()
    {
        return store.Load(static () => new ProfileCollection(), Normalize);
    }

    public void Save(ProfileCollection collection)
    {
        Normalize(collection);
        store.Save(collection);
    }

    private static void Normalize(ProfileCollection collection)
    {
        collection.Profiles ??= [];
        foreach (AddonProfile profile in collection.Profiles)
        {
            profile.Addons ??= [];
            profile.AddonFolderNames ??= [];
        }
    }
}
