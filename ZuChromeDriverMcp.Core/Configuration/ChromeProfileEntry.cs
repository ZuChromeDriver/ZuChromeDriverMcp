namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class ChromeProfileEntry
{
    public const string TempProfileId = "temp";

    public string Id { get; set; }

    public string Name { get; set; }

    public ChromeProfileKind Kind { get; set; }

    /// <summary>
    /// Subfolder name under the <c>Profiles</c> directory next to the executable (for <see cref="ChromeProfileKind.Folder"/>).
    /// </summary>
    public string FolderName { get; set; }

    /// <summary>
    /// Absolute path to Chrome user-data-dir (for <see cref="ChromeProfileKind.CustomPath"/>).
    /// </summary>
    public string UserDir { get; set; }

    public bool IsBuiltIn { get; set; }

    public static ChromeProfileEntry CreateTemp()
    {
        return new ChromeProfileEntry
        {
            Id = TempProfileId,
            Name = "Temp",
            Kind = ChromeProfileKind.Temp,
            IsBuiltIn = true,
        };
    }

    public static ChromeProfileEntry CreateFolder(string name, string folderName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(folderName))
            throw new ArgumentException("Folder name is required.", nameof(folderName));

        return new ChromeProfileEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Kind = ChromeProfileKind.Folder,
            FolderName = SanitizeFolderName(folderName),
        };
    }

    public static ChromeProfileEntry CreateCustomPath(string name, string userDir)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(userDir))
            throw new ArgumentException("User directory is required.", nameof(userDir));

        return new ChromeProfileEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Kind = ChromeProfileKind.CustomPath,
            UserDir = userDir.Trim(),
        };
    }

    public string GetDisplayPath()
    {
        return Kind switch
        {
            ChromeProfileKind.Temp => "%TEMP% (удаляется при закрытии)",
            ChromeProfileKind.Folder => ChromeProfilePaths.GetFolderProfilePath(FolderName),
            ChromeProfileKind.CustomPath => UserDir ?? "",
            _ => "",
        };
    }

    static string SanitizeFolderName(string folderName)
    {
        var trimmed = folderName.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            trimmed = trimmed.Replace(invalid, '_');

        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Folder name is invalid.", nameof(folderName));

        return trimmed;
    }
}
