using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Core.Tools;

public sealed class McpToolAvailability
{
    readonly McpHostOptions _options;

    public McpToolAvailability(McpHostOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsAvailable(string toolName)
    {
        if (!McpToolCatalog.TryGet(toolName, out var definition))
            return true;

        if (!IsGroupEnabled(definition.Group))
            return false;

        foreach (var behavior in definition.RequiredBehaviors)
        {
            if (!IsBehaviorEnabled(behavior))
                return false;
        }

        return true;
    }

    public string GetUnavailableReason(string toolName)
    {
        if (!McpToolCatalog.TryGet(toolName, out var definition))
            return $"Tool \"{toolName}\" is not available.";

        if (!IsGroupEnabled(definition.Group))
            return GetGroupDisabledMessage(definition.Group);

        foreach (var behavior in definition.RequiredBehaviors)
        {
            if (!IsBehaviorEnabled(behavior))
                return GetBehaviorRequiredMessage(toolName, behavior);
        }

        return $"Tool \"{toolName}\" is not available.";
    }

    public bool IsGroupEnabled(McpToolGroup group)
    {
        return group switch
        {
            McpToolGroup.Meta => true,
            McpToolGroup.NetworkRequests => _options.EnableListNetworkRequestsTool,
            McpToolGroup.ConsoleMessages => _options.EnableListConsoleMessagesTool,
            McpToolGroup.Evaluate => _options.EnableEvaluateTool,
            McpToolGroup.Screenshot => _options.EnableScreenshotTool,
            McpToolGroup.Snapshot => _options.EnableTakeSnapshotTool,
            McpToolGroup.Input => _options.Categories.Input,
            McpToolGroup.Navigation => _options.Categories.Navigation,
            McpToolGroup.Memory => _options.Categories.Memory,
            _ => true,
        };
    }

    public bool IsBehaviorEnabled(McpBrowserBehavior behavior)
    {
        return behavior switch
        {
            McpBrowserBehavior.FrameTracker => _options.EnableFrameTrackerOnConnect,
            McpBrowserBehavior.DomTracker => _options.EnableDomTrackerOnConnect,
            McpBrowserBehavior.BrowserLog => _options.EnableBrowserLogCaptureOnConnect,
            McpBrowserBehavior.NetworkCdp => _options.EnableNetworkCaptureOnConnect,
            McpBrowserBehavior.ConsoleCdp => _options.EnableConsoleCaptureOnConnect,
            _ => true,
        };
    }

    static string GetGroupDisabledMessage(McpToolGroup group)
    {
        return group switch
        {
            McpToolGroup.NetworkRequests =>
                "Tool group Network requests is disabled. Enable it in Settings or set EnableListNetworkRequestsTool=true.",
            McpToolGroup.ConsoleMessages =>
                "Tool group Console messages is disabled. Enable it in Settings or set EnableListConsoleMessagesTool=true.",
            McpToolGroup.Evaluate =>
                "Tool group Evaluate is disabled. Enable it in Settings or set EnableEvaluateTool=true.",
            McpToolGroup.Screenshot =>
                "Tool group Screenshot is disabled. Enable it in Settings or set EnableScreenshotTool=true.",
            McpToolGroup.Snapshot =>
                "Tool group Snapshot is disabled. Enable it in Settings or set EnableTakeSnapshotTool=true.",
            McpToolGroup.Input =>
                "Tool category Input is disabled. Enable it with --category-input=true or env ZU_CHROME_DRIVER_MCP_CATEGORY_INPUT=true.",
            McpToolGroup.Navigation =>
                "Tool category Navigation is disabled. Enable it with --category-navigation=true or env ZU_CHROME_DRIVER_MCP_CATEGORY_NAVIGATION=true.",
            McpToolGroup.Memory =>
                "Tool category Memory is disabled. Enable it with --category-memory=true or env ZU_CHROME_DRIVER_MCP_CATEGORY_MEMORY=true.",
            _ => "Tool group is disabled.",
        };
    }

    static string GetBehaviorRequiredMessage(string toolName, McpBrowserBehavior behavior)
    {
        return behavior switch
        {
            McpBrowserBehavior.NetworkCdp =>
                $"Tool \"{toolName}\" requires Network (CDP) capture. Enable Network (CDP) in browser behavior settings or env ZU_CHROME_DRIVER_MCP_ENABLE_NETWORK_CAPTURE=true.",
            McpBrowserBehavior.ConsoleCdp =>
                $"Tool \"{toolName}\" requires Console (CDP) capture. Enable Console (CDP) in browser behavior settings or env ZU_CHROME_DRIVER_MCP_ENABLE_CONSOLE_CAPTURE=true.",
            McpBrowserBehavior.BrowserLog =>
                $"Tool \"{toolName}\" requires browser log capture. Enable Browser log capture in settings.",
            McpBrowserBehavior.FrameTracker =>
                $"Tool \"{toolName}\" requires Frame tracker. Enable Frame tracker in settings.",
            McpBrowserBehavior.DomTracker =>
                $"Tool \"{toolName}\" requires DOM tracker. Enable DOM tracker in settings.",
            _ => $"Tool \"{toolName}\" requires browser behavior {behavior}.",
        };
    }
}
