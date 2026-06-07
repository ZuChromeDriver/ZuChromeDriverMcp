using Microsoft.Extensions.DependencyInjection;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Snapshot;
using ZuChromeDriverMcp.Core.Tools;

namespace ZuChromeDriverMcp.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZuChromeDriverMcpCore(this IServiceCollection services, McpHostOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(options.Categories);
        services.AddSingleton<ChromeProfileService>();
        services.AddSingleton<McpSnapshotStore>();
        services.AddSingleton<McpDevToolsCollector>();
        services.AddSingleton<McpBrowserContext>();
        services.AddSingleton<McpPageService>();
        services.AddSingleton<McpSnapshotService>();
        services.AddSingleton<McpElementActions>();
        services.AddSingleton<McpHeapSnapshotService>();
        services.AddSingleton<McpRuntimeMonitor>();
        services.AddSingleton<McpOperatorService>();
        services.AddHostedService(sp => sp.GetRequiredService<McpRuntimeMonitor>());
        services.AddSingleton<SingleFlightLock>();
        services.AddSingleton<McpToolAvailability>();
        services.AddSingleton<McpToolGate>();
        services.AddSingleton<ChromeTools>();
        services.AddSingleton<BrowserTools>();
        services.AddSingleton<PageTools>();
        services.AddSingleton<SnapshotTools>();
        services.AddSingleton<InputTools>();
        services.AddSingleton<CollectorTools>();
        services.AddSingleton<MemoryTools>();
        return services;
    }
}
