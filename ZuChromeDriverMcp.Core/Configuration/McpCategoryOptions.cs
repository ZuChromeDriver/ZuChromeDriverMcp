namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class McpCategoryOptions
{
    public bool Input { get; set; } = true;

    public bool Navigation { get; set; } = true;

    public bool Emulation { get; set; } = true;

    public bool Network { get; set; } = true;

    public bool Debugging { get; set; } = true;

    public bool Memory { get; set; } = true;

    public bool IsEnabled(McpToolCategory category)
    {
        return category switch
        {
            McpToolCategory.Input => Input,
            McpToolCategory.Navigation => Navigation,
            McpToolCategory.Emulation => Emulation,
            McpToolCategory.Network => Network,
            McpToolCategory.Debugging => Debugging,
            McpToolCategory.Memory => Memory,
            _ => true,
        };
    }

    public string GetFlagName(McpToolCategory category)
    {
        return category switch
        {
            McpToolCategory.Input => "category-input",
            McpToolCategory.Navigation => "category-navigation",
            McpToolCategory.Emulation => "category-emulation",
            McpToolCategory.Network => "category-network",
            McpToolCategory.Debugging => "category-debugging",
            McpToolCategory.Memory => "category-memory",
            _ => "category-unknown",
        };
    }
}
