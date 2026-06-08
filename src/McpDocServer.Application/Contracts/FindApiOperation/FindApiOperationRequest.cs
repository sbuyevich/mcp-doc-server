namespace McpDocServer.Application.Contracts.FindApiOperation;

/// <summary>
/// Request to find an OpenAPI operation and generated client mapping.
/// </summary>
public sealed record FindApiOperationRequest(
    string Query,
    string? Service = null,
    string? LibraryId = null,
    string? Version = null,
    string? HttpMethod = null);
