using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace McpDocServer.Configuration;

/// <summary>
/// Validates retrieval host configuration before the MCP transport starts.
/// </summary>
public sealed partial class McpDocServerOptionsValidator : IValidateOptions<McpDocServerOptions>
{
    public ValidateOptionsResult Validate(string? name, McpDocServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ValidateTransport(options, failures);
        ConfigurationValidation.ValidatePath(
            options.DatabasePath,
            "McpDocServer:DatabasePath",
            failures);
        ValidateRetrieval(options.Retrieval, failures);
        ValidateRecommendedVersions(options.RecommendedVersions, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateTransport(
        McpDocServerOptions options,
        List<string> failures)
    {
        if (options.Transport is not ("stdio" or "http"))
        {
            failures.Add("McpDocServer:Transport must be 'stdio' or 'http'.");
        }

        if (!Uri.TryCreate(options.Http.Url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp
            || !uri.IsLoopback
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            failures.Add(
                "McpDocServer:Http:Url must be an absolute HTTP loopback URL without a path, query, fragment, or credentials.");
        }

        if (string.IsNullOrWhiteSpace(options.Http.Path)
            || !options.Http.Path.StartsWith("/", StringComparison.Ordinal)
            || options.Http.Path.Contains("?", StringComparison.Ordinal)
            || options.Http.Path.Contains("#", StringComparison.Ordinal))
        {
            failures.Add(
                "McpDocServer:Http:Path must start with '/' and must not contain a query or fragment.");
        }
    }

    private static void ValidateRetrieval(RetrievalOptions options, List<string> failures)
    {
        if (options.SourceOrder.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("McpDocServer:Retrieval:SourceOrder contains an empty source name.");
        }

        var duplicateSource = options.SourceOrder
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .GroupBy(source => source, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSource is not null)
        {
            failures.Add(
                $"McpDocServer:Retrieval:SourceOrder contains duplicate source '{duplicateSource.Key}'.");
        }

        if (options.DefaultMaxResults <= 0)
        {
            failures.Add("McpDocServer:Retrieval:DefaultMaxResults must be positive.");
        }

        if (options.MaxResults <= 0 || options.MaxResults < options.DefaultMaxResults)
        {
            failures.Add(
                "McpDocServer:Retrieval:MaxResults must be positive and at least DefaultMaxResults.");
        }

        if (options.MaxResponseBytes <= 0)
        {
            failures.Add("McpDocServer:Retrieval:MaxResponseBytes must be positive.");
        }

        if (options.QueryTimeout <= TimeSpan.Zero)
        {
            failures.Add("McpDocServer:Retrieval:QueryTimeout must be positive.");
        }

        if (!double.IsFinite(options.MinimumEvidenceScore)
            || options.MinimumEvidenceScore is < 0 or > 1)
        {
            failures.Add(
                "McpDocServer:Retrieval:MinimumEvidenceScore must be between 0 and 1.");
        }

        if (options.AmbiguousSymbolLimit <= 0)
        {
            failures.Add("McpDocServer:Retrieval:AmbiguousSymbolLimit must be positive.");
        }
    }

    private static void ValidateRecommendedVersions(
        IReadOnlyDictionary<string, string> versions,
        List<string> failures)
    {
        foreach (var (packageId, version) in versions)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                failures.Add("RecommendedVersions contains an empty package ID.");
            }

            if (string.IsNullOrWhiteSpace(version) || !SemanticVersionPattern().IsMatch(version))
            {
                failures.Add(
                    $"Recommended version '{version}' for package '{packageId}' is not a valid semantic version.");
            }
        }
    }

    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SemanticVersionPattern();
}
