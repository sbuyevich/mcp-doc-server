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

    [Theory]
    [InlineData("stdio")]
    [InlineData("http")]
    public void SupportedTransportIsValid(string transport)
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions { Transport = transport });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void UnsupportedTransportFails()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions { Transport = "websocket" });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("Transport", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://127.0.0.1:5034")]
    [InlineData("http://0.0.0.0:5034")]
    [InlineData("http://example.com:5034")]
    [InlineData("not-a-url")]
    public void UnsafeHttpUrlFails(string url)
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                Http = new HttpHostOptions { Url = url }
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("Http:Url", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("mcp")]
    [InlineData("/mcp?mode=test")]
    [InlineData("/mcp#fragment")]
    public void InvalidHttpPathFails(string path)
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                Http = new HttpHostOptions { Path = path }
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("Http:Path", StringComparison.Ordinal));
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
                        ServiceIndex = "https://packages.example/v3/index.json",
                        PackageIds = ["Company.Package"]
                    }
                ],
                RepositorySources =
                [
                    new RepositorySourceOptions
                    {
                        Name = "Internal",
                        RootPath = "repositories/internal"
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
                        PackagePrefixes = ["Company.", ""],
                        MaxVersionsPerPackage = 0
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
        Assert.Contains(result.Failures, failure => failure.Contains("MaxVersionsPerPackage", StringComparison.Ordinal));
    }

    [Fact]
    public void NuGetSourceRequiresASelectionRule()
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
                ]
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("package prefix or package ID", StringComparison.Ordinal));
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
