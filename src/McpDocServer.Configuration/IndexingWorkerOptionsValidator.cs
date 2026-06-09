using Microsoft.Extensions.Options;

namespace McpDocServer.Configuration;

/// <summary>
/// Validates indexing worker configuration before the first refresh.
/// </summary>
public sealed class IndexingWorkerOptionsValidator :
    IValidateOptions<IndexingWorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, IndexingWorkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ConfigurationValidation.ValidatePath(
            options.DatabasePath,
            "McpDocServer:DatabasePath",
            failures);
        ValidateLimits(options.Indexing, failures);
        ValidateSourceNames(options, failures);
        ValidateNuGetSources(options.NuGetSources, failures);
        ValidateRepositorySources(options.RepositorySources, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
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

        if (options.PackageDownloadTimeout <= TimeSpan.Zero)
        {
            failures.Add("McpDocServer:Indexing:PackageDownloadTimeout must be positive.");
        }
    }

    private static void ValidateSourceNames(
        IndexingWorkerOptions options,
        List<string> failures)
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
            if (!ConfigurationValidation.IsNuGetSource(source.ServiceIndex))
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

    private static void ValidateRepositorySources(
        IEnumerable<RepositorySourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            ConfigurationValidation.ValidatePath(
                source.RootPath,
                $"Repository source '{source.Name}' root path",
                failures);
        }
    }
}
