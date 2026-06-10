using McpDocServer.Indexing.Models;

namespace McpDocServer.Indexing.Services;

public interface IIndexCoordinator
{
    Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(CancellationToken cancellationToken);
}
