using McpDocServer.Application.Contracts.ListVersions;

namespace McpDocServer.Application.Retrieval.Services;

public interface IListVersionsHandler
{
    Task<ListVersionsResponse> HandleAsync(ListVersionsRequest request, CancellationToken cancellationToken);
}
