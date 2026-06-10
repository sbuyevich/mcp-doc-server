using McpDocServer.Application.Retrieval.Abstractions;
using McpDocServer.Application.Retrieval.Models;
using McpDocServer.Configuration;
using Microsoft.Extensions.Options;

namespace McpDocServer.Host.Retrieval;

internal sealed class OptionsRetrievalConfigurationProvider(
    IOptions<McpDocServerOptions> options) : IRetrievalConfigurationProvider
{
    public RetrievalSettings GetSettings()
    {
        var value = options.Value;
        var retrieval = value.Retrieval;

        return new(
            value.DatabasePath,
            retrieval.EnvironmentOrder.ToArray(),
            retrieval.SourceOrder.ToArray(),
            new Dictionary<string, string>(
                value.RecommendedVersions,
                StringComparer.OrdinalIgnoreCase),
            new RetrievalLimits(
                retrieval.DefaultMaxResults,
                retrieval.MaxResults,
                retrieval.MaxResponseBytes,
                retrieval.QueryTimeout,
                retrieval.MinimumEvidenceScore,
                retrieval.AmbiguousSymbolLimit));
    }
}
