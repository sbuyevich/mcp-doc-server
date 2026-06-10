using McpDocServer.Application.Retrieval.Models;

namespace McpDocServer.Application.Retrieval.Services;

public interface IRetrievalLibraryResolver
{
    Task<LibraryResolutionResult> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> environmentOrder,
        IReadOnlyList<string> sourceOrder,
        IReadOnlyDictionary<string, string> recommendedVersions,
        string? requestedVersion,
        string? projectVersion,
        bool includePrerelease,
        CancellationToken cancellationToken);
}
