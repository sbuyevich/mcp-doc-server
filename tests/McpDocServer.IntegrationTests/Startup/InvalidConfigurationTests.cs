using McpDocServer.Host;
using McpDocServer.Indexer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpDocServer.IntegrationTests.Startup;

public sealed class InvalidConfigurationTests
{
    [Fact]
    public async Task InvalidConfigurationFailsStartup()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings
            {
                Args = [],
                DisableDefaults = true
            });

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["McpDocServer:DatabasePath"] = "data/docs.db",
                ["McpDocServer:Retrieval:DefaultMaxResults"] = "0"
            });
        builder.Logging.ClearProviders();
        builder.Services.AddMcpDocServerCore(builder.Configuration);

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            host.StartAsync(CancellationToken.None));

        Assert.Contains("DefaultMaxResults", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidIndexerConfigurationFailsStartup()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings
            {
                Args = [],
                DisableDefaults = true
            });

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["McpDocServer:DatabasePath"] = "data/docs.db",
                ["McpDocServer:Indexing:MaxPackageBytes"] = "0"
            });
        builder.Logging.ClearProviders();
        builder.Services.AddIndexer(builder.Configuration);

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            host.StartAsync(CancellationToken.None));

        Assert.Contains("MaxPackageBytes", exception.Message, StringComparison.Ordinal);
    }
}
