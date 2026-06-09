using McpDocServer.Application.Retrieval.Abstractions;
using McpDocServer.Application.Retrieval.Models;
using McpDocServer.Host.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace McpDocServer.Host.Retrieval;

internal sealed class OptionsRetrievalConfigurationProvider(
    IOptions<McpDocServerOptions> options,
    IConfiguration configuration) : IRetrievalConfigurationProvider
{
    public RetrievalSettings GetSettings()
    {
        var value = options.Value;
        var retrieval = value.Retrieval;
        var section = configuration.GetSection(McpDocServerOptions.SectionName);
        var defaultMaxResults = section["Retrieval:DefaultMaxResults"] is null
            ? value.Indexing.DefaultMaxResults
            : retrieval.DefaultMaxResults;
        var queryTimeout = section["Retrieval:QueryTimeout"] is null
            ? value.Indexing.RequestTimeout
            : retrieval.QueryTimeout;

        return new(
            value.DatabasePath,
            value.NuGetSources.Select(source => source.Name).ToArray(),
            new Dictionary<string, string>(
                value.RecommendedVersions,
                StringComparer.OrdinalIgnoreCase),
            new RetrievalLimits(
                defaultMaxResults,
                retrieval.MaxResults,
                retrieval.MaxResponseBytes,
                queryTimeout,
                retrieval.MinimumEvidenceScore,
                retrieval.AmbiguousSymbolLimit));
    }
}
