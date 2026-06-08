using System.Text.Json.Serialization;
using McpDocServer.Application.Contracts.Common;

namespace McpDocServer.Application.Contracts.FindApiOperation;

/// <summary>
/// Response for the find_api_operation tool.
/// </summary>
public sealed record FindApiOperationResponse : ToolResponse<FindApiOperationResult>;

/// <summary>
/// Operation search payload.
/// </summary>
public sealed record FindApiOperationResult
{
    [JsonPropertyName("operations")]
    public IReadOnlyList<ApiOperationMatch> Operations { get; init; } = [];
}

public sealed record ApiOperationMatch
{
    [JsonPropertyName("operationId")]
    public required string OperationId { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("clientMapping")]
    public ClientMapping? ClientMapping { get; init; }

    [JsonPropertyName("citationUri")]
    public string? CitationUri { get; init; }
}

public sealed record ClientMapping
{
    [JsonPropertyName("clientType")]
    public required string ClientType { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("requestType")]
    public string? RequestType { get; init; }

    [JsonPropertyName("responseType")]
    public string? ResponseType { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("generator")]
    public string? Generator { get; init; }
}
