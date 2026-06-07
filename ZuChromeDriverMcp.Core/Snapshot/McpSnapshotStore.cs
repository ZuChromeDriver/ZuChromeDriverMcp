namespace ZuChromeDriverMcp.Core.Snapshot;

public sealed class McpSnapshotStore
{
    McpSnapshotNode _root;
    readonly Dictionary<string, McpSnapshotNode> _uidToNode = new(StringComparer.Ordinal);

    public bool HasSnapshot => _root != null;

    public void SetSnapshot(McpSnapshotNode root, IReadOnlyDictionary<string, McpSnapshotNode> uidMap)
    {
        _root = root;
        _uidToNode.Clear();
        foreach (var pair in uidMap)
            _uidToNode[pair.Key] = pair.Value;
    }

    public void Clear()
    {
        _root = null;
        _uidToNode.Clear();
    }

    public McpSnapshotNode GetRoot()
    {
        if (_root == null)
            throw new InvalidOperationException("No snapshot is available. Call take_snapshot first.");

        return _root;
    }

    public McpSnapshotNode GetNode(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("uid is required.", nameof(uid));

        if (!_uidToNode.TryGetValue(uid, out var node))
            throw new InvalidOperationException($"Element uid \"{uid}\" not found. Take a fresh take_snapshot.");

        return node;
    }
}
