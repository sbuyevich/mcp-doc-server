using McpDocServer.Indexer.Abstractions;
using McpDocServer.Indexer.Configuration;
using McpDocServer.Indexer.NuGet;
using McpDocServer.Indexer.Persistence;
using McpDocServer.Indexer.Processing;
using McpDocServer.Indexer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpDocServer.Indexer;

/// <summary>
/// Indexer configuration and service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIndexer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<IndexerOptions>, IndexerOptionsValidator>();
        services.AddOptions<IndexerOptions>()
            .Bind(configuration.GetSection(IndexerOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IIndexCoordinator, IndexCoordinator>();
        services.AddSingleton<IContentHasher, Sha256ContentHasher>();
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddSingleton<
            INuGetSourceAuthenticationProvider,
            AnonymousNuGetSourceAuthenticationProvider>();
        services.AddSingleton<IPackageSourceClient, NuGetPackageSourceClient>();
        services.AddSingleton<IPackageProcessor, NuGetPackageProcessor>();
        services.AddSingleton<IIndexStore, SqliteIndexStore>();
        services.AddSingleton<IIndexingConfigurationProvider, OptionsIndexingConfigurationProvider>();
        services.AddSingleton<IndexerRunner>();

        return services;
    }
}
