namespace McpDocServer.Configuration;

/// <summary>
/// Root configuration for the indexing worker.
/// </summary>
public sealed class IndexingWorkerOptions
{
    public const string SectionName = "McpDocServer";

    public string DatabasePath { get; set; } = "data/docs.db";

    public List<NuGetSourceOptions> NuGetSources { get; set; } = [];

    public List<RepositorySourceOptions> RepositorySources { get; set; } = [];

    public IndexingOptions Indexing { get; set; } = new();
}
