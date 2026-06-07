using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class CollectorTools
{
    readonly McpBrowserContext _context;
    readonly SingleFlightLock _singleFlightLock;
    readonly McpToolGate _gate;

    public CollectorTools(McpBrowserContext context, SingleFlightLock singleFlightLock, McpToolGate gate)
    {
        _context = context;
        _singleFlightLock = singleFlightLock;
        _gate = gate;
    }

    [McpServerTool(Name = "list_network_requests", ReadOnly = true)]
    [Description("List network requests captured for the current page since the last navigation.")]
    public async Task<CallToolResult> ListNetworkRequests(
        [Description("Maximum number of requests to return.")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("list_network_requests", response))
                return response.ToCallToolResult();

            try
            {
                var entries = _context.Collector.GetNetworkEntries();
                FormatNetworkList(response, entries, pageSize);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "list_console_messages", ReadOnly = true)]
    [Description("List console API messages captured for the current page since the last navigation.")]
    public async Task<CallToolResult> ListConsoleMessages(
        [Description("Maximum number of messages to return.")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("list_console_messages", response))
                return response.ToCallToolResult();

            try
            {
                var entries = _context.Collector.GetConsoleEntries();
                FormatConsoleList(response, entries, pageSize);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    static void FormatNetworkList(McpResponse response, IReadOnlyList<McpNetworkEntry> entries, int? pageSize)
    {
        if (entries.Count == 0)
        {
            response.AppendLine("No network requests captured yet.");
            return;
        }

        var slice = pageSize.HasValue && pageSize.Value > 0
            ? entries.Take(pageSize.Value)
            : entries;

        response.AppendLine("## Network requests");
        foreach (var entry in slice)
            response.AppendLine($"{entry.Id}: [{entry.Method}] {entry.ResourceType} {entry.Url}");
    }

    static void FormatConsoleList(McpResponse response, IReadOnlyList<McpConsoleEntry> entries, int? pageSize)
    {
        if (entries.Count == 0)
        {
            response.AppendLine("No console messages captured yet.");
            return;
        }

        var slice = pageSize.HasValue && pageSize.Value > 0
            ? entries.Take(pageSize.Value)
            : entries;

        response.AppendLine("## Console messages");
        foreach (var entry in slice)
            response.AppendLine($"{entry.Id}: [{entry.Type}] {entry.Text}");
    }
}
