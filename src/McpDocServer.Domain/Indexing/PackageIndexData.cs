namespace McpDocServer.Domain.Indexing;

public sealed record PackageIndexData(
    string PackageId,
    string Version,
    string ContentHash,
    string? Title,
    string? Description,
    string? Summary,
    string? Authors,
    string? Tags,
    string? ProjectUrl,
    string? RepositoryUrl,
    bool IsListed,
    bool IsPrerelease,
    bool IsDeprecated,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<ArtifactRecord> Artifacts,
    IReadOnlyList<DocumentChunkRecord> Documents,
    IReadOnlyList<SymbolRecord> Symbols,
    IReadOnlyList<DependencyRecord> Dependencies,
    IReadOnlyList<TargetFrameworkRecord> TargetFrameworks);

public sealed record ArtifactRecord(
    string Path,
    string Kind,
    string ContentHash,
    long Size);

public sealed record DocumentChunkRecord(
    string Path,
    string Kind,
    string? MemberName,
    int Ordinal,
    string Content,
    string ContentHash);

public sealed record SymbolRecord(
    string Namespace,
    string FullyQualifiedName,
    string Kind,
    string Signature,
    string? ContainingType,
    string AssemblyPath,
    string? TargetFramework,
    string? XmlDocumentationMember);

public sealed record DependencyRecord(
    string PackageId,
    string VersionRange,
    string? TargetFramework);

public sealed record TargetFrameworkRecord(string Framework);
