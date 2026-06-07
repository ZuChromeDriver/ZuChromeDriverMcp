using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class PageTools
{
    readonly McpBrowserContext _context;
    readonly McpPageService _pages;
    readonly SingleFlightLock _singleFlightLock;
    readonly McpToolGate _gate;

    public PageTools(
        McpBrowserContext context,
        McpPageService pages,
        SingleFlightLock singleFlightLock,
        McpToolGate gate)
    {
        _context = context;
        _pages = pages;
        _singleFlightLock = singleFlightLock;
        _gate = gate;
    }

    [McpServerTool(Name = "list_pages", ReadOnly = true)]
    [Description("Get a list of pages open in the browser.")]
    public async Task<CallToolResult> ListPages(CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("list_pages", response))
                return response.ToCallToolResult();

            try
            {
                var pages = await _pages.ListPagesAsync(cancellationToken).ConfigureAwait(false);
                _pages.FormatPagesList(response, pages);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "select_page", ReadOnly = true)]
    [Description("Select a page as a context for future tool calls.")]
    public async Task<CallToolResult> SelectPage(
        [Description("The ID of the page to select. Call list_pages to get available pages.")] int pageId,
        [Description("Whether to focus the page and bring it to the top.")] bool? bringToFront = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("select_page", response))
                return response.ToCallToolResult();

            try
            {
                await _pages.SelectPageAsync(pageId, bringToFront ?? false, cancellationToken).ConfigureAwait(false);
                var pages = await _pages.ListPagesAsync(cancellationToken).ConfigureAwait(false);
                _pages.FormatPagesList(response, pages);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "new_page", ReadOnly = false)]
    [Description("Open a new tab and load a URL.")]
    public async Task<CallToolResult> NewPage(
        [Description("URL to load in a new page.")] string url,
        CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("new_page", response))
                return response.ToCallToolResult();

            if (string.IsNullOrWhiteSpace(url))
            {
                response.SetError("Parameter 'url' is required.");
                return response.ToCallToolResult();
            }

            try
            {
                await _pages.CreatePageAsync(url, cancellationToken).ConfigureAwait(false);
                var pages = await _pages.ListPagesAsync(cancellationToken).ConfigureAwait(false);
                _pages.FormatPagesList(response, pages);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "close_page", ReadOnly = false)]
    [Description("Closes a page by its id. The last open page cannot be closed.")]
    public async Task<CallToolResult> ClosePage(
        [Description("The ID of the page to close. Call list_pages to list pages.")] int pageId,
        CancellationToken cancellationToken)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();
            if (!_gate.TryBegin("close_page", response))
                return response.ToCallToolResult();

            try
            {
                await _pages.ClosePageAsync(pageId, cancellationToken).ConfigureAwait(false);
                var pages = await _pages.ListPagesAsync(cancellationToken).ConfigureAwait(false);
                _pages.FormatPagesList(response, pages);
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }
}
