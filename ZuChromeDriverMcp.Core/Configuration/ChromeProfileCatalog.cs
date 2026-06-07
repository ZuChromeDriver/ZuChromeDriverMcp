using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class ChromeProfileCatalog
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string SelectedProfileId { get; set; } = ChromeProfileEntry.TempProfileId;

    public List<ChromeProfileEntry> Profiles { get; set; } = new();

    public static ChromeProfileCatalog LoadOrCreateDefault()
    {
        ChromeProfilePaths.EnsureProfilesRoot();
        var path = ChromeProfilePaths.GetCatalogFilePath();
        if (!File.Exists(path))
        {
            var catalog = CreateDefault();
            catalog.Save();
            return catalog;
        }

        try
        {
            var json = File.ReadAllText(path);
            var catalog = JsonSerializer.Deserialize<ChromeProfileCatalog>(json, JsonOptions);
            if (catalog == null || catalog.Profiles.Count == 0)
            {
                catalog = CreateDefault();
                catalog.Save();
                return catalog;
            }

            catalog.EnsureBuiltInProfiles();
            catalog.EnsureSelectedProfile();
            return catalog;
        }
        catch
        {
            var catalog = CreateDefault();
            catalog.Save();
            return catalog;
        }
    }

    public static ChromeProfileCatalog CreateDefault()
    {
        var catalog = new ChromeProfileCatalog
        {
            SelectedProfileId = ChromeProfileEntry.TempProfileId,
            Profiles =
            [
                ChromeProfileEntry.CreateTemp(),
                ChromeProfileEntry.CreateFolder("Profile1", "Profile1"),
                ChromeProfileEntry.CreateFolder("Profile2", "Profile2"),
            ],
        };

        foreach (var profile in catalog.Profiles)
            ChromeProfilePaths.EnsureProfileDirectory(profile);

        return catalog;
    }

    public void Save()
    {
        EnsureBuiltInProfiles();
        EnsureSelectedProfile();
        ChromeProfilePaths.EnsureProfilesRoot();
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ChromeProfilePaths.GetCatalogFilePath(), json);
    }

    public ChromeProfileEntry FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return Profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public ChromeProfileEntry FindByNameOrId(string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
            return null;

        var byId = FindById(nameOrId);
        if (byId != null)
            return byId;

        return Profiles.FirstOrDefault(p => string.Equals(p.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
    }

    public ChromeProfileEntry GetSelectedProfile()
    {
        return FindById(SelectedProfileId) ?? Profiles.FirstOrDefault();
    }

    void EnsureBuiltInProfiles()
    {
        if (Profiles.All(p => !string.Equals(p.Id, ChromeProfileEntry.TempProfileId, StringComparison.Ordinal)))
            Profiles.Insert(0, ChromeProfileEntry.CreateTemp());
    }

    void EnsureSelectedProfile()
    {
        if (GetSelectedProfile() == null)
            SelectedProfileId = ChromeProfileEntry.TempProfileId;
    }
}
