using Zu.Chrome;
using Zu.ChromeDevTools.Network;
using Zu.ChromeDevTools.Runtime;
using ZuChromeDriverMcp.Core.Configuration;
using NetworkEnableCommand = Zu.ChromeDevTools.Network.EnableCommand;
using RuntimeEnableCommand = Zu.ChromeDevTools.Runtime.EnableCommand;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpDevToolsCollector
{
    readonly McpHostOptions _options;
    readonly object _lock = new();
    readonly List<McpNetworkEntry> _network = new();
    readonly List<McpConsoleEntry> _console = new();
    bool _networkSubscribed;
    bool _consoleSubscribed;

    public McpDevToolsCollector(McpHostOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public event EventHandler Changed;

    public int NetworkCount
    {
        get
        {
            lock (_lock)
                return _network.Count;
        }
    }

    public int ConsoleCount
    {
        get
        {
            lock (_lock)
                return _console.Count;
        }
    }

    public void ResetSubscription()
    {
        _networkSubscribed = false;
        _consoleSubscribed = false;
    }

    public async Task EnsureSubscribedAsync(ZuChromeDriver driver, CancellationToken cancellationToken = default)
    {
        if (driver?.DevTools?.Session == null)
            return;

        var devTools = driver.DevTools;

        if (_options.EnableNetworkCaptureOnConnect && !_networkSubscribed)
        {
            try
            {
                await devTools.Network.Enable(new NetworkEnableCommand(), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore duplicate enable
            }

            devTools.Network.SubscribeToRequestWillBeSentEvent(OnRequestWillBeSent);
            _networkSubscribed = true;
        }

        if (_options.EnableConsoleCaptureOnConnect && !_consoleSubscribed)
        {
            try
            {
                await devTools.Runtime.Enable(new RuntimeEnableCommand(), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore duplicate enable
            }

            devTools.Runtime.SubscribeToConsoleAPICalledEvent(OnConsoleApiCalled);
            _consoleSubscribed = true;
        }
    }

    public void ResetForNavigation()
    {
        lock (_lock)
        {
            _network.Clear();
            _console.Clear();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    void OnRequestWillBeSent(RequestWillBeSentEvent evt)
    {
        if (evt?.Request == null)
            return;

        lock (_lock)
        {
            _network.Add(new McpNetworkEntry
            {
                Id = _network.Count + 1,
                Url = evt.Request.Url ?? "",
                Method = evt.Request.Method ?? "",
                ResourceType = evt.Type.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
            });
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    void OnConsoleApiCalled(ConsoleAPICalledEvent evt)
    {
        if (evt == null)
            return;

        lock (_lock)
        {
            _console.Add(new McpConsoleEntry
            {
                Id = _console.Count + 1,
                Type = evt.Type.ToString(),
                Text = FormatConsoleArgs(evt.Args),
                Timestamp = DateTimeOffset.UtcNow,
            });
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    static string FormatConsoleArgs(RemoteObject[] args)
    {
        if (args == null || args.Length == 0)
            return "";

        var parts = new List<string>();
        foreach (var arg in args)
        {
            if (!string.IsNullOrEmpty(arg?.Description))
                parts.Add(arg.Description);
            else if (arg?.Value != null)
                parts.Add(arg.Value.ToString());
        }

        return string.Join(" ", parts);
    }

    public IReadOnlyList<McpNetworkEntry> GetNetworkEntries()
    {
        lock (_lock)
            return _network.ToList();
    }

    public IReadOnlyList<McpConsoleEntry> GetConsoleEntries()
    {
        lock (_lock)
            return _console.ToList();
    }
}

public sealed class McpNetworkEntry
{
    public int Id { get; set; }

    public string Url { get; set; } = "";

    public string Method { get; set; } = "";

    public string ResourceType { get; set; } = "";

    public DateTimeOffset Timestamp { get; set; }
}

public sealed class McpConsoleEntry
{
    public int Id { get; set; }

    public string Type { get; set; } = "";

    public string Text { get; set; } = "";

    public DateTimeOffset Timestamp { get; set; }
}
