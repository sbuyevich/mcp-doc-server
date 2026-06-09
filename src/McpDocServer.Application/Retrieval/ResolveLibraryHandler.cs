using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.ResolveLibrary;

namespace McpDocServer.Application.Retrieval;

internal sealed class ResolveLibraryHandler(
    IRetrievalConfigurationProvider configurationProvider,
    INuGetReadStore store,
    IVersionResolver versionResolver) : IResolveLibraryHandler
{
    public async Task<ResolveLibraryResponse> HandleAsync(
        ResolveLibraryRequest request,
        CancellationToken cancellationToken)
    {
        Guard.NotBlank(request.Query, nameof(request.Query));
        Guard.Positive(request.Limit, nameof(request.Limit));
        var settings = configurationProvider.GetSettings();
        using var timeout = RetrievalHandlerSupport.CreateTimeout(
            settings.Limits.QueryTimeout,
            cancellationToken);

        try
        {
            var limit = Math.Min(request.Limit, settings.Limits.MaxResults);
            var rawCandidates = await store.SearchLibrariesAsync(
                settings.DatabasePath,
                request.Query,
                Math.Max(limit * 4, 20),
                timeout.Token);
            var matches = new List<(LibraryMatch Match, string PackageId, int SourceIndex)>();

            foreach (var candidate in rawCandidates)
            {
                if (!request.IncludePrerelease
                    && candidate.LatestPrerelease
                    && !candidate.LatestListed)
                {
                    continue;
                }

                var versions = await store.ListVersionsAsync(
                    settings.DatabasePath,
                    candidate.LibraryId,
                    timeout.Token);
                settings.RecommendedVersions.TryGetValue(candidate.PackageId, out var recommendation);
                var resolution = versionResolver.Resolve(
                    versions,
                    null,
                    null,
                    recommendation,
                    request.IncludePrerelease);
                if (resolution is null)
                {
                    continue;
                }

                var sourceIndex = RetrievalLibraryResolver.SourceIndex(
                    settings.SourceOrder,
                    candidate.SourceName);
                var score = candidate.ExactId
                    ? 1.0
                    : candidate.PrefixId ? 0.9 : candidate.TextScore;
                if (recommendation is not null
                    && versions.Any(version =>
                        version.Version.Equals(recommendation, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 0.05;
                }

                if (sourceIndex != int.MaxValue)
                {
                    score += Math.Max(0, 0.03 - (sourceIndex * 0.005));
                }

                if (resolution.Version.Deprecated)
                {
                    score -= 0.10;
                }

                matches.Add((
                    new LibraryMatch
                    {
                        LibraryId = new LibraryId(candidate.PackageId).ToString(),
                        Kind = "nuget",
                        DisplayName = candidate.PackageId,
                        RecommendedVersion = resolution.Version.Version,
                        Description = candidate.Description,
                        Confidence = Math.Clamp(score, 0, 1)
                    },
                    candidate.PackageId,
                    sourceIndex));
            }

            var selected = matches
                .GroupBy(match => match.PackageId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderBy(item => item.SourceIndex)
                    .ThenByDescending(item => item.Match.Confidence)
                    .ThenBy(item => item.Match.DisplayName, StringComparer.Ordinal)
                    .First())
                .OrderByDescending(item => item.Match.Confidence)
                .ThenBy(item => item.Match.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .Select(item => item.Match)
                .ToArray();

            return selected.Length == 0
                ? new ResolveLibraryResponse
                {
                    Status = ToolResultStatus.NotFound,
                    Data = new ResolveLibraryResult(),
                    Errors =
                    [
                        RetrievalHandlerSupport.Error(
                            "library_not_found",
                            $"No indexed NuGet package matched '{request.Query}'.")
                    ]
                }
                : new ResolveLibraryResponse
                {
                    Status = ToolResultStatus.Ok,
                    Data = new ResolveLibraryResult { Matches = selected }
                };
        }
        catch (IndexUnavailableException exception)
        {
            return new ResolveLibraryResponse
            {
                Status = ToolResultStatus.NotFound,
                Data = new ResolveLibraryResult(),
                Errors = [RetrievalHandlerSupport.IndexUnavailable(exception)]
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResolveLibraryResponse
            {
                Status = ToolResultStatus.InsufficientEvidence,
                Data = new ResolveLibraryResult(),
                Errors =
                [
                    RetrievalHandlerSupport.Error(
                        "query_timeout",
                        "Library resolution exceeded the configured timeout.")
                ]
            };
        }
    }
}
