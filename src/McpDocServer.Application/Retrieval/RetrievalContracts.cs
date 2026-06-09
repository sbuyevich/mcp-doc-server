namespace McpDocServer.Application.Retrieval;

public sealed record RetrievalSettings(
    string DatabasePath,
    IReadOnlyList<string> SourceOrder,
    IReadOnlyDictionary<string, string> RecommendedVersions,
    RetrievalLimits Limits);

public sealed record RetrievalLimits(
    int DefaultMaxResults,
    int MaxResults,
    int MaxResponseBytes,
    TimeSpan QueryTimeout,
    double MinimumEvidenceScore,
    int AmbiguousSymbolLimit);

public sealed record LibraryCandidateRecord(
    string LibraryId,
    string SourceName,
    string PackageId,
    string? Description,
    string? LatestVersion,
    bool LatestListed,
    bool LatestPrerelease,
    bool LatestDeprecated,
    bool ExactId,
    bool PrefixId,
    double TextScore);

public sealed record IndexedVersionRecord(
    string LibraryVersionId,
    string Version,
    bool Listed,
    bool Prerelease,
    bool Deprecated,
    DateTimeOffset? PublishedAt);

public sealed record ResolvedLibraryRecord(
    string LibraryId,
    string SourceName,
    string PackageId,
    string? Description);

public sealed record VersionResolution(
    IndexedVersionRecord Version,
    string Reason,
    IReadOnlyList<string> WarningCodes);

public sealed record DocumentHitRecord(
    string Path,
    string Kind,
    string? MemberName,
    string Content,
    string ContentHash,
    double Rank);

public sealed record SymbolHitRecord(
    string FullyQualifiedName,
    string Kind,
    string Signature,
    string? ContainingType,
    string AssemblyPath,
    string? TargetFramework,
    string? XmlDocumentationMember,
    string? Documentation,
    int MatchTier);

public sealed record ResourceDocumentRecord(string Text, string MimeType);

public sealed record ResolvedLibrarySelection(
    ResolvedLibraryRecord Library,
    IReadOnlyList<IndexedVersionRecord> Versions);

public interface IRetrievalConfigurationProvider
{
    RetrievalSettings GetSettings();
}

public interface INuGetReadStore
{
    Task<IReadOnlyList<LibraryCandidateRecord>> SearchLibrariesAsync(
        string databasePath,
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResolvedLibraryRecord>> FindLibrariesAsync(
        string databasePath,
        string packageId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IndexedVersionRecord>> ListVersionsAsync(
        string databasePath,
        string libraryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentHitRecord>> SearchDocumentsAsync(
        string databasePath,
        string libraryVersionId,
        string question,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SymbolHitRecord>> SearchSymbolsAsync(
        string databasePath,
        string libraryVersionId,
        string query,
        string? targetFramework,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SymbolHitRecord>> GetRelatedSymbolsAsync(
        string databasePath,
        string libraryVersionId,
        string containingType,
        string fullyQualifiedName,
        int limit,
        CancellationToken cancellationToken);

    Task<ResourceDocumentRecord?> ReadArtifactAsync(
        string databasePath,
        string sourceName,
        string packageId,
        string version,
        string path,
        CancellationToken cancellationToken);

    Task<ResourceDocumentRecord?> ReadSymbolAsync(
        string databasePath,
        string sourceName,
        string packageId,
        string version,
        string qualifiedName,
        CancellationToken cancellationToken);
}

public interface IVersionResolver
{
    VersionResolution? Resolve(
        IReadOnlyList<IndexedVersionRecord> versions,
        string? requestedVersion,
        string? projectVersion,
        string? recommendedVersion,
        bool includePrerelease);
}

public interface ICitationFactory
{
    string ArtifactUri(string source, string packageId, string version, string path);

    string SymbolUri(string source, string packageId, string version, string qualifiedName);
}

public interface IResponseBudget
{
    IReadOnlyList<T> Take<T>(
        IReadOnlyList<T> values,
        int maximumCount,
        int maximumBytes,
        Func<T, string> textSelector,
        out bool truncated);
}

public interface IRetrievalLibraryResolver
{
    Task<ResolvedLibrarySelection?> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> sourceOrder,
        IReadOnlyDictionary<string, string> recommendedVersions,
        CancellationToken cancellationToken);
}
