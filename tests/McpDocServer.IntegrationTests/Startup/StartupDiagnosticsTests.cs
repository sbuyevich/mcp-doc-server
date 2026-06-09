namespace McpDocServer.IntegrationTests.Startup;

public sealed class StartupDiagnosticsTests
{
    [Fact]
    public async Task InMemoryServerStartsAndStopsCleanly()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var server = await Mcp.McpTestServer.StartAsync(timeout.Token);

        var tools = await server.Client.ListToolsAsync(cancellationToken: timeout.Token);

        Assert.Equal(4, tools.Count);
    }
}
