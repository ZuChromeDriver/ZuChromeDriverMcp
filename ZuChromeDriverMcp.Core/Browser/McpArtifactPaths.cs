using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Core.Browser;

public static class McpArtifactPaths
{
    public static string ResolveOutputPath(McpHostOptions options, string filePath, string defaultFileName)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(filePath))
        {
            var directory = options.GetArtifactsDirectory();
            Directory.CreateDirectory(directory);
            var safeName = Path.GetFileName(defaultFileName);
            return Path.Combine(directory, $"{Guid.NewGuid():N}_{safeName}");
        }

        var path = Path.GetFullPath(filePath);
        var directoryName = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryName))
            Directory.CreateDirectory(directoryName);

        return path;
    }

    public static string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = "." + extension;

        return Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? path
            : path + extension;
    }
}
