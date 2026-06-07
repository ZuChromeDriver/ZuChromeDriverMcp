using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class InputTools
{
    readonly McpElementActions _elements;
    readonly SingleFlightLock _singleFlightLock;
    readonly McpToolGate _gate;

    public InputTools(McpElementActions elements, SingleFlightLock singleFlightLock, McpToolGate gate)
    {
        _elements = elements;
        _singleFlightLock = singleFlightLock;
        _gate = gate;
    }

    [McpServerTool(Name = "click", ReadOnly = false)]
    [Description("Clicks an element identified by snapshot uid or CSS selector.")]
    public async Task<CallToolResult> Click(
        [Description("Element uid from take_snapshot.")] string uid = null,
        [Description("CSS selector when uid is not used.")] string selector = null,
        [Description("Double-click when true.")] bool? dblClick = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("click", response))
                return response.ToCallToolResult();

            try
            {
                await _elements.ClickAsync(uid ?? "", selector ?? "", dblClick ?? false, cancellationToken)
                    .ConfigureAwait(false);
                response.AppendLine(dblClick == true
                    ? "Successfully double clicked on the element."
                    : "Successfully clicked on the element.");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "fill", ReadOnly = false)]
    [Description("Fill a form element by snapshot uid or CSS selector.")]
    public async Task<CallToolResult> Fill(
        [Description("Element uid from take_snapshot.")] string uid = null,
        [Description("CSS selector when uid is not used.")] string selector = null,
        [Description("Value to enter. Use true/false for checkboxes.")] string value = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("fill", response))
                return response.ToCallToolResult();

            try
            {
                await _elements.FillAsync(uid ?? "", selector ?? "", value, cancellationToken).ConfigureAwait(false);
                response.AppendLine("Successfully filled out the element.");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }
}
