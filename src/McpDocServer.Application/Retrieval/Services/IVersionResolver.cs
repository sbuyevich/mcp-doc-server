using McpDocServer.Application.Retrieval.Models;

namespace McpDocServer.Application.Retrieval.Services;

public interface IVersionResolver
{
    VersionResolution? Resolve(
        IReadOnlyList<IndexedVersionRecord> versions,
        string? requestedVersion,
        string? projectVersion,
        string? recommendedVersion,
        bool includePrerelease);
}
