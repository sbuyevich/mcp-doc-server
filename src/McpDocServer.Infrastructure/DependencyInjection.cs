using McpDocServer.Application.Retrieval.Abstractions;
using McpDocServer.Infrastructure.Diagnostics;
using McpDocServer.Infrastructure.Indexing.Abstractions;
using McpDocServer.Infrastructure.Indexing.NuGet;
using McpDocServer.Infrastructure.Indexing.Persistence;
using McpDocServer.Infrastructure.Indexing.Processing;
using McpDocServer.Infrastructure.Retrieval;
using McpDocServer.Indexer.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpDocServer.Infrastructure;

/// <summary>
/// Infrastructure service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRetrievalInfrastructure(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ILocalDependencyCheck, LocalDependencyCheck>();
        services.AddSingleton<INuGetReadStore, SqliteNuGetReadStore>();
        return services;
    }

    public static IServiceCollection AddIndexingInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IContentHasher, Sha256ContentHasher>();
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddSingleton<
            INuGetSourceAuthenticationProvider,
            AnonymousNuGetSourceAuthenticationProvider>();
        services.AddSingleton<IPackageSourceClient, NuGetPackageSourceClient>();
        services.AddSingleton<IPackageProcessor, NuGetPackageProcessor>();
        services.AddSingleton<IIndexStore, SqliteIndexStore>();
        return services;
    }
}
