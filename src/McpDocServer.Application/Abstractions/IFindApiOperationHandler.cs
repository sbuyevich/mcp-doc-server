using McpDocServer.Application.Contracts.FindApiOperation;

namespace McpDocServer.Application.Abstractions;

public interface IFindApiOperationHandler
{
    Task<FindApiOperationResponse> HandleAsync(FindApiOperationRequest request, CancellationToken cancellationToken);
}
