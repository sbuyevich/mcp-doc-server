using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.Application.Abstractions;

public interface IResolveLibraryHandler
{
    Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken);
}
