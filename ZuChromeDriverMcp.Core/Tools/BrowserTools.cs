using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class BrowserTools
{
    readonly McpBrowserContext _context;
    readonly SingleFlightLock _singleFlightLock;
    readonly McpToolGate _gate;

    public BrowserTools(McpBrowserContext context, SingleFlightLock singleFlightLock, McpToolGate gate)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _singleFlightLock = singleFlightLock ?? throw new ArgumentNullException(nameof(singleFlightLock));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    [McpServerTool(Name = "navigate", ReadOnly = false, Destructive = false)]
    [Description("Loads a URL in the current browser tab.")]
    public async Task<CallToolResult> Navigate(
        [Description("URL to navigate to")] string url,
        CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("navigate", response))
                return response.ToCallToolResult();

            if (string.IsNullOrWhiteSpace(url))
            {
                response.SetError("Parameter 'url' is required.");
                return response.ToCallToolResult();
            }

            try
            {
                _context.SnapshotStore.Clear();
                _context.Collector.ResetForNavigation();
                await _context.Driver.WindowCommands.GoToUrl(url, cancellationToken: cancellationToken).ConfigureAwait(false);
                var currentUrl = await _context.Driver.WindowCommands.GetCurrentUrl().ConfigureAwait(false);
                response.AppendLine($"Navigated to {currentUrl}.");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "evaluate", ReadOnly = false)]
    [Description("Evaluates a JavaScript expression on the current page.")]
    public async Task<CallToolResult> Evaluate(
        [Description("JavaScript to run on the page")] string script,
        CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("evaluate", response))
                return response.ToCallToolResult();

            if (string.IsNullOrWhiteSpace(script))
            {
                response.SetError("Parameter 'script' is required.");
                return response.ToCallToolResult();
            }

            try
            {
                var evaluation = await _context.Driver.WebView
                    .EvaluateScript(script, returnByValue: true, cancellationToken: cancellationToken, awaitPromise: true)
                    .ConfigureAwait(false);

                if (evaluation.ExceptionDetails != null)
                {
                    var detail = evaluation.ExceptionDetails.Text ?? evaluation.ExceptionDetails.ToString() ?? "Script evaluation failed.";
                    response.SetError(detail);
                    return response.ToCallToolResult();
                }

                response.AppendLine(FormatEvaluateResult(
                    evaluation.Result?.Value,
                    evaluation.Result?.UnserializableValue,
                    evaluation.Result?.Description));
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "screenshot", ReadOnly = false, Destructive = false)]
    [Description("Takes a screenshot of the current page and returns the path to a PNG file.")]
    public async Task<CallToolResult> Screenshot(CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("screenshot", response))
                return response.ToCallToolResult();

            try
            {
                var screenshot = await _context.Driver.Screenshot.GetScreenshot(cancellationToken).ConfigureAwait(false);
                if (screenshot?.AsByteArray == null || screenshot.AsByteArray.Length == 0)
                {
                    response.SetError("Screenshot capture returned no image data.");
                    return response.ToCallToolResult();
                }

                var filePath = await _context.SaveTemporaryFileAsync(screenshot.AsByteArray, "screenshot.png", cancellationToken)
                    .ConfigureAwait(false);
                response.AppendLine(filePath);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    static string FormatEvaluateResult(object value, string unserializableValue, string description)
    {
        if (value != null)
            return JsonSerializer.Serialize(value);

        if (!string.IsNullOrEmpty(unserializableValue))
            return unserializableValue;

        if (!string.IsNullOrEmpty(description))
            return description;

        return "undefined";
    }
}
