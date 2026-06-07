using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Responses;
using ZuChromeDriverMcp.Core.Snapshot;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class SnapshotTools
{
    readonly SingleFlightLock _singleFlightLock;
    readonly McpSnapshotService _snapshot;
    readonly McpToolGate _gate;

    public SnapshotTools(SingleFlightLock singleFlightLock, McpSnapshotService snapshot, McpToolGate gate)
    {
        _singleFlightLock = singleFlightLock;
        _snapshot = snapshot;
        _gate = gate;
    }

    [McpServerTool(Name = "take_snapshot", ReadOnly = false)]
    [Description("Take a text snapshot of the current page from the accessibility tree. Elements include stable uid values for click/fill.")]
    public async Task<CallToolResult> TakeSnapshot(
        [Description("Include all nodes from the a11y tree, not only interesting nodes.")] bool? verbose = null,
        [Description("Absolute or relative path to save the snapshot instead of returning it in the response.")] string filePath = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("take_snapshot", response))
                return response.ToCallToolResult();

            try
            {
                var text = await _snapshot.CaptureAsync(verbose ?? false, filePath, cancellationToken).ConfigureAwait(false);
                response.AppendLine(text);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }
}
