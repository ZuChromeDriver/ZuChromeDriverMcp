using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class MemoryTools
{
    readonly McpBrowserContext _context;
    readonly McpHeapSnapshotService _heap;
    readonly SingleFlightLock _singleFlightLock;
    readonly McpToolGate _gate;

    public MemoryTools(
        McpBrowserContext context,
        McpHeapSnapshotService heap,
        SingleFlightLock singleFlightLock,
        McpToolGate gate)
    {
        _context = context;
        _heap = heap;
        _singleFlightLock = singleFlightLock;
        _gate = gate;
    }

    [McpServerTool(Name = "take_heapsnapshot", ReadOnly = false)]
    [Description("Capture a heap snapshot of the current page. Returns the path to a .heapsnapshot file.")]
    public async Task<CallToolResult> TakeHeapSnapshot(
        [Description("Path to save the .heapsnapshot file (absolute or relative). When omitted, saves to the artifacts directory.")] string filePath = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("take_heapsnapshot", response))
                return response.ToCallToolResult();

            try
            {
                var path = await _heap.CaptureAsync(_context.Driver, filePath, cancellationToken)
                    .ConfigureAwait(false);
                response.AppendLine($"Heap snapshot saved to {path}");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }
}
