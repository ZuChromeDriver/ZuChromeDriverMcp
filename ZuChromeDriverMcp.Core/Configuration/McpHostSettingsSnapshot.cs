namespace ZuChromeDriverMcp.Core.Configuration;

/// <summary>
/// Persisted WPF settings. Each toggle is stored as its own JSON field.
/// </summary>
public sealed class McpHostSettingsSnapshot
{
    public int Port { get; set; }

    public bool Headless { get; set; }

    public string CommandLineArguments { get; set; }

    public bool AttachOnly { get; set; }

    public int? WindowWidth { get; set; }

    public int? WindowHeight { get; set; }

    public bool EnableFrameTrackerOnConnect { get; set; }

    public bool EnableDomTrackerOnConnect { get; set; }

    public bool EnableBrowserLogCaptureOnConnect { get; set; }

    public bool EnableNetworkCaptureOnConnect { get; set; } = true;

    public bool EnableConsoleCaptureOnConnect { get; set; } = true;

    public bool EnableListNetworkRequestsTool { get; set; } = true;

    public bool EnableListConsoleMessagesTool { get; set; } = true;

    public bool EnableEvaluateTool { get; set; } = true;

    public bool EnableScreenshotTool { get; set; } = true;

    public bool EnableTakeSnapshotTool { get; set; } = true;

    public bool CategoryInput { get; set; } = true;

    public bool CategoryNavigation { get; set; } = true;

    public bool CategoryMemory { get; set; } = true;

    public bool CategoryEmulation { get; set; } = true;

    public int McpHttpPort { get; set; } = 5100;

    public string McpHttpPath { get; set; } = "/mcp";

    public bool ConnectChromeOnStartup { get; set; }

    public McpArtifactsLocation ArtifactsLocation { get; set; } = McpArtifactsLocation.Executable;
}
