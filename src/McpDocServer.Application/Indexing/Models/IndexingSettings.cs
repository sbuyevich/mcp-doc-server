namespace McpDocServer.Application.Indexing.Models;

public sealed record IndexingSettings(
    string DatabasePath,
    PackageProcessingLimits Limits,
    IReadOnlyList<IndexSourceDefinition> Sources);
