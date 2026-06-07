using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Core.Tools;

public sealed class McpToolDefinition
{
    public string Name { get; init; }

    public McpToolGroup Group { get; init; }

    public McpBrowserBehavior[] RequiredBehaviors { get; init; } = [];
}

public static class McpToolCatalog
{
    static readonly McpToolDefinition[] All =
    [
        new() { Name = "list_chrome_profiles", Group = McpToolGroup.Meta },
        new() { Name = "connect_chrome", Group = McpToolGroup.Meta },
        new() { Name = "disconnect_chrome", Group = McpToolGroup.Meta },
        new() { Name = "list_network_requests", Group = McpToolGroup.NetworkRequests, RequiredBehaviors = [McpBrowserBehavior.NetworkCdp] },
        new() { Name = "list_console_messages", Group = McpToolGroup.ConsoleMessages, RequiredBehaviors = [McpBrowserBehavior.ConsoleCdp] },
        new() { Name = "evaluate", Group = McpToolGroup.Evaluate },
        new() { Name = "screenshot", Group = McpToolGroup.Screenshot },
        new() { Name = "take_snapshot", Group = McpToolGroup.Snapshot },
        new() { Name = "click", Group = McpToolGroup.Input },
        new() { Name = "fill", Group = McpToolGroup.Input },
        new() { Name = "navigate", Group = McpToolGroup.Navigation },
        new() { Name = "list_pages", Group = McpToolGroup.Navigation },
        new() { Name = "select_page", Group = McpToolGroup.Navigation },
        new() { Name = "new_page", Group = McpToolGroup.Navigation },
        new() { Name = "close_page", Group = McpToolGroup.Navigation },
        new() { Name = "take_heapsnapshot", Group = McpToolGroup.Memory },
    ];

    static readonly Dictionary<string, McpToolDefinition> ByName =
        All.ToDictionary(t => t.Name, StringComparer.Ordinal);

    public static IReadOnlyList<McpToolDefinition> Tools => All;

    public static bool TryGet(string toolName, out McpToolDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            definition = null;
            return false;
        }

        return ByName.TryGetValue(toolName, out definition);
    }
}
