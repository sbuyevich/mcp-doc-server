using McpDocServer.Application.Retrieval.Services;

namespace McpDocServer.UnitTests.Retrieval;

public sealed class LibraryIdTests
{
    [Theory]
    [InlineData("nuget:Company.Package", "Company.Package")]
    [InlineData("NUGET:company.package", "company.package")]
    public void ParsesNuGetLibraryId(string value, string expected)
    {
        Assert.True(LibraryId.TryParse(value, out var libraryId));
        Assert.Equal(expected, libraryId.PackageId);
        Assert.StartsWith("nuget:", libraryId.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Company.Package")]
    [InlineData("nuget:")]
    public void RejectsInvalidLibraryId(string value)
    {
        Assert.False(LibraryId.TryParse(value, out _));
    }
}
