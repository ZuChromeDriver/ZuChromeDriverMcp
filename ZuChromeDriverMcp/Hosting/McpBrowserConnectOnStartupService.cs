using Microsoft.Extensions.Hosting;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Hosting;

public sealed class McpBrowserConnectOnStartupService : IHostedService
{
    readonly McpBrowserContext _browserContext;
    readonly McpHostOptions _options;

    public McpBrowserConnectOnStartupService(McpBrowserContext browserContext, McpHostOptions options)
    {
        _browserContext = browserContext;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ConnectChromeOnStartup)
            return;

        await _browserContext.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
