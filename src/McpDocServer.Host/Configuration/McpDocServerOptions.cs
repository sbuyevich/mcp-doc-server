namespace McpDocServer.Host.Configuration;

/// <summary>
/// Root configuration for the MCP documentation server.
/// </summary>
public sealed class McpDocServerOptions
{
    public const string SectionName = "McpDocServer";

    public string Transport { get; set; } = "stdio";

    public HttpHostOptions Http { get; set; } = new();

    public string DatabasePath { get; set; } = "data/docs.db";

    public Dictionary<string, string> RecommendedVersions { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<NuGetSourceOptions> NuGetSources { get; set; } = [];

    public List<RepositorySourceOptions> RepositorySources { get; set; } = [];

    public IndexingOptions Indexing { get; set; } = new();

    public RetrievalOptions Retrieval { get; set; } = new();
}
