using System.Text.Json.Serialization;
using McpDocServer.Application.Contracts.Common;

namespace McpDocServer.Application.Contracts.GetSymbol;

/// <summary>
/// Response for the get_symbol tool.
/// </summary>
public sealed record GetSymbolResponse : ToolResponse<GetSymbolResult>;

/// <summary>
/// Symbol lookup payload.
/// </summary>
public sealed record GetSymbolResult
{
    [JsonPropertyName("symbol")]
    public SymbolDetails? Symbol { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<SymbolDetails> Candidates { get; init; } = [];
}

public sealed record SymbolDetails
{
    [JsonPropertyName("fullyQualifiedName")]
    public required string FullyQualifiedName { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("signature")]
    public required string Signature { get; init; }

    [JsonPropertyName("documentation")]
    public string? Documentation { get; init; }

    [JsonPropertyName("assembly")]
    public string? Assembly { get; init; }

    [JsonPropertyName("targetFrameworks")]
    public IReadOnlyList<string> TargetFrameworks { get; init; } = [];

    [JsonPropertyName("citationUri")]
    public string? CitationUri { get; init; }
}
