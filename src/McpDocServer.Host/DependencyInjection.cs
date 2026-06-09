using McpDocServer.Application;
using McpDocServer.Application.Indexing;
using McpDocServer.Application.Retrieval;
using McpDocServer.Host.Configuration;
using McpDocServer.Host.Diagnostics;
using McpDocServer.Host.Indexing;
using McpDocServer.Host.Retrieval;
using McpDocServer.Host.Resources;
using McpDocServer.Host.Tools;
using McpDocServer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpDocServer.Host;

/// <summary>
/// Host service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMcpDocServerCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<McpDocServerOptions>, McpDocServerOptionsValidator>();
        services.AddOptions<McpDocServerOptions>()
            .Bind(configuration.GetSection(McpDocServerOptions.SectionName))
            .ValidateOnStart();

        services.AddApplication();
        services.AddInfrastructure();
        services.AddSingleton<IIndexingConfigurationProvider, OptionsIndexingConfigurationProvider>();
        services.AddSingleton<IRetrievalConfigurationProvider>(provider =>
            new OptionsRetrievalConfigurationProvider(
                provider.GetRequiredService<IOptions<McpDocServerOptions>>(),
                configuration));
        services.AddSingleton<ToolRegistrationCatalog>();
        services.AddHostedService<StartupDiagnosticsHostedService>();
        services.AddHostedService<NuGetIndexingHostedService>();

        return services;
    }

    public static IMcpServerBuilder WithMcpDocServerTools(this IMcpServerBuilder builder)
    {
        return builder
            .WithTools<ResolveLibraryTool>()
            .WithTools<QueryDocsTool>()
            .WithTools<GetSymbolTool>()
            .WithTools<ListVersionsTool>()
            .WithResources<NuGetResources>();
    }
}
