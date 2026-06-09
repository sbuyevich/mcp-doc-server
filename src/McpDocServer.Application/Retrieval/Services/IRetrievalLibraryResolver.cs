using McpDocServer.Application.Retrieval.Models;

namespace McpDocServer.Application.Retrieval.Services;

public interface IRetrievalLibraryResolver
{
    Task<ResolvedLibrarySelection?> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> sourceOrder,
        IReadOnlyDictionary<string, string> recommendedVersions,
        CancellationToken cancellationToken);
}
