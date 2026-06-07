namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpRuntimeSnapshot
{
    public bool IsConnected { get; set; }

    public int ChromePort { get; set; }

    public string ActivePageUrl { get; set; } = "";

    public int NetworkCount { get; set; }

    public int ConsoleCount { get; set; }

    public string LastError { get; set; } = "";

    public string LastArtifactPath { get; set; } = "";

    public IReadOnlyList<McpConsoleEntry> RecentConsole { get; set; } = Array.Empty<McpConsoleEntry>();

    public IReadOnlyList<McpNetworkEntry> RecentNetwork { get; set; } = Array.Empty<McpNetworkEntry>();
}
