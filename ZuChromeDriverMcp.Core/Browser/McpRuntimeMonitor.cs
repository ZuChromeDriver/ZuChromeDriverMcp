using Microsoft.Extensions.Hosting;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpRuntimeMonitor : IHostedService, IDisposable
{
    const int RecentEntryLimit = 20;

    readonly McpBrowserContext _context;
    readonly McpPageService _pageService;
    readonly McpHostOptions _options;
    readonly object _stateLock = new();
    Timer _refreshTimer;
    string _lastError = "";
    string _lastArtifactPath = "";

    public McpRuntimeMonitor(McpBrowserContext context, McpPageService pageService, McpHostOptions options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _context.ConnectionStateChanged += OnConnectionStateChanged;
        _context.Collector.Changed += OnCollectorChanged;
    }

    public event EventHandler<McpRuntimeSnapshot> SnapshotChanged;

    public McpRuntimeSnapshot CurrentSnapshot { get; private set; } = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _refreshTimer = new Timer(OnRefreshTimer, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        return RefreshAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public void SetLastError(string message)
    {
        lock (_stateLock)
            _lastError = message ?? "";
        PublishSnapshot();
    }

    public void SetLastArtifactPath(string path)
    {
        lock (_stateLock)
            _lastArtifactPath = path ?? "";
        PublishSnapshot();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);
        CurrentSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);
    }

    void OnConnectionStateChanged(object sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    void OnCollectorChanged(object sender, EventArgs e)
    {
        PublishSnapshot();
    }

    void OnRefreshTimer(object state)
    {
        if (!_context.IsConnected)
            return;

        _ = RefreshAsync();
    }

    void PublishSnapshot()
    {
        var snapshot = BuildSnapshotFromCurrentState();
        CurrentSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);
    }

    async Task<McpRuntimeSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken)
    {
        var activePageUrl = "";
        if (_context.IsConnected)
        {
            try
            {
                var pages = await _pageService.ListPagesAsync(cancellationToken).ConfigureAwait(false);
                var selected = pages.FirstOrDefault(p => p.IsSelected);
                if (selected != null)
                    activePageUrl = selected.Url ?? "";
            }
            catch
            {
                // Best-effort for runtime panel.
            }
        }

        return BuildSnapshotFromCurrentState(activePageUrl);
    }

    McpRuntimeSnapshot BuildSnapshotFromCurrentState(string activePageUrl = null)
    {
        var console = _context.Collector.GetConsoleEntries();
        var network = _context.Collector.GetNetworkEntries();
        string lastError;
        string lastArtifactPath;
        lock (_stateLock)
        {
            lastError = _lastError;
            lastArtifactPath = _lastArtifactPath;
        }

        if (activePageUrl == null)
            activePageUrl = CurrentSnapshot.ActivePageUrl ?? "";

        return new McpRuntimeSnapshot
        {
            IsConnected = _context.IsConnected,
            ChromePort = _options.Port,
            ActivePageUrl = activePageUrl,
            NetworkCount = _context.Collector.NetworkCount,
            ConsoleCount = _context.Collector.ConsoleCount,
            LastError = lastError,
            LastArtifactPath = lastArtifactPath,
            RecentConsole = TakeRecent(console, RecentEntryLimit),
            RecentNetwork = TakeRecent(network, RecentEntryLimit),
        };
    }

    static IReadOnlyList<T> TakeRecent<T>(IReadOnlyList<T> entries, int limit)
    {
        if (entries == null || entries.Count == 0)
            return Array.Empty<T>();

        if (entries.Count <= limit)
            return entries.ToList();

        return entries.Skip(entries.Count - limit).ToList();
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _context.ConnectionStateChanged -= OnConnectionStateChanged;
        _context.Collector.Changed -= OnCollectorChanged;
    }
}
