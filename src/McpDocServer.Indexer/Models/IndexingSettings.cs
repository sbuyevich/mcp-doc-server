namespace McpDocServer.Indexer.Models;

public sealed record IndexingSettings(
    string DatabasePath,
    PackageProcessingLimits Limits,
    IReadOnlyList<IndexSourceDefinition> Sources);
