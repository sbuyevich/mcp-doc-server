using McpDocServer.Application.Indexing.Models;
using McpDocServer.Domain.Indexing;

namespace McpDocServer.Application.Indexing.Abstractions;

public interface IPackageProcessor
{
    Task<PackageIndexData> ProcessAsync(
        PackageVersionCandidate candidate,
        DownloadedPackage package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
