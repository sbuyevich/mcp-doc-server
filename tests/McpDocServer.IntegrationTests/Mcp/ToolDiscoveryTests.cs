using System.Text.Json;
using McpDocServer.Host.Tools;

namespace McpDocServer.IntegrationTests.Mcp;

public sealed class ToolDiscoveryTests
{
    [Fact]
    public async Task ClientDiscoversExpectedToolsAndSchemas()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var server = await McpTestServer.StartAsync(timeout.Token);

        var tools = await server.Client.ListToolsAsync(cancellationToken: timeout.Token);

        Assert.Equal(
            ToolRegistrationCatalog.ExpectedNames.Order(StringComparer.Ordinal),
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));

        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));
            Assert.NotEqual(JsonValueKind.Undefined, tool.JsonSchema.ValueKind);
            Assert.True(tool.ReturnJsonSchema.HasValue);
            Assert.NotEqual(JsonValueKind.Undefined, tool.ReturnJsonSchema.Value.ValueKind);
        }

        var resolveLibrary = tools.Single(tool => tool.Name == "resolve_library");
        var inputSchema = resolveLibrary.JsonSchema.GetRawText();
        Assert.Contains("query", inputSchema, StringComparison.Ordinal);
        Assert.Contains("includePrerelease", inputSchema, StringComparison.Ordinal);
        Assert.Contains("limit", inputSchema, StringComparison.Ordinal);

        var outputSchema = resolveLibrary.ReturnJsonSchema!.Value.GetRawText();
        Assert.Contains("status", outputSchema, StringComparison.Ordinal);
        Assert.Contains("errors", outputSchema, StringComparison.Ordinal);
    }
}
