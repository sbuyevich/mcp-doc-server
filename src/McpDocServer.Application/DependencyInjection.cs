using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Indexing;
using McpDocServer.Application.Placeholders;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.Application;

/// <summary>
/// Application service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IResolveLibraryHandler, PlaceholderResolveLibraryHandler>();
        services.AddSingleton<IQueryDocsHandler, PlaceholderQueryDocsHandler>();
        services.AddSingleton<IGetSymbolHandler, PlaceholderGetSymbolHandler>();
        services.AddSingleton<IFindApiOperationHandler, PlaceholderFindApiOperationHandler>();
        services.AddSingleton<IListVersionsHandler, PlaceholderListVersionsHandler>();
        services.AddSingleton<IIndexCoordinator, IndexCoordinator>();
        return services;
    }
}
