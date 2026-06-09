namespace McpDocServer.Application.Retrieval;

internal sealed class RetrievalLibraryResolver(INuGetReadStore store) :
    IRetrievalLibraryResolver
{
    public async Task<ResolvedLibrarySelection?> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> sourceOrder,
        IReadOnlyDictionary<string, string> recommendedVersions,
        CancellationToken cancellationToken)
    {
        var libraries = await store.FindLibrariesAsync(
            databasePath,
            libraryId.PackageId,
            cancellationToken);
        if (libraries.Count == 0)
        {
            return null;
        }

        recommendedVersions.TryGetValue(libraryId.PackageId, out var recommendation);
        var candidates = new List<(ResolvedLibraryRecord Library, IReadOnlyList<IndexedVersionRecord> Versions)>();
        foreach (var library in libraries)
        {
            candidates.Add((
                library,
                await store.ListVersionsAsync(databasePath, library.LibraryId, cancellationToken)));
        }

        var selected = candidates
            .OrderBy(candidate => SourceIndex(sourceOrder, candidate.Library.SourceName))
            .ThenByDescending(candidate =>
                recommendation is not null
                && candidate.Versions.Any(version =>
                    version.Version.Equals(recommendation, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(candidate =>
                candidate.Versions.Any(version => version.Listed && !version.Prerelease))
            .ThenBy(candidate => candidate.Library.SourceName, StringComparer.Ordinal)
            .First();

        return new(selected.Library, selected.Versions);
    }

    internal static int SourceIndex(IReadOnlyList<string> sourceOrder, string sourceName)
    {
        for (var index = 0; index < sourceOrder.Count; index++)
        {
            if (sourceOrder[index].Equals(sourceName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return int.MaxValue;
    }
}
