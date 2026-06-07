using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Host;

public sealed class McpBrowserLifetimeService : IHostedService
{
    readonly McpBrowserContext _browserContext;
    readonly McpHostOptions _options;
    readonly ILogger<McpBrowserLifetimeService> _logger;

    public McpBrowserLifetimeService(
        McpBrowserContext browserContext,
        McpHostOptions options,
        ILogger<McpBrowserLifetimeService> logger)
    {
        _browserContext = browserContext;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ConnectChromeOnStartup)
        {
            _logger.LogInformation("Chrome auto-connect disabled; waiting for manual connect or MCP tools.");
            return;
        }

        _logger.LogInformation("Connecting to Chrome...");
        await _browserContext.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Connected to Chrome on port {Port}.", _browserContext.Driver.Port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down Chrome session...");
        await _browserContext.ShutdownAsync(cancellationToken).ConfigureAwait(false);
    }
}
