namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpPageInfo
{
    public int PageId { get; set; }

    public string TargetId { get; set; } = "";

    public string Url { get; set; } = "";

    public string Title { get; set; } = "";

    public bool IsSelected { get; set; }
}
