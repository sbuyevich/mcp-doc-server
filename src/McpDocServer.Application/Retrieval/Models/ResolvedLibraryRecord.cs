namespace McpDocServer.Application.Retrieval.Models;

public sealed record ResolvedLibraryRecord(
    string LibraryId,
    string SourceName,
    string PackageId,
    string? Description);
