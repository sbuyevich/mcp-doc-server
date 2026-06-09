using McpDocServer.Application.Indexing.Services;
using McpDocServer.Host.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpDocServer.Host.Indexing;

internal sealed class NuGetIndexingHostedService(
    IOptions<McpDocServerOptions> options,
    IIndexCoordinator indexCoordinator,
    ILogger<NuGetIndexingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Indexing.RunOnStartup)
        {
            logger.LogDebug("NuGet indexing on startup is disabled.");
            return;
        }

        if (options.Value.NuGetSources.Count == 0)
        {
            logger.LogInformation(
                "NuGet indexing on startup was requested, but no NuGet sources are configured.");
            return;
        }

        try
        {
            var summaries = await indexCoordinator.IndexAllAsync(stoppingToken);
            foreach (var summary in summaries)
            {
                logger.LogInformation(
                    "NuGet index run completed. Source={SourceName}, Status={Status}, Discovered={Discovered}, Indexed={Indexed}, Changed={Changed}, Unchanged={Unchanged}, Errors={ErrorCount}",
                    summary.SourceName,
                    summary.Status,
                    summary.Discovered,
                    summary.Indexed,
                    summary.Changed,
                    summary.Unchanged,
                    summary.Errors.Count);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("NuGet startup indexing was cancelled.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NuGet startup indexing failed.");
        }
    }
}
