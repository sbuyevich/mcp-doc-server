using McpDocServer.Infrastructure.Diagnostics;
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
        return services;
    }
}
