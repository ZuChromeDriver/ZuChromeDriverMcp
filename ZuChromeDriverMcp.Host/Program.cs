using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZuChromeDriverMcp.Core;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Host;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

var options = McpHostOptions.FromConfiguration(builder.Configuration, args, defaultConnectOnStartup: true);
builder.Services.AddZuChromeDriverMcpCore(options);
builder.Services.AddHostedService<McpBrowserLifetimeService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .AddZuChromeDriverMcpTools();

await builder.Build().RunAsync();
