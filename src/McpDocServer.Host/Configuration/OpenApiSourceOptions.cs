namespace McpDocServer.Host.Configuration;

/// <summary>
/// Approved OpenAPI source configuration.
/// </summary>
public sealed class OpenApiSourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
}
