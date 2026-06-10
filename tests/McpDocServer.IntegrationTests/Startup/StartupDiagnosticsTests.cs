using McpDocServer.Host;
using McpDocServer.Indexing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.IntegrationTests.Startup;

public sealed class StartupDiagnosticsTests
{
    [Fact]
    public async Task InMemoryServerStartsAndStopsCleanly()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var server = await Mcp.McpTestServer.StartAsync(timeout.Token);

        var tools = await server.Client.ListToolsAsync(cancellationToken: timeout.Token);

        Assert.Equal(4, tools.Count);
    }

    [Fact]
    public void HostCoreDoesNotRegisterIndexingServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["McpDocServer:DatabasePath"] = "data/docs.db"
                })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMcpDocServerCore(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<IIndexCoordinator>());
    }
}
