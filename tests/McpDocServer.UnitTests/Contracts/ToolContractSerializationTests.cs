using System.Text.Json;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.QueryDocs;
using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.UnitTests.Contracts;

public sealed class ToolContractSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PlaceholderResponseSerializesToExpectedShape()
    {
        var response = PlaceholderResponseFactory.Create<ResolveLibraryResponse, ResolveLibraryResult>();

        var json = JsonSerializer.Serialize(response, SerializerOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("not_ready", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("resolvedContext").ValueKind);
        Assert.Empty(root.GetProperty("evidence").EnumerateArray());
        Assert.Empty(root.GetProperty("citations").EnumerateArray());
        Assert.Empty(root.GetProperty("warnings").EnumerateArray());

        var error = Assert.Single(root.GetProperty("errors").EnumerateArray());
        Assert.Equal("stage_not_implemented", error.GetProperty("code").GetString());
        Assert.Equal(
            "This capability is planned for a later stage.",
            error.GetProperty("message").GetString());
    }

    [Fact]
    public void RequestAndResponseContractsRoundTrip()
    {
        var request = new QueryDocsRequest(
            "nuget:Company.Customer.Client",
            "How do I register the client?",
            "4.2.0",
            "net10.0",
            8);
        var response = PlaceholderResponseFactory.Create<QueryDocsResponse, QueryDocsResult>();

        var requestJson = JsonSerializer.Serialize(request, SerializerOptions);
        var responseJson = JsonSerializer.Serialize(response, SerializerOptions);

        var deserializedRequest = JsonSerializer.Deserialize<QueryDocsRequest>(requestJson, SerializerOptions);
        var deserializedResponse = JsonSerializer.Deserialize<QueryDocsResponse>(responseJson, SerializerOptions);

        Assert.Equal(request, deserializedRequest);
        Assert.NotNull(deserializedResponse);
        Assert.Equal(ToolResultStatus.NotReady, deserializedResponse.Status);
    }
}
