using System.Xml.Linq;

namespace McpDocServer.UnitTests.Architecture;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void IndexerHasNoProjectReferences()
    {
        var document = LoadProject("McpDocServer.Indexer");

        Assert.Empty(ProjectReferences(document));
    }

    [Fact]
    public void IndexerCliReferencesIndexerAndInfrastructure()
    {
        AssertProjectReferences(
            "McpDocServer.Indexer.Cli",
            "McpDocServer.Indexer",
            "McpDocServer.Infrastructure");
    }

    [Fact]
    public void ApplicationHasNoProjectReferences()
    {
        var document = LoadProject("McpDocServer.Application");

        Assert.Empty(ProjectReferences(document));
    }

    [Fact]
    public void ConfigurationHasNoProjectReferences()
    {
        var document = LoadProject("McpDocServer.Configuration");

        Assert.Empty(ProjectReferences(document));
    }

    [Fact]
    public void InfrastructureReferencesApplicationAndIndexer()
    {
        AssertProjectReferences(
            "McpDocServer.Infrastructure",
            "McpDocServer.Application",
            "McpDocServer.Indexer");
    }

    [Fact]
    public void HostReferencesApplicationConfigurationAndInfrastructure()
    {
        AssertProjectReferences(
            "McpDocServer.Host",
            "McpDocServer.Application",
            "McpDocServer.Configuration",
            "McpDocServer.Infrastructure");
    }

    [Fact]
    public void OldIndexingProjectsAreAbsent()
    {
        var oldFeatureProject = "McpDocServer." + "Indexing";
        var oldWorkerProject = oldFeatureProject + ".Worker";

        Assert.False(File.Exists(ProjectPath(
            "src",
            oldFeatureProject,
            $"{oldFeatureProject}.csproj")));
        Assert.False(File.Exists(ProjectPath(
            "src",
            oldWorkerProject,
            $"{oldWorkerProject}.csproj")));
    }

    private static void AssertProjectReferences(
        string projectName,
        params string[] expectedProjectNames)
    {
        var actual = ProjectReferences(LoadProject(projectName))
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = expectedProjectNames
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static XDocument LoadProject(string projectName)
    {
        return XDocument.Load(ProjectPath("src", projectName, $"{projectName}.csproj"));
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
