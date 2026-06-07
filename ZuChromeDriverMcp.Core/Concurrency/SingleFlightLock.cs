namespace ZuChromeDriverMcp.Core.Concurrency;

/// <summary>
/// Ensures MCP tool handlers run one at a time against the shared browser session.
/// </summary>
public sealed class SingleFlightLock
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    sealed class Releaser : IDisposable
    {
        SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore?.Release();
            _semaphore = null;
        }
    }
}
