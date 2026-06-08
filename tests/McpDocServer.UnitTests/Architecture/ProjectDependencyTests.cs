using System.Xml.Linq;

namespace McpDocServer.UnitTests.Architecture;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void DomainHasNoProjectReferences()
    {
        var document = XDocument.Load(ProjectPath("src", "McpDocServer.Domain", "McpDocServer.Domain.csproj"));

        Assert.Empty(ProjectReferences(document));
    }

    [Fact]
    public void ApplicationReferencesOnlyDomain()
    {
        var document = XDocument.Load(ProjectPath("src", "McpDocServer.Application", "McpDocServer.Application.csproj"));

        var references = ProjectReferences(document);

        var reference = Assert.Single(references);
        Assert.Equal(
            @"..\McpDocServer.Domain\McpDocServer.Domain.csproj",
            reference.Replace('/', '\\'));
    }

    private static IReadOnlyList<string> ProjectReferences(XDocument document)
    {
        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static string ProjectPath(params string[] parts)
    {
        var root = FindRepositoryRoot();
        return Path.Combine([root, .. parts]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpDocServer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

}
