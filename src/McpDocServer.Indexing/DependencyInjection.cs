using McpDocServer.Indexing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.Indexing;

/// <summary>
/// Indexing feature service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIndexing(this IServiceCollection services)
    {
        services.AddSingleton<IIndexCoordinator, IndexCoordinator>();
        return services;
    }
}
