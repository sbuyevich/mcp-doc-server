using McpDocServer.Domain.Indexing;

namespace McpDocServer.Application.Indexing;

public sealed record IndexingSettings(
    string DatabasePath,
    PackageProcessingLimits Limits,
    IReadOnlyList<IndexSourceDefinition> Sources);

public sealed record PackageProcessingLimits(
    long MaxPackageBytes,
    long MaxDocumentBytes,
    int MaxArchiveEntries,
    long MaxExtractedBytes,
    double MaxCompressionRatio,
    int MaxDocumentChars,
    TimeSpan PackageDownloadTimeout);

public sealed record IndexSourceDefinition(
    string Name,
    string ServiceIndex,
    IReadOnlyList<string> PackagePrefixes,
    IReadOnlyList<string> PackageIds,
    bool IncludePrerelease,
    bool IncludeUnlisted,
    int MaxVersionsPerPackage,
    int MaxPackages);

public sealed record PackageVersionCandidate(
    string PackageId,
    string Version,
    bool IsListed,
    bool IsDeprecated,
    DateTimeOffset? PublishedAt);

public sealed record IndexRunError(
    string Code,
    string Message,
    string? PackageId = null,
    string? Version = null);

public sealed record IndexRunSummary(
    string SourceName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int Discovered,
    int Indexed,
    int Changed,
    int Unchanged,
    IReadOnlyList<IndexRunError> Errors);

public sealed record IndexPublishResult(int Changed, int Unchanged);

public sealed class DownloadedPackage(
    string filePath,
    string contentHash,
    long length) : IAsyncDisposable
{
    public string FilePath { get; } = filePath;

    public string ContentHash { get; } = contentHash;

    public long Length { get; } = length;

    public ValueTask DisposeAsync()
    {
        try
        {
            File.Delete(FilePath);
        }
        catch (IOException)
        {
            // Cleanup is best-effort; indexing results are already independent of this file.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best-effort; indexing results are already independent of this file.
        }

        return ValueTask.CompletedTask;
    }
}

public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}

public interface IIndexCoordinator
{
    Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(CancellationToken cancellationToken);
}

public interface IPackageSourceClient
{
    Task<IReadOnlyList<PackageVersionCandidate>> DiscoverAsync(
        IndexSourceDefinition source,
        CancellationToken cancellationToken);

    Task<DownloadedPackage> DownloadAsync(
        IndexSourceDefinition source,
        PackageVersionCandidate package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}

public interface IPackageProcessor
{
    Task<PackageIndexData> ProcessAsync(
        PackageVersionCandidate candidate,
        DownloadedPackage package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}

public interface IIndexStore
{
    Task InitializeAsync(string databasePath, CancellationToken cancellationToken);

    Task<IndexPublishResult> PublishSourceAsync(
        string databasePath,
        IndexSourceDefinition source,
        DateTimeOffset startedAt,
        IReadOnlyList<PackageIndexData> packages,
        IReadOnlyCollection<PackageIdentityKey> retainedPackages,
        IReadOnlyList<IndexRunError> errors,
        bool pruneMissing,
        CancellationToken cancellationToken);
}

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}

public interface IContentHasher
{
    string Hash(ReadOnlySpan<byte> content);

    Task<string> HashAsync(Stream stream, CancellationToken cancellationToken);
}

public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
