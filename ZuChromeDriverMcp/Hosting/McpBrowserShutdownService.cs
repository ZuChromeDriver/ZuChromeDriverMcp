using Microsoft.Extensions.Hosting;
using ZuChromeDriverMcp.Core.Browser;

namespace ZuChromeDriverMcp.Hosting;

public sealed class McpBrowserShutdownService : IHostedService
{
    readonly McpBrowserContext _browserContext;

    public McpBrowserShutdownService(McpBrowserContext browserContext)
    {
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _browserContext.ShutdownAsync(cancellationToken).ConfigureAwait(false);
    }
}
