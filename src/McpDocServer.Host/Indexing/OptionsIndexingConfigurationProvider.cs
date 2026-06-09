using McpDocServer.Application.Indexing.Abstractions;
using McpDocServer.Application.Indexing.Models;
using McpDocServer.Host.Configuration;
using Microsoft.Extensions.Options;

namespace McpDocServer.Host.Indexing;

internal sealed class OptionsIndexingConfigurationProvider(
    IOptions<McpDocServerOptions> options) : IIndexingConfigurationProvider
{
    public IndexingSettings GetSettings()
    {
        var value = options.Value;
        var limits = value.Indexing;

        return new(
            value.DatabasePath,
            new PackageProcessingLimits(
                limits.MaxPackageBytes,
                limits.MaxDocumentBytes,
                limits.MaxArchiveEntries,
                limits.MaxExtractedBytes,
                limits.MaxCompressionRatio,
                limits.MaxDocumentChars,
                limits.PackageDownloadTimeout),
            value.NuGetSources.Select(source => new IndexSourceDefinition(
                source.Name,
                ResolveSource(source.ServiceIndex),
                source.PackagePrefixes.ToArray(),
                source.PackageIds.ToArray(),
                source.IncludePrerelease,
                source.IncludeUnlisted,
                source.MaxVersionsPerPackage,
                source.MaxPackages)).ToArray());
    }

    private static string ResolveSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
                ? source
                : Path.GetFullPath(source);
    }
}
