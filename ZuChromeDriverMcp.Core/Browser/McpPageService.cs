using Zu.ChromeDevTools.Target;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpPageService
{
    readonly McpBrowserContext _context;

    public McpPageService(McpBrowserContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<McpPageInfo>> ListPagesAsync(CancellationToken cancellationToken)
    {
        var driver = _context.Driver;
        var targets = await McpChromeJsonTarget.FetchPageTargetsAsync(driver.Port, cancellationToken).ConfigureAwait(false);
        var currentId = driver.DevTools?.ConnectedTargetId ?? "";
        var pages = new List<McpPageInfo>();
        var pageId = 1;
        foreach (var target in targets)
        {
            pages.Add(new McpPageInfo
            {
                PageId = pageId++,
                TargetId = target.Id,
                Url = target.Url ?? "",
                Title = target.Title ?? "",
                IsSelected = string.Equals(target.Id, currentId, StringComparison.Ordinal),
            });
        }

        return pages;
    }

    public void FormatPagesList(McpResponse response, IReadOnlyList<McpPageInfo> pages)
    {
        if (pages.Count == 0)
        {
            response.AppendLine("No open pages.");
            return;
        }

        response.AppendLine("## Pages");
        foreach (var page in pages)
        {
            var selected = page.IsSelected ? " [selected]" : "";
            response.AppendLine($"{page.PageId}: {page.Url}{selected}");
        }
    }

    public async Task SelectPageAsync(int pageId, bool bringToFront, CancellationToken cancellationToken)
    {
        var pages = await ListPagesAsync(cancellationToken).ConfigureAwait(false);
        var page = pages.FirstOrDefault(p => p.PageId == pageId);
        if (page == null)
            throw new InvalidOperationException($"Page id {pageId} was not found. Call list_pages to see open pages.");

        await _context.Driver.SwitchDevToolsToTarget(page.TargetId, cancellationToken).ConfigureAwait(false);
        _context.SnapshotStore.Clear();
        _context.Collector.ResetForNavigation();
        await _context.EnsureCollectorForCurrentSessionAsync(cancellationToken).ConfigureAwait(false);

        if (bringToFront)
        {
            try
            {
                await _context.Driver.DevTools.Target.ActivateTarget(
                    new ActivateTargetCommand { TargetId = page.TargetId },
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort; attach still switched CDP session.
            }
        }
    }

    public async Task<string> CreatePageAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            url = "about:blank";

        var response = await _context.Driver.DevTools.Target.CreateTarget(
            new CreateTargetCommand { Url = url },
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(response?.TargetId))
            throw new InvalidOperationException("Target.createTarget did not return a target id.");

        await _context.Driver.SwitchDevToolsToTarget(response.TargetId, cancellationToken).ConfigureAwait(false);
        _context.SnapshotStore.Clear();
        _context.Collector.ResetForNavigation();
        await _context.EnsureCollectorForCurrentSessionAsync(cancellationToken).ConfigureAwait(false);
        return response.TargetId;
    }

    public async Task ClosePageAsync(int pageId, CancellationToken cancellationToken)
    {
        var pages = await ListPagesAsync(cancellationToken).ConfigureAwait(false);
        if (pages.Count <= 1)
            throw new InvalidOperationException("The last open page cannot be closed.");

        var page = pages.FirstOrDefault(p => p.PageId == pageId);
        if (page == null)
            throw new InvalidOperationException($"Page id {pageId} was not found. Call list_pages to see open pages.");

        var driver = _context.Driver;
        var currentId = driver.DevTools?.ConnectedTargetId ?? "";
        if (string.Equals(page.TargetId, currentId, StringComparison.Ordinal))
        {
            var fallback = pages.First(p => p.PageId != pageId);
            await driver.SwitchDevToolsToTarget(fallback.TargetId, cancellationToken).ConfigureAwait(false);
            _context.SnapshotStore.Clear();
            _context.Collector.ResetForNavigation();
            await _context.EnsureCollectorForCurrentSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        await driver.DevTools.Target.CloseTarget(
            new CloseTargetCommand { TargetId = page.TargetId },
            cancellationToken).ConfigureAwait(false);

        if (!string.Equals(page.TargetId, currentId, StringComparison.Ordinal))
        {
            _context.SnapshotStore.Clear();
            _context.Collector.ResetForNavigation();
        }
    }
}
