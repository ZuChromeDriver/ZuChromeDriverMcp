namespace ZuChromeDriverMcp.Core.Configuration;

public enum McpArtifactsLocation
{
    /// <summary>
    /// <c>{exe_dir}/Temp/zu-chrome-driver-mcp</c> — default.
    /// </summary>
    Executable,

    /// <summary>
    /// <c>%TEMP%/zu-chrome-driver-mcp</c> via <see cref="Path.GetTempPath"/>.
    /// </summary>
    SystemTemp,
}
