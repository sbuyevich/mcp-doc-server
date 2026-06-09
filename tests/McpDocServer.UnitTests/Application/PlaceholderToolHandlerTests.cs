using McpDocServer.Application;
using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.FindApiOperation;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.UnitTests.Application;

public sealed class PlaceholderToolHandlerTests
{
    [Fact]
    public async Task OpenApiHandlerRemainsNotReady()
    {
        using var provider = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();

        var findOperation = await provider.GetRequiredService<IFindApiOperationHandler>()
            .HandleAsync(new FindApiOperationRequest("create customer"), CancellationToken.None);

        AssertPlaceholder(findOperation);
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
            provider.GetRequiredService<IFindApiOperationHandler>()
                .HandleAsync(
                    new FindApiOperationRequest("create customer"),
                    cancellationTokenSource.Token));
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
