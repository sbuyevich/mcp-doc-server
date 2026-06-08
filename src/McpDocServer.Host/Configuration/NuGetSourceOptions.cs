namespace McpDocServer.Host.Configuration;

/// <summary>
/// Approved NuGet v3 source configuration.
/// </summary>
public sealed class NuGetSourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string ServiceIndex { get; set; } = string.Empty;

    public List<string> PackagePrefixes { get; set; } = [];
}
