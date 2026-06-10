using McpDocServer.Configuration;
using McpDocServer.Indexing.Models;
using McpDocServer.Indexing.Services;
using McpDocServer.Indexing.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpDocServer.UnitTests.Indexing;

public sealed class IndexingBackgroundServiceTests
{
    [Fact]
    public async Task RunsImmediatelyAndNeverOverlapsRefreshes()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var coordinator = new TrackingCoordinator();
        var options = Options.Create(
            new IndexingWorkerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "fixture",
                        Environment = "test",
                        ServiceIndex = "fixture",
                        PackageIds = ["Fixture.Package"]
                    }
                ],
                Indexing = new IndexingOptions
                {
                    RefreshInterval = TimeSpan.FromMilliseconds(10)
                }
            });
        var executor = new IndexingRunExecutor(
            options,
            coordinator,
            NullLogger<IndexingRunExecutor>.Instance);
        var service = new IndexingBackgroundService(
            options,
            executor,
            NullLogger<IndexingBackgroundService>.Instance);

        await service.StartAsync(timeout.Token);
        await coordinator.FirstRunStarted.Task.WaitAsync(timeout.Token);
        await coordinator.SecondRunCompleted.Task.WaitAsync(timeout.Token);
        await service.StopAsync(timeout.Token);

        Assert.True(coordinator.RunCount >= 2);
        Assert.Equal(1, coordinator.MaximumConcurrentRuns);
    }

    [Fact]
    public async Task CancellationStopsAnActiveRefresh()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var coordinator = new BlockingCoordinator();
        var options = Options.Create(
            new IndexingWorkerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "fixture",
                        Environment = "test",
                        ServiceIndex = "fixture",
                        PackageIds = ["Fixture.Package"]
                    }
                ]
            });
        var executor = new IndexingRunExecutor(
            options,
            coordinator,
            NullLogger<IndexingRunExecutor>.Instance);
        var service = new IndexingBackgroundService(
            options,
            executor,
            NullLogger<IndexingBackgroundService>.Instance);

        await service.StartAsync(timeout.Token);
        await coordinator.RunStarted.Task.WaitAsync(timeout.Token);
        await service.StopAsync(timeout.Token);

        await coordinator.CancellationObserved.Task.WaitAsync(timeout.Token);
    }

    private sealed class TrackingCoordinator : IIndexCoordinator
    {
        private int _activeRuns;
        private int _maximumConcurrentRuns;
        private int _runCount;

        public TaskCompletionSource FirstRunStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondRunCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int MaximumConcurrentRuns => Volatile.Read(ref _maximumConcurrentRuns);

        public int RunCount => Volatile.Read(ref _runCount);

        public async Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(
            CancellationToken cancellationToken)
        {
            var activeRuns = Interlocked.Increment(ref _activeRuns);
            SetMaximum(activeRuns);
            FirstRunStarted.TrySetResult();

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(30), cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRuns);
            }

            var runCount = Interlocked.Increment(ref _runCount);
            if (runCount >= 2)
            {
                SecondRunCompleted.TrySetResult();
            }

            return
            [
                new IndexRunSummary(
                    "fixture",
                    "succeeded",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    1,
                    1,
                    1,
                    0,
                    [])
            ];
        }

        private void SetMaximum(int value)
        {
            int current;
            do
            {
                current = Volatile.Read(ref _maximumConcurrentRuns);
                if (value <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                ref _maximumConcurrentRuns,
                value,
                current) != current);
        }
    }

    private sealed class BlockingCoordinator : IIndexCoordinator
    {
        public TaskCompletionSource RunStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CancellationObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(
            CancellationToken cancellationToken)
        {
            RunStarted.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                CancellationObserved.TrySetResult();
                throw;
            }

            return [];
        }
    }
}
