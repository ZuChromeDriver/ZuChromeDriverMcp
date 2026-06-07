using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZuChromeDriverMcp.Core;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Hosting;
using ZuChromeDriverMcp.ViewModels;

namespace ZuChromeDriverMcp;

public static class WpfAppBootstrap
{
    public static int Run(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var savedSettings = McpHostSettingsStore.TryLoad();
        var options = McpHostOptions.FromConfiguration(
            configuration,
            args,
            defaultConnectOnStartup: false,
            savedSettings: savedSettings);

        if (options.McpTransport == McpTransportKind.Stdio)
            return RunStdioHostAsync(args).GetAwaiter().GetResult();

        return RunGuiHost(args, options);
    }

    static async Task<int> RunStdioHostAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        var savedSettings = McpHostSettingsStore.TryLoad();
        var options = McpHostOptions.FromConfiguration(
            builder.Configuration,
            args,
            defaultConnectOnStartup: false,
            savedSettings: savedSettings);
        if (!options.ConnectChromeOnStartup)
            options.ConnectChromeOnStartup = true;

        builder.Services.AddZuChromeDriverMcpCore(options);
        builder.Services.AddHostedService<McpBrowserConnectOnStartupService>();
        builder.Services.AddHostedService<McpBrowserShutdownService>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .AddZuChromeDriverMcpTools();

        await builder.Build().RunAsync().ConfigureAwait(false);
        return 0;
    }

    static int RunGuiHost(string[] args, McpHostOptions options)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddConsole();
        builder.WebHost.UseUrls($"http://127.0.0.1:{options.McpHttpPort}");

        builder.Services.AddZuChromeDriverMcpCore(options);
        builder.Services.AddHostedService<McpBrowserShutdownService>();

        if (options.ConnectChromeOnStartup)
            builder.Services.AddHostedService<McpBrowserConnectOnStartupService>();

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .AddZuChromeDriverMcpTools();

        RegisterViewModels(builder.Services, options);

        var webApp = builder.Build();
        webApp.MapMcp(options.McpHttpPath);

        var hostRunTask = webApp.RunAsync();

        var wpfApp = new App();
        var mainWindow = webApp.Services.GetRequiredService<MainWindow>();
        try
        {
            wpfApp.Run(mainWindow);
        }
        finally
        {
            webApp.Services.GetRequiredService<McpBrowserContext>()
                .ShutdownAsync()
                .GetAwaiter()
                .GetResult();
        }

        webApp.Lifetime.StopApplication();
        hostRunTask.GetAwaiter().GetResult();
        return 0;
    }

    static void RegisterViewModels(IServiceCollection services, McpHostOptions options)
    {
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ProfilesViewModel>();
        services.AddTransient<ControlViewModel>();
        services.AddTransient<RuntimeViewModel>();
        services.AddTransient<McpInfoViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
