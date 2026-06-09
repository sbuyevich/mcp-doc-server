using McpDocServer.Application.Contracts.QueryDocs;

namespace McpDocServer.Application.Retrieval.Services;

public interface IQueryDocsHandler
{
    Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken);
}
