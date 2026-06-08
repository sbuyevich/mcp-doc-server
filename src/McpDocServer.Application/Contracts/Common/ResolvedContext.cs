using System.Text.Json.Serialization;

namespace McpDocServer.Application.Contracts.Common;

/// <summary>
/// Package, source, and version context used to produce a response.
/// </summary>
public sealed record ResolvedContext
{
    /// <summary>
    /// Stable library identifier, such as nuget:Company.Customer.Client.
    /// </summary>
    [JsonPropertyName("libraryId")]
    public string? LibraryId { get; init; }

    /// <summary>
    /// Source identifier from configuration.
    /// </summary>
    [JsonPropertyName("sourceId")]
    public string? SourceId { get; init; }

    /// <summary>
    /// Version searched by the tool.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>
    /// Reason the version was selected.
    /// </summary>
    [JsonPropertyName("versionSelectionReason")]
    public string? VersionSelectionReason { get; init; }
}
