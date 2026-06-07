namespace ZuChromeDriverMcp.Core.Configuration;

public static class ChromeProfilePaths
{
    public static string GetProfilesRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "Profiles");
    }

    public static string GetCatalogFilePath()
    {
        return Path.Combine(GetProfilesRoot(), "profiles.json");
    }

    public static string GetFolderProfilePath(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            throw new ArgumentException("Folder name is required.", nameof(folderName));

        return Path.Combine(GetProfilesRoot(), folderName);
    }

    public static void EnsureProfilesRoot()
    {
        Directory.CreateDirectory(GetProfilesRoot());
    }

    public static void EnsureProfileDirectory(ChromeProfileEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        if (entry.Kind == ChromeProfileKind.Folder)
            Directory.CreateDirectory(GetFolderProfilePath(entry.FolderName));
        else if (entry.Kind == ChromeProfileKind.CustomPath && !string.IsNullOrWhiteSpace(entry.UserDir))
            Directory.CreateDirectory(entry.UserDir);
    }
}
