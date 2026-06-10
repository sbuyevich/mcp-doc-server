using McpDocServer.Indexer;
using McpDocServer.Indexer.Abstractions;
using McpDocServer.Indexer.Cli.Configuration;
using McpDocServer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpDocServer.Indexer.Cli;

/// <summary>
/// Indexer CLI composition and configuration registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIndexerCli(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<IndexerOptions>, IndexerOptionsValidator>();
        services.AddOptions<IndexerOptions>()
            .Bind(configuration.GetSection(IndexerOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IIndexingConfigurationProvider, OptionsIndexingConfigurationProvider>();
        services.AddSingleton<IndexerRunner>();
        services.AddIndexer();
        services.AddIndexingInfrastructure();

        return services;
    }
}
