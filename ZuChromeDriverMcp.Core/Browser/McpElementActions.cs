using Zu.Chrome.DriverCore;
using Zu.ChromeDevTools.DOM;
using Zu.ChromeDevTools.Runtime;
using Zu.ChromeWebDriver;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpElementActions
{
    readonly McpBrowserContext _context;

    public McpElementActions(McpBrowserContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task ClickAsync(string uid, string selector, bool dblClick, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(uid))
        {
            await ClickByUidAsync(uid, dblClick, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(selector))
        {
            var elementId = await FindElementByCssAsync(selector, cancellationToken).ConfigureAwait(false);
            await _context.Driver.ElementCommands.ClickElement(elementId).ConfigureAwait(false);
            return;
        }

        throw new ArgumentException("Either 'uid' or 'selector' is required.");
    }

    public async Task FillAsync(string uid, string selector, string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Parameter 'value' is required.", nameof(value));

        if (!string.IsNullOrWhiteSpace(uid))
        {
            await FillByUidAsync(uid, value, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(selector))
        {
            var elementId = await FindElementByCssAsync(selector, cancellationToken).ConfigureAwait(false);
            await _context.Driver.ElementCommands.ClearElement(elementId, cancellationToken).ConfigureAwait(false);
            await _context.Driver.ElementCommands.SendKeysToElement(elementId, value, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new ArgumentException("Either 'uid' or 'selector' is required.");
    }

    async Task ClickByUidAsync(string uid, bool dblClick, CancellationToken cancellationToken)
    {
        var node = _context.SnapshotStore.GetNode(uid);
        if (!node.BackendDomNodeId.HasValue)
            throw new InvalidOperationException($"Element uid \"{uid}\" has no DOM backend id; take a new snapshot.");

        var objectId = await ResolveBackendNodeObjectIdAsync(node.BackendDomNodeId.Value, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var clicks = dblClick ? 2 : 1;
            for (var i = 0; i < clicks; i++)
            {
                var resp = await _context.Driver.DevTools.Runtime.CallFunctionOn(
                    new CallFunctionOnCommand
                    {
                        FunctionDeclaration = "function() { this.click(); }",
                        ObjectId = objectId,
                        ReturnByValue = true,
                    },
                    cancellationToken).ConfigureAwait(false);
                ThrowIfEvalFailed(resp);
            }
        }
        finally
        {
            await ReleaseObjectAsync(objectId, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task FillByUidAsync(string uid, string value, CancellationToken cancellationToken)
    {
        var node = _context.SnapshotStore.GetNode(uid);
        if (!node.BackendDomNodeId.HasValue)
            throw new InvalidOperationException($"Element uid \"{uid}\" has no DOM backend id; take a new snapshot.");

        var objectId = await ResolveBackendNodeObjectIdAsync(node.BackendDomNodeId.Value, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var resp = await _context.Driver.DevTools.Runtime.CallFunctionOn(
                new CallFunctionOnCommand
                {
                    FunctionDeclaration =
                        "function(v) {" +
                        " if (this.type === 'checkbox' || this.type === 'radio') { this.checked = (v === 'true'); return; }" +
                        " if (this.isContentEditable) { this.textContent = v; return; }" +
                        " this.value = v;" +
                        "}",
                    ObjectId = objectId,
                    Arguments =
                    [
                        new CallArgument { Value = value },
                    ],
                    ReturnByValue = true,
                },
                cancellationToken).ConfigureAwait(false);
            ThrowIfEvalFailed(resp);
        }
        finally
        {
            await ReleaseObjectAsync(objectId, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task<string> FindElementByCssAsync(string selector, CancellationToken cancellationToken)
    {
        var result = await _context.Driver.WindowCommands
            .FindElement("css selector", selector, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var elementId = ChromeDriverElements.GetElementFromResponse(result);
        if (string.IsNullOrEmpty(elementId))
            throw new InvalidOperationException($"No element matched selector: {selector}");

        return elementId;
    }

    async Task<string> ResolveBackendNodeObjectIdAsync(long backendNodeId, CancellationToken cancellationToken)
    {
        var devTools = _context.Driver.DevTools;
        try
        {
            await devTools.DOM.Enable(null, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        var resolved = await devTools.DOM.ResolveNode(
            new ResolveNodeCommand { BackendNodeId = backendNodeId },
            cancellationToken).ConfigureAwait(false);

        var objectId = resolved?.Object?.ObjectId;
        if (string.IsNullOrEmpty(objectId))
            throw new InvalidOperationException("DOM.resolveNode did not return an object id for the snapshot element.");

        return objectId;
    }

    static void ThrowIfEvalFailed(CallFunctionOnCommandResponse resp)
    {
        if (resp?.ExceptionDetails != null)
            throw new DriverCoreException(
                resp.ExceptionDetails.Text ?? resp.ExceptionDetails.ToString() ?? "Script failed.",
                "javascript error");
    }

    async Task ReleaseObjectAsync(string objectId, CancellationToken cancellationToken)
    {
        try
        {
            await _context.Driver.DevTools.Runtime.ReleaseObject(
                new ReleaseObjectCommand { ObjectId = objectId },
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // object may already be gone
        }
    }
}
