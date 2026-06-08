using McpDocServer.Host.Configuration;
using Microsoft.Extensions.Options;

namespace McpDocServer.UnitTests.Configuration;

public sealed class McpDocServerOptionsValidatorTests
{
    private readonly McpDocServerOptionsValidator _validator = new();

    [Fact]
    public void DefaultOptionsAreValid()
    {
        var result = _validator.Validate(null, new McpDocServerOptions());

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void EmptyDatabasePathFails()
    {
        var result = _validator.Validate(null, new McpDocServerOptions { DatabasePath = " " });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("DatabasePath", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateSourceNamesFail()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        ServiceIndex = "https://packages.example/v3/index.json"
                    }
                ],
                OpenApiSources =
                [
                    new OpenApiSourceOptions
                    {
                        Name = "Internal",
                        Location = "https://apis.example/openapi.json"
                    }
                ]
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("configured more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidSourceValuesFail()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        ServiceIndex = "ftp://packages.example/index.json",
                        PackagePrefixes = ["Company.", ""]
                    }
                ],
                Indexing = new IndexingOptions
                {
                    RefreshInterval = TimeSpan.Zero,
                    MaxPackageBytes = 0,
                    MaxDocumentBytes = 1,
                    DefaultMaxResults = 10,
                    RequestTimeout = TimeSpan.FromSeconds(30)
                }
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("ServiceIndex URI", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("empty package prefix", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("RefreshInterval", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MaxPackageBytes", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidRecommendedVersionFails()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                RecommendedVersions = new Dictionary<string, string>
                {
                    ["Company.Customer"] = "four"
                }
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("valid semantic version", StringComparison.Ordinal));
    }
}
