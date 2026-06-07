namespace ZuChromeDriverMcp.Core.Configuration;

public static class ChromeProfileResolver
{
    public static void ApplyToOptions(ChromeProfileEntry entry, McpHostOptions options)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.ActiveProfileId = entry.Id;
        options.ActiveProfileName = entry.Name;

        switch (entry.Kind)
        {
            case ChromeProfileKind.Temp:
                options.IsTempProfile = true;
                options.UserDir = null;
                break;
            case ChromeProfileKind.Folder:
                ChromeProfilePaths.EnsureProfileDirectory(entry);
                options.IsTempProfile = false;
                options.UserDir = ChromeProfilePaths.GetFolderProfilePath(entry.FolderName);
                break;
            case ChromeProfileKind.CustomPath:
                ChromeProfilePaths.EnsureProfileDirectory(entry);
                options.IsTempProfile = false;
                options.UserDir = entry.UserDir;
                break;
            default:
                throw new InvalidOperationException($"Unsupported profile kind: {entry.Kind}");
        }
    }

    public static bool TryApplyStartupProfile(McpHostOptions options, string profileOverride)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (!string.IsNullOrWhiteSpace(options.UserDir))
            return false;

        var catalog = ChromeProfileCatalog.LoadOrCreateDefault();
        ChromeProfileEntry entry = null;
        if (!string.IsNullOrWhiteSpace(profileOverride))
            entry = catalog.FindByNameOrId(profileOverride);

        entry ??= catalog.GetSelectedProfile();
        if (entry == null)
            return false;

        ApplyToOptions(entry, options);
        return true;
    }
}
