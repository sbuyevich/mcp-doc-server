using McpDocServer.Configuration;
using McpDocServer.Indexing;
using McpDocServer.Indexing.Abstractions;
using McpDocServer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpDocServer.Indexing.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddIndexingWorkerCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<
            IValidateOptions<IndexingWorkerOptions>,
            IndexingWorkerOptionsValidator>();
        services.AddOptions<IndexingWorkerOptions>()
            .Bind(configuration.GetSection(IndexingWorkerOptions.SectionName))
            .ValidateOnStart();

        services.AddIndexing();
        services.AddIndexingInfrastructure();
        services.AddSingleton<
            IIndexingConfigurationProvider,
            OptionsIndexingConfigurationProvider>();
        services.AddSingleton<IndexingRunExecutor>();

        return services;
    }
}
