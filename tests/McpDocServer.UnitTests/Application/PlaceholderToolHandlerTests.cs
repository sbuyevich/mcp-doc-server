using McpDocServer.Application;
using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.FindApiOperation;
using McpDocServer.Application.Contracts.GetSymbol;
using McpDocServer.Application.Contracts.ListVersions;
using McpDocServer.Application.Contracts.QueryDocs;
using McpDocServer.Application.Contracts.ResolveLibrary;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.UnitTests.Application;

public sealed class PlaceholderToolHandlerTests
{
    [Fact]
    public async Task AllHandlersReturnNotReady()
    {
        using var provider = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();

        var resolve = await provider.GetRequiredService<IResolveLibraryHandler>()
            .HandleAsync(new ResolveLibraryRequest("customer"), CancellationToken.None);
        var queryDocs = await provider.GetRequiredService<IQueryDocsHandler>()
            .HandleAsync(new QueryDocsRequest("nuget:Customer", "How do I use it?"), CancellationToken.None);
        var getSymbol = await provider.GetRequiredService<IGetSymbolHandler>()
            .HandleAsync(new GetSymbolRequest("nuget:Customer", "CustomerClient"), CancellationToken.None);
        var findOperation = await provider.GetRequiredService<IFindApiOperationHandler>()
            .HandleAsync(new FindApiOperationRequest("create customer"), CancellationToken.None);
        var listVersions = await provider.GetRequiredService<IListVersionsHandler>()
            .HandleAsync(new ListVersionsRequest("nuget:Customer"), CancellationToken.None);

        AssertPlaceholder(resolve);
        AssertPlaceholder(queryDocs);
        AssertPlaceholder(getSymbol);
        AssertPlaceholder(findOperation);
        AssertPlaceholder(listVersions);
    }

    [Fact]
    public async Task HandlerObservesCancellation()
    {
        using var provider = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IResolveLibraryHandler>()
                .HandleAsync(new ResolveLibraryRequest("customer"), cancellationTokenSource.Token));
    }

    private static void AssertPlaceholder<TData>(ToolResponse<TData> response)
    {
        Assert.Equal(ToolResultStatus.NotReady, response.Status);
        Assert.Null(response.Data);
        Assert.Null(response.ResolvedContext);
        Assert.Empty(response.Evidence);
        Assert.Empty(response.Citations);
        Assert.Empty(response.Warnings);
        var error = Assert.Single(response.Errors);
        Assert.Equal(PlaceholderResponseFactory.ErrorCode, error.Code);
        Assert.Equal(PlaceholderResponseFactory.ErrorMessage, error.Message);
    }
}
