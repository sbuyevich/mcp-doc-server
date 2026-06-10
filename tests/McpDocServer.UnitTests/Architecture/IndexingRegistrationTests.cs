using McpDocServer.Indexer;
using McpDocServer.Indexer.Abstractions;
using McpDocServer.Indexer.Services;
using McpDocServer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.UnitTests.Architecture;

public sealed class IndexingRegistrationTests
{
    [Fact]
    public void IndexerRegistersOrchestrationWithoutConcreteAdapters()
    {
        var services = new ServiceCollection();

        services.AddIndexer();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IIndexCoordinator));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IIndexStore));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IPackageSourceClient));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IPackageProcessor));
    }

    [Fact]
    public void InfrastructureRegistersIndexerPortImplementations()
    {
        var services = new ServiceCollection();

        services.AddIndexingInfrastructure();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IIndexStore>());
        Assert.NotNull(provider.GetService<IPackageSourceClient>());
        Assert.NotNull(provider.GetService<IPackageProcessor>());
    }
}
