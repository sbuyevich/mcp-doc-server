using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.FindApiOperation;
using McpDocServer.Application.Contracts.GetSymbol;
using McpDocServer.Application.Contracts.ListVersions;
using McpDocServer.Application.Contracts.QueryDocs;
using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.Application.Placeholders;

internal sealed class PlaceholderResolveLibraryHandler : IResolveLibraryHandler
{
    public Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.Query, nameof(request.Query));
        Guard.Positive(request.Limit, nameof(request.Limit));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlaceholderResponseFactory.Create<ResolveLibraryResponse, ResolveLibraryResult>());
    }
}

internal sealed class PlaceholderQueryDocsHandler : IQueryDocsHandler
{
    public Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.LibraryId, nameof(request.LibraryId));
        Guard.NotBlank(request.Question, nameof(request.Question));
        Guard.Positive(request.MaxResults, nameof(request.MaxResults));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlaceholderResponseFactory.Create<QueryDocsResponse, QueryDocsResult>());
    }
}

internal sealed class PlaceholderGetSymbolHandler : IGetSymbolHandler
{
    public Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.LibraryId, nameof(request.LibraryId));
        Guard.NotBlank(request.Symbol, nameof(request.Symbol));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlaceholderResponseFactory.Create<GetSymbolResponse, GetSymbolResult>());
    }
}

internal sealed class PlaceholderFindApiOperationHandler : IFindApiOperationHandler
{
    public Task<FindApiOperationResponse> HandleAsync(FindApiOperationRequest request, CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.Query, nameof(request.Query));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlaceholderResponseFactory.Create<FindApiOperationResponse, FindApiOperationResult>());
    }
}

internal sealed class PlaceholderListVersionsHandler : IListVersionsHandler
{
    public Task<ListVersionsResponse> HandleAsync(ListVersionsRequest request, CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.LibraryId, nameof(request.LibraryId));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PlaceholderResponseFactory.Create<ListVersionsResponse, ListVersionsResult>());
    }
}

internal static class Guard
{
    public static void NotBlank(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be blank.", parameterName);
        }
    }

    public static void Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
        }
    }
}
