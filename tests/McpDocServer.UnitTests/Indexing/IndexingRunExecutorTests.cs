using McpDocServer.Configuration;
using McpDocServer.Indexing.Models;
using McpDocServer.Indexing.Services;
using McpDocServer.Indexing.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpDocServer.UnitTests.Indexing;

public sealed class IndexingRunExecutorTests
{
    [Fact]
    public async Task NoConfiguredSourcesSucceedsWithoutInvokingCoordinator()
    {
        var coordinator = new UnexpectedCoordinator();
        var executor = new IndexingRunExecutor(
            Options.Create(new IndexingWorkerOptions()),
            coordinator,
            NullLogger<IndexingRunExecutor>.Instance);

        var succeeded = await executor.RunOnceAsync(CancellationToken.None);

        Assert.True(succeeded);
        Assert.False(coordinator.WasCalled);
    }

    private sealed class UnexpectedCoordinator : IIndexCoordinator
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("Coordinator should not be called.");
        }
    }
}
