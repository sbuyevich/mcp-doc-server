namespace McpDocServer.Application.Retrieval.Models;

public sealed record IndexedVersionRecord(
    string LibraryVersionId,
    string Version,
    bool Listed,
    bool Prerelease,
    bool Deprecated,
    DateTimeOffset? PublishedAt);
