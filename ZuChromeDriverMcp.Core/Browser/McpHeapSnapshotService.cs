using System.Text;
using Zu.Chrome;
using Zu.ChromeDevTools.HeapProfiler;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.Core.Browser;

public sealed class McpHeapSnapshotService
{
    readonly McpHostOptions _options;

    public McpHeapSnapshotService(McpHostOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> CaptureAsync(
        ZuChromeDriver driver,
        string filePath,
        CancellationToken cancellationToken)
    {
        if (driver == null)
            throw new ArgumentNullException(nameof(driver));

        var outputPath = McpArtifactPaths.EnsureExtension(
            McpArtifactPaths.ResolveOutputPath(_options, filePath, "heap.heapsnapshot"),
            ".heapsnapshot");

        var devTools = driver.DevTools;
        var chunks = new StringBuilder();
        var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChunk(AddHeapSnapshotChunkEvent evt)
        {
            if (!string.IsNullOrEmpty(evt?.Chunk))
                chunks.Append(evt.Chunk);
        }

        void OnProgress(ReportHeapSnapshotProgressEvent evt)
        {
            if (evt?.Finished == true)
                finished.TrySetResult(true);
        }

        devTools.HeapProfiler.SubscribeToAddHeapSnapshotChunkEvent(OnChunk);
        devTools.HeapProfiler.SubscribeToReportHeapSnapshotProgressEvent(OnProgress);

        try
        {
            await devTools.HeapProfiler.Enable(new EnableCommand(), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // already enabled
        }

        await devTools.HeapProfiler.TakeHeapSnapshot(
                new TakeHeapSnapshotCommand { ReportProgress = true },
                cancellationToken)
            .ConfigureAwait(false);

        var completed = await Task.WhenAny( 
                finished.Task,
                Task.Delay(TimeSpan.FromMinutes(5), cancellationToken))
            .ConfigureAwait(false);

        if (completed != finished.Task && chunks.Length == 0)
            throw new TimeoutException("Timed out waiting for heap snapshot data from Chrome.");

        if (chunks.Length == 0)
            throw new InvalidOperationException("Heap snapshot capture returned no data.");

        await File.WriteAllTextAsync(outputPath, chunks.ToString(), cancellationToken).ConfigureAwait(false);
        return outputPath;
    }
}
