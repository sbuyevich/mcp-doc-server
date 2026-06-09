using McpDocServer.Application.Contracts.GetSymbol;

namespace McpDocServer.Application.Retrieval.Services;

public interface IGetSymbolHandler
{
    Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken);
}
