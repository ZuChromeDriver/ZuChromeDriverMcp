using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Tools;

namespace ZuChromeDriverMcp.Core;

public static class McpServerServiceCollectionExtensions
{
    public static IMcpServerBuilder AddZuChromeDriverMcpTools(this IMcpServerBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder
            .WithTools<ChromeTools>()
            .WithTools<BrowserTools>()
            .WithTools<PageTools>()
            .WithTools<SnapshotTools>()
            .WithTools<InputTools>()
            .WithTools<CollectorTools>()
            .WithTools<MemoryTools>()
            .WithRequestFilters(filters =>
            {
                filters.AddListToolsFilter(next => async (context, cancellationToken) =>
                {
                    var result = await next(context, cancellationToken).ConfigureAwait(false);
                    var availability = context.Services?.GetService(typeof(McpToolAvailability)) as McpToolAvailability;
                    if (availability == null || result.Tools == null)
                        return result;

                    var filtered = new List<Tool>();
                    foreach (var tool in result.Tools)
                    {
                        if (tool.Name != null && availability.IsAvailable(tool.Name))
                            filtered.Add(tool);
                    }

                    result.Tools = filtered;
                    return result;
                });

                filters.AddCallToolFilter(next => async (context, cancellationToken) =>
                {
                    var toolName = context.Params?.Name;
                    var availability = context.Services?.GetService(typeof(McpToolAvailability)) as McpToolAvailability;
                    if (availability != null && !string.IsNullOrWhiteSpace(toolName) && !availability.IsAvailable(toolName))
                    {
                        return new CallToolResult
                        {
                            IsError = true,
                            Content = [new TextContentBlock { Text = availability.GetUnavailableReason(toolName) }],
                        };
                    }

                    return await next(context, cancellationToken).ConfigureAwait(false);
                });
            });
    }
}
