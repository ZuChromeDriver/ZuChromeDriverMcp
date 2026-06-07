namespace ZuChromeDriverMcp.Core.Snapshot;

public sealed class McpSnapshotNode
{
    public string Uid { get; set; } = "";

    public long? BackendDomNodeId { get; set; }

    public string FrameId { get; set; } = "";

    public bool Ignored { get; set; }

    public string Role { get; set; } = "";

    public string Name { get; set; } = "";

    public string Value { get; set; } = "";

    public List<McpSnapshotNode> Children { get; } = new();
}
