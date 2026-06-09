namespace McpDocServer.Application.Retrieval.Models;

public sealed record RetrievalLimits(
    int DefaultMaxResults,
    int MaxResults,
    int MaxResponseBytes,
    TimeSpan QueryTimeout,
    double MinimumEvidenceScore,
    int AmbiguousSymbolLimit);
