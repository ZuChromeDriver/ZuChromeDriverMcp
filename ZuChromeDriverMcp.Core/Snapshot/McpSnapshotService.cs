using Zu.ChromeDevTools.Accessibility;
using ZuChromeDriverMcp.Core.Browser;

namespace ZuChromeDriverMcp.Core.Snapshot;

public sealed class McpSnapshotService
{
    static int _nextSnapshotId = 1;

    readonly McpBrowserContext _context;
    readonly McpSnapshotStore _store;

    public McpSnapshotService(McpBrowserContext context, McpSnapshotStore store)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<string> CaptureAsync(bool verbose, string filePath, CancellationToken cancellationToken)
    {
        var devTools = _context.Driver.DevTools;
        try
        {
            await devTools.Accessibility.Enable(null, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // duplicate enable
        }

        var tree = await devTools.Accessibility.GetFullAXTree(new GetFullAXTreeCommand(), cancellationToken)
            .ConfigureAwait(false);
        var nodes = tree?.Nodes ?? Array.Empty<AXNode>();
        var (root, uidMap) = BuildTree(nodes, verbose);
        _store.SetSnapshot(root, uidMap);

        var text = McpSnapshotFormatter.Format(root, verbose);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(fullPath, text, cancellationToken).ConfigureAwait(false);
            return $"Snapshot saved to {fullPath}";
        }

        return text;
    }

    static (McpSnapshotNode Root, Dictionary<string, McpSnapshotNode> UidMap) BuildTree(AXNode[] flatNodes, bool verbose)
    {
        var snapshotId = _nextSnapshotId++;
        var idCounter = 0;
        var uidMap = new Dictionary<string, McpSnapshotNode>(StringComparer.Ordinal);
        var nodeIdToMcp = new Dictionary<string, McpSnapshotNode>(StringComparer.Ordinal);
        var backendKeyToUid = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var ax in flatNodes)
        {
            if (ax == null || string.IsNullOrEmpty(ax.NodeId))
                continue;

            var role = FormatAxValue(ax.Role);
            var ignored = ax.Ignored || role == "none";
            if (!verbose && ignored)
                continue;

            string uid;
            if (ax.BackendDOMNodeId.HasValue)
            {
                var key = $"{ax.FrameId ?? ""}_{ax.BackendDOMNodeId.Value}";
                if (!backendKeyToUid.TryGetValue(key, out uid))
                {
                    uid = $"{snapshotId}_{idCounter++}";
                    backendKeyToUid[key] = uid;
                }
            }
            else
            {
                uid = $"{snapshotId}_{idCounter++}";
            }

            var mcpNode = new McpSnapshotNode
            {
                Uid = uid,
                BackendDomNodeId = ax.BackendDOMNodeId,
                FrameId = ax.FrameId ?? "",
                Ignored = ignored,
                Role = role,
                Name = FormatAxValue(ax.Name),
                Value = FormatAxValue(ax.Value),
            };

            if (string.Equals(role, "option", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(mcpNode.Name))
                mcpNode.Value = mcpNode.Name;

            nodeIdToMcp[ax.NodeId] = mcpNode;
            uidMap[uid] = mcpNode;
        }

        McpSnapshotNode root = null;
        foreach (var ax in flatNodes)
        {
            if (ax == null || string.IsNullOrEmpty(ax.NodeId) || !nodeIdToMcp.TryGetValue(ax.NodeId, out var mcpNode))
                continue;

            if (string.IsNullOrEmpty(ax.ParentId))
            {
                if (root == null)
                    root = mcpNode;
                continue;
            }

            if (nodeIdToMcp.TryGetValue(ax.ParentId, out var parent))
                parent.Children.Add(mcpNode);
        }

        if (root == null && nodeIdToMcp.Count > 0)
            root = nodeIdToMcp.Values.First();

        return (root ?? new McpSnapshotNode { Uid = $"{snapshotId}_0", Role = "Root" }, uidMap);
    }

    static string FormatAxValue(AXValue value)
    {
        if (value?.Value == null)
            return "";

        return value.Value switch
        {
            string s => s,
            _ => value.Value.ToString() ?? "",
        };
    }
}
