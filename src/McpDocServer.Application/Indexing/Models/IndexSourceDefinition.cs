namespace McpDocServer.Application.Indexing.Models;

public sealed record IndexSourceDefinition(
    string Name,
    string ServiceIndex,
    IReadOnlyList<string> PackagePrefixes,
    IReadOnlyList<string> PackageIds,
    bool IncludePrerelease,
    bool IncludeUnlisted,
    int MaxVersionsPerPackage,
    int MaxPackages);
