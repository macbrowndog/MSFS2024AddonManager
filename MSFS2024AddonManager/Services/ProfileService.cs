using System.Text.Json;
using MSFS2024AddonManager.Models;

namespace MSFS2024AddonManager.Services;

public sealed class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string profilesPath;

    public ProfileService()
    {
        profilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFS2024AddonManager",
            "profiles.json");
    }

    public ProfileCollection Load()
    {
        try
        {
            if (!File.Exists(profilesPath))
            {
                return new ProfileCollection();
            }

            return JsonSerializer.Deserialize<ProfileCollection>(
                File.ReadAllText(profilesPath),
                JsonOptions) ?? new ProfileCollection();
        }
        catch (JsonException)
        {
            return new ProfileCollection();
        }
        catch (IOException)
        {
            return new ProfileCollection();
        }
    }

    public void Save(ProfileCollection collection)
    {
        string? folder = Path.GetDirectoryName(profilesPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(
            profilesPath,
            JsonSerializer.Serialize(collection, JsonOptions));
    }
}
