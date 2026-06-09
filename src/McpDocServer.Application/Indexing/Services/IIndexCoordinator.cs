using McpDocServer.Application.Indexing.Models;

namespace McpDocServer.Application.Indexing.Services;

public interface IIndexCoordinator
{
    Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(CancellationToken cancellationToken);
}
