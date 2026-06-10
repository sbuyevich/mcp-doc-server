using McpDocServer.Indexer.Models;

namespace McpDocServer.Indexer.Services;

public interface IIndexCoordinator
{
    Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(CancellationToken cancellationToken);
}
