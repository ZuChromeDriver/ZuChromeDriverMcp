using Zu.Chrome;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Snapshot;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpBrowserContext : IAsyncDisposable
{
    readonly McpHostOptions _options;
    ZuChromeDriver _driver;

    public McpBrowserContext(
        McpHostOptions options,
        McpSnapshotStore snapshotStore,
        McpDevToolsCollector collector)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        SnapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        Collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    public McpSnapshotStore SnapshotStore { get; }

    public McpDevToolsCollector Collector { get; }

    public ZuChromeDriver Driver
    {
        get
        {
            if (_driver == null)
                throw new InvalidOperationException("Chrome is not connected yet. Wait for host startup to finish.");

            return _driver;
        }
    }

    public bool IsConnected => _driver != null && _driver.IsConnected;

    public event EventHandler ConnectionStateChanged;

    public bool TryGetDriver(out ZuChromeDriver driver)
    {
        if (_driver != null && _driver.IsConnected)
        {
            driver = _driver;
            return true;
        }

        driver = null;
        return false;
    }

    internal void RaiseConnectionStateChanged()
    {
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_driver != null && _driver.IsConnected)
            return;

        if (_driver != null)
            await ShutdownAsync(cancellationToken).ConfigureAwait(false);

        var config = _options.ToChromeDriverConfig();
        _driver = new ZuChromeDriver(config);
        await _driver.Connect(cancellationToken).ConfigureAwait(false);
        await EnsureCollectorForCurrentSessionAsync(cancellationToken).ConfigureAwait(false);
        RaiseConnectionStateChanged();
    }

    public async Task EnsureCollectorForCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableNetworkCaptureOnConnect && !_options.EnableConsoleCaptureOnConnect)
            return;

        Collector.ResetSubscription();
        await Collector.EnsureSubscribedAsync(Driver, cancellationToken).ConfigureAwait(false);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var driver = _driver;
        if (driver == null)
            return;

        _driver = null;

        try
        {
            // CloseSync quits the browser; Close() only closes the current window when multiple tabs are open.
            await Task.Run(() => driver.CloseSync(), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
        finally
        {
            SnapshotStore.Clear();
            Collector.ResetForNavigation();
            Collector.ResetSubscription();
            RaiseConnectionStateChanged();
        }
    }

    public async Task<string> SaveTemporaryFileAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Screenshot data is empty.", nameof(data));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var directory = _options.GetArtifactsDirectory();
        Directory.CreateDirectory(directory);

        var safeName = Path.GetFileName(fileName);
        var filePath = Path.Combine(directory, $"{Guid.NewGuid():N}_{safeName}");
        await File.WriteAllBytesAsync(filePath, data, cancellationToken).ConfigureAwait(false);
        return filePath;
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync().ConfigureAwait(false);
    }
}
