using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.Application.Retrieval.Services;

public interface IResolveLibraryHandler
{
    Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken);
}
