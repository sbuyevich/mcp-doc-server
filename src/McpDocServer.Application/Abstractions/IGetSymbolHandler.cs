using McpDocServer.Application.Contracts.GetSymbol;

namespace McpDocServer.Application.Abstractions;

public interface IGetSymbolHandler
{
    Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken);
}
