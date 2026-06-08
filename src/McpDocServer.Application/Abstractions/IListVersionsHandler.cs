using McpDocServer.Application.Contracts.ListVersions;

namespace McpDocServer.Application.Abstractions;

public interface IListVersionsHandler
{
    Task<ListVersionsResponse> HandleAsync(ListVersionsRequest request, CancellationToken cancellationToken);
}
