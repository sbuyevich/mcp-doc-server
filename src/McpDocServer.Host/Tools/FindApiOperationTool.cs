using System.ComponentModel;
using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.FindApiOperation;
using ModelContextProtocol.Server;

namespace McpDocServer.Host.Tools;

[McpServerToolType]
public sealed class FindApiOperationTool(IFindApiOperationHandler handler)
{
    [McpServerTool(
        Name = "find_api_operation",
        UseStructuredContent = true,
        OutputSchemaType = typeof(FindApiOperationResponse))]
    [Description("Finds OpenAPI operations and their generated C# client mappings.")]
    public Task<FindApiOperationResponse> FindApiOperationAsync(
        [Description("Operation ID, route, or business intent to find.")] string query,
        [Description("Optional configured service name.")] string? service = null,
        [Description("Optional stable generated-client library identifier.")] string? libraryId = null,
        [Description("Exact API document or client version.")] string? version = null,
        [Description("Optional HTTP method filter such as GET or POST.")] string? httpMethod = null,
        CancellationToken cancellationToken = default)
    {
        return handler.HandleAsync(
            new FindApiOperationRequest(query, service, libraryId, version, httpMethod),
            cancellationToken);
    }
}
