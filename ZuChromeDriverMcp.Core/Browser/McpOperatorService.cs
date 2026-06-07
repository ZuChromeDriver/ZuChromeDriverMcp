using ZuChromeDriverMcp.Core.Concurrency;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpOperatorService
{
    readonly McpBrowserContext _context;
    readonly McpPageService _pageService;
    readonly McpRuntimeMonitor _runtimeMonitor;
    readonly SingleFlightLock _singleFlightLock;

    public McpOperatorService(
        McpBrowserContext context,
        McpPageService pageService,
        McpRuntimeMonitor runtimeMonitor,
        SingleFlightLock singleFlightLock)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _runtimeMonitor = runtimeMonitor ?? throw new ArgumentNullException(nameof(runtimeMonitor));
        _singleFlightLock = singleFlightLock ?? throw new ArgumentNullException(nameof(singleFlightLock));
    }

    public bool IsConnected => _context.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _context.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await _runtimeMonitor.RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _context.ShutdownAsync(cancellationToken).ConfigureAwait(false);
        await _runtimeMonitor.RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            _context.SnapshotStore.Clear();
            _context.Collector.ResetForNavigation();
            await _context.Driver.WindowCommands.GoToUrl(url, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _runtimeMonitor.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> TakeScreenshotAsync(CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var screenshot = await _context.Driver.Screenshot.GetScreenshot(cancellationToken).ConfigureAwait(false);
            if (screenshot?.AsByteArray == null || screenshot.AsByteArray.Length == 0)
                throw new InvalidOperationException("Screenshot capture returned no image data.");

            var filePath = await _context.SaveTemporaryFileAsync(screenshot.AsByteArray, "screenshot.png", cancellationToken)
                .ConfigureAwait(false);
            _runtimeMonitor.SetLastArtifactPath(filePath);
            await _runtimeMonitor.RefreshAsync(cancellationToken).ConfigureAwait(false);
            return filePath;
        }
    }

    public async Task<IReadOnlyList<McpPageInfo>> ListPagesAsync(CancellationToken cancellationToken = default)
    {
        return await _pageService.ListPagesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SelectPageAsync(int pageId, CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            await _pageService.SelectPageAsync(pageId, bringToFront: true, cancellationToken).ConfigureAwait(false);
            await _runtimeMonitor.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void ReportError(string message)
    {
        _runtimeMonitor.SetLastError(message);
    }
}
