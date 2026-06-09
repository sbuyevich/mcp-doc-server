using McpDocServer.Configuration;
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

        AssertFailure(result, "Transport");
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

        AssertFailure(result, "Http:Url");
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

        AssertFailure(result, "Http:Path");
    }

    [Fact]
    public void EmptyDatabasePathFails()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions { DatabasePath = " " });

        AssertFailure(result, "DatabasePath");
    }

    [Fact]
    public void InvalidRetrievalValuesFail()
    {
        var result = _validator.Validate(
            null,
            new McpDocServerOptions
            {
                Retrieval = new RetrievalOptions
                {
                    SourceOrder = ["nuget.org", "NuGet.org"],
                    DefaultMaxResults = 0
                }
            });

        AssertFailure(result, "SourceOrder");
        AssertFailure(result, "DefaultMaxResults");
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

        AssertFailure(result, "valid semantic version");
    }

    private static void AssertFailure(
        ValidateOptionsResult result,
        string expectedText)
    {
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains(expectedText, StringComparison.Ordinal));
    }
}
