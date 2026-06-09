using McpDocServer.Configuration;
using Microsoft.Extensions.Options;

namespace McpDocServer.UnitTests.Configuration;

public sealed class IndexingWorkerOptionsValidatorTests
{
    private readonly IndexingWorkerOptionsValidator _validator = new();

    [Fact]
    public void DefaultOptionsAreValid()
    {
        var result = _validator.Validate(null, new IndexingWorkerOptions());

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void DuplicateSourceNamesFail()
    {
        var result = _validator.Validate(
            null,
            new IndexingWorkerOptions
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

        AssertFailure(result, "configured more than once");
    }

    [Fact]
    public void InvalidSourceAndLimitValuesFail()
    {
        var result = _validator.Validate(
            null,
            new IndexingWorkerOptions
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
                    MaxDocumentBytes = 1
                }
            });

        AssertFailure(result, "ServiceIndex URI");
        AssertFailure(result, "empty package prefix");
        AssertFailure(result, "RefreshInterval");
        AssertFailure(result, "MaxPackageBytes");
        AssertFailure(result, "MaxVersionsPerPackage");
    }

    [Fact]
    public void NuGetSourceRequiresASelectionRule()
    {
        var result = _validator.Validate(
            null,
            new IndexingWorkerOptions
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

        AssertFailure(result, "package prefix or package ID");
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
