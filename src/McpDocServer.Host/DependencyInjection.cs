using McpDocServer.Application;
using McpDocServer.Application.Retrieval.Abstractions;
using McpDocServer.Configuration;
using McpDocServer.Host.Diagnostics;
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
        services.AddRetrievalInfrastructure();
        services.AddSingleton<IRetrievalConfigurationProvider>(provider =>
            new OptionsRetrievalConfigurationProvider(
                provider.GetRequiredService<IOptions<McpDocServerOptions>>()));
        services.AddSingleton<ToolRegistrationCatalog>();
        services.AddHostedService<StartupDiagnosticsHostedService>();

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
