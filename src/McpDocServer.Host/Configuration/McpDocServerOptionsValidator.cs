using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace McpDocServer.Host.Configuration;

/// <summary>
/// Validates server configuration before the MCP transport starts.
/// </summary>
public sealed partial class McpDocServerOptionsValidator : IValidateOptions<McpDocServerOptions>
{
    public ValidateOptionsResult Validate(string? name, McpDocServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateTransport(options, failures);
        ValidatePath(options.DatabasePath, "McpDocServer:DatabasePath", failures);
        ValidateLimits(options.Indexing, failures);
        ValidateRetrieval(options.Retrieval, failures);
        ValidateSourceNames(options, failures);
        ValidateNuGetSources(options.NuGetSources, failures);
        ValidateRepositorySources(options.RepositorySources, failures);
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

    private static void ValidateLimits(IndexingOptions options, List<string> failures)
    {
        if (options.RefreshInterval <= TimeSpan.Zero)
        {
            failures.Add("McpDocServer:Indexing:RefreshInterval must be positive.");
        }

        if (options.MaxPackageBytes <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxPackageBytes must be positive.");
        }

        if (options.MaxDocumentBytes <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxDocumentBytes must be positive.");
        }

        if (options.MaxArchiveEntries <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxArchiveEntries must be positive.");
        }

        if (options.MaxExtractedBytes <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxExtractedBytes must be positive.");
        }

        if (!double.IsFinite(options.MaxCompressionRatio) || options.MaxCompressionRatio <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxCompressionRatio must be positive.");
        }

        if (options.MaxDocumentChars <= 0)
        {
            failures.Add("McpDocServer:Indexing:MaxDocumentChars must be positive.");
        }

        if (options.DefaultMaxResults <= 0)
        {
            failures.Add("McpDocServer:Indexing:DefaultMaxResults must be positive.");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add("McpDocServer:Indexing:RequestTimeout must be positive.");
        }

        if (options.PackageDownloadTimeout <= TimeSpan.Zero)
        {
            failures.Add("McpDocServer:Indexing:PackageDownloadTimeout must be positive.");
        }
    }

    private static void ValidateSourceNames(McpDocServerOptions options, List<string> failures)
    {
        var names = options.NuGetSources.Select(source => source.Name)
            .Concat(options.RepositorySources.Select(source => source.Name))
            .ToList();

        foreach (var name in names.Where(string.IsNullOrWhiteSpace))
        {
            failures.Add("Every configured source must have a non-empty name.");
        }

        var duplicates = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicate in duplicates)
        {
            failures.Add($"Source name '{duplicate}' is configured more than once.");
        }
    }

    private static void ValidateNuGetSources(
        IEnumerable<NuGetSourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            if (!IsNuGetSource(source.ServiceIndex))
            {
                failures.Add(
                    $"NuGet source '{source.Name}' must have an absolute HTTP/HTTPS ServiceIndex URI or a valid local path.");
            }

            if (source.PackagePrefixes.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"NuGet source '{source.Name}' contains an empty package prefix.");
            }

            if (source.PackageIds.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"NuGet source '{source.Name}' contains an empty package ID.");
            }

            if (source.PackagePrefixes.Count == 0 && source.PackageIds.Count == 0)
            {
                failures.Add(
                    $"NuGet source '{source.Name}' must configure at least one package prefix or package ID.");
            }

            if (source.MaxVersionsPerPackage <= 0)
            {
                failures.Add(
                    $"NuGet source '{source.Name}' MaxVersionsPerPackage must be positive.");
            }

            if (source.MaxPackages <= 0)
            {
                failures.Add($"NuGet source '{source.Name}' MaxPackages must be positive.");
            }
        }
    }

    private static bool IsNuGetSource(string value)
    {
        if (TryGetHttpUri(value, out _))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            _ = Path.GetFullPath(value);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            return false;
        }
    }

    private static void ValidateRepositorySources(
        IEnumerable<RepositorySourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            ValidatePath(source.RootPath, $"Repository source '{source.Name}' root path", failures);
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

    private static bool TryGetHttpUri(string value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri)
            && uri.Scheme is "http" or "https")
        {
            return true;
        }

        uri = null;
        return false;
    }

    private static void ValidatePath(string value, string fieldName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{fieldName} must not be empty.");
            return;
        }

        try
        {
            _ = Path.GetFullPath(value);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            failures.Add($"{fieldName} is not a valid path: {exception.Message}");
        }
    }

    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SemanticVersionPattern();
}
