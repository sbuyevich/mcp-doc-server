namespace McpDocServer.Application.Retrieval.Models;

public sealed record ResolvedLibrarySelection(
    ResolvedLibraryRecord Library,
    IReadOnlyList<IndexedVersionRecord> Versions,
    VersionResolution? Version);
