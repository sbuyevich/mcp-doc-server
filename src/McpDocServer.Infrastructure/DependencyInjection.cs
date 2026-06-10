using McpDocServer.Application.Retrieval.Abstractions;
using McpDocServer.Infrastructure.Diagnostics;
using McpDocServer.Infrastructure.Retrieval;
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
}
