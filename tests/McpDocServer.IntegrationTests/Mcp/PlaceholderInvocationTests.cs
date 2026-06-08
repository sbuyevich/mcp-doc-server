using System.Text.Json;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.IntegrationTests.Mcp;

public sealed class PlaceholderInvocationTests
{
    [Fact]
    public async Task ToolInvocationReturnsStructuredNotReadyResponse()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var server = await McpTestServer.StartAsync(timeout.Token);

        var result = await server.Client.CallToolAsync(
            "resolve_library",
            new Dictionary<string, object?>
            {
                ["query"] = "customer"
            },
            cancellationToken: timeout.Token);

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);

        var response = result.StructuredContent.Value.Deserialize<ResolveLibraryResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.Equal(ToolResultStatus.NotReady, response.Status);
        var error = Assert.Single(response.Errors);
        Assert.Equal(PlaceholderResponseFactory.ErrorCode, error.Code);
    }
}
