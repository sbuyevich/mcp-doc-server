namespace McpDocServer.Application.Retrieval.Models;

public sealed record RetrievalSettings(
    string DatabasePath,
    IReadOnlyList<string> EnvironmentOrder,
    IReadOnlyList<string> SourceOrder,
    IReadOnlyDictionary<string, string> RecommendedVersions,
    RetrievalLimits Limits);
