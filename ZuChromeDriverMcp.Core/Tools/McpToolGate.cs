using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

public sealed class McpToolGate
{
    readonly McpToolAvailability _availability;

    public McpToolGate(McpToolAvailability availability)
    {
        _availability = availability ?? throw new ArgumentNullException(nameof(availability));
    }

    public bool TryBegin(string toolName, McpResponse response)
    {
        if (_availability.IsAvailable(toolName))
            return true;

        response.SetError(_availability.GetUnavailableReason(toolName));
        return false;
    }
}
