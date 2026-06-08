using McpDocServer.Application.Contracts.QueryDocs;

namespace McpDocServer.Application.Abstractions;

public interface IQueryDocsHandler
{
    Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken);
}
