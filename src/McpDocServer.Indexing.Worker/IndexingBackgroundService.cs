using McpDocServer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpDocServer.Indexing.Worker;

internal sealed class IndexingBackgroundService(
    IOptions<IndexingWorkerOptions> options,
    IndexingRunExecutor executor,
    ILogger<IndexingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await executor.RunOnceAsync(stoppingToken);

            logger.LogDebug(
                "Next indexing refresh is scheduled after {RefreshInterval}.",
                options.Value.Indexing.RefreshInterval);

            await Task.Delay(
                options.Value.Indexing.RefreshInterval,
                stoppingToken);
        }
    }
}
