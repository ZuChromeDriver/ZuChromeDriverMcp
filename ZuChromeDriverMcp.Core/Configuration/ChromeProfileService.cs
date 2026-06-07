namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class ChromeProfileService
{
    readonly object _sync = new();
    ChromeProfileCatalog _catalog;

    public ChromeProfileService()
    {
        Reload();
    }

    public IReadOnlyList<ChromeProfileEntry> Profiles
    {
        get
        {
            lock (_sync)
                return _catalog.Profiles.ToList();
        }
    }

    public string SelectedProfileId
    {
        get
        {
            lock (_sync)
                return _catalog.SelectedProfileId;
        }
    }

    public ChromeProfileEntry GetSelectedProfile()
    {
        lock (_sync)
            return _catalog.GetSelectedProfile();
    }

    public void Reload()
    {
        lock (_sync)
            _catalog = ChromeProfileCatalog.LoadOrCreateDefault();
    }

    public void ApplySelectedTo(McpHostOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        ChromeProfileEntry entry;
        lock (_sync)
            entry = _catalog.GetSelectedProfile();

        if (entry == null)
            throw new InvalidOperationException("No profile is selected.");

        ChromeProfileResolver.ApplyToOptions(entry, options);
    }

    public void SelectProfile(string profileId, McpHostOptions options)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile id is required.", nameof(profileId));

        lock (_sync)
        {
            var entry = _catalog.FindById(profileId);
            if (entry == null)
                throw new InvalidOperationException($"Profile '{profileId}' was not found.");

            _catalog.SelectedProfileId = entry.Id;
            _catalog.Save();
            if (options != null)
                ChromeProfileResolver.ApplyToOptions(entry, options);
        }
    }

    public ChromeProfileEntry AddFolderProfile(string name, string folderName)
    {
        lock (_sync)
        {
            var entry = ChromeProfileEntry.CreateFolder(name, folderName);
            if (_catalog.Profiles.Any(p => string.Equals(p.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Profile '{entry.Name}' already exists.");

            ChromeProfilePaths.EnsureProfileDirectory(entry);
            _catalog.Profiles.Add(entry);
            _catalog.Save();
            return entry;
        }
    }

    public ChromeProfileEntry AddCustomProfile(string name, string userDir)
    {
        lock (_sync)
        {
            var entry = ChromeProfileEntry.CreateCustomPath(name, userDir);
            if (_catalog.Profiles.Any(p => string.Equals(p.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Profile '{entry.Name}' already exists.");

            ChromeProfilePaths.EnsureProfileDirectory(entry);
            _catalog.Profiles.Add(entry);
            _catalog.Save();
            return entry;
        }
    }

    public void RemoveProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile id is required.", nameof(profileId));

        lock (_sync)
        {
            var entry = _catalog.FindById(profileId);
            if (entry == null)
                throw new InvalidOperationException($"Profile '{profileId}' was not found.");
            if (entry.IsBuiltIn)
                throw new InvalidOperationException("Built-in profiles cannot be removed.");

            _catalog.Profiles.Remove(entry);
            if (string.Equals(_catalog.SelectedProfileId, entry.Id, StringComparison.Ordinal))
                _catalog.SelectedProfileId = ChromeProfileEntry.TempProfileId;

            _catalog.Save();
        }
    }

    public ChromeProfileEntry ApplyProfileByNameOrId(string nameOrId, McpHostOptions options)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
            throw new ArgumentException("Profile name or id is required.", nameof(nameOrId));

        lock (_sync)
        {
            var entry = _catalog.FindByNameOrId(nameOrId);
            if (entry == null)
                throw new InvalidOperationException($"Profile '{nameOrId}' was not found.");

            _catalog.SelectedProfileId = entry.Id;
            _catalog.Save();
            if (options != null)
                ChromeProfileResolver.ApplyToOptions(entry, options);

            return entry;
        }
    }

    public void ApplyDirectProfile(McpHostOptions options, string userDir, bool isTempProfile)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.ActiveProfileId = null;
        options.ActiveProfileName = null;
        options.IsTempProfile = isTempProfile;
        options.UserDir = isTempProfile ? null : userDir;

        if (!isTempProfile && !string.IsNullOrWhiteSpace(userDir))
            Directory.CreateDirectory(userDir);
    }
}
