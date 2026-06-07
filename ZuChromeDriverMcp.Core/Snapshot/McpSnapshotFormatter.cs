using System.Text;

namespace ZuChromeDriverMcp.Core.Snapshot;

public static class McpSnapshotFormatter
{
    public static string Format(McpSnapshotNode root, bool verbose)
    {
        var chunks = new StringBuilder();
        FormatNode(root, 0, verbose, chunks);
        return chunks.ToString();
    }

    static void FormatNode(McpSnapshotNode node, int depth, bool verbose, StringBuilder output)
    {
        if (!verbose && node.Ignored && node.Children.Count == 0)
            return;

        var indent = new string(' ', depth * 2);
        var attributes = new List<string> { $"uid={node.Uid}" };

        if (!string.IsNullOrEmpty(node.Role))
            attributes.Add(node.Role == "none" ? "ignored" : node.Role);

        if (!string.IsNullOrEmpty(node.Name))
            attributes.Add($"\"{node.Name}\"");

        if (!string.IsNullOrEmpty(node.Value))
            attributes.Add($"value=\"{node.Value}\"");

        output.Append(indent);
        output.AppendLine(string.Join(" ", attributes));

        foreach (var child in node.Children)
            FormatNode(child, depth + 1, verbose, output);
    }
}
