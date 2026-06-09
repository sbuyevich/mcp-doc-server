using McpDocServer.Application.Indexing;
using McpDocServer.Application.Retrieval;
using McpDocServer.Infrastructure.Diagnostics;
using McpDocServer.Infrastructure.Indexing;
using McpDocServer.Infrastructure.NuGet;
using McpDocServer.Infrastructure.Persistence;
using McpDocServer.Infrastructure.Retrieval;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.Infrastructure;

/// <summary>
/// Infrastructure service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILocalDependencyCheck, LocalDependencyCheck>();
        services.AddSingleton<IContentHasher, Sha256ContentHasher>();
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddSingleton<INuGetSourceAuthenticationProvider, AnonymousNuGetSourceAuthenticationProvider>();
        services.AddSingleton<IPackageSourceClient, NuGetPackageSourceClient>();
        services.AddSingleton<IPackageProcessor, NuGetPackageProcessor>();
        services.AddSingleton<IIndexStore, SqliteIndexStore>();
        services.AddSingleton<INuGetReadStore, SqliteNuGetReadStore>();
        return services;
    }
}
