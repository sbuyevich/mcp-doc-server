using McpDocServer.Application.Contracts.Common;
using McpDocServer.Application.Contracts.ListVersions;
using McpDocServer.Application.Contracts.QueryDocs;
using McpDocServer.Application.Contracts.ResolveLibrary;
using McpDocServer.Application.Retrieval.Services;
using McpDocServer.Host;
using McpDocServer.Indexer.Cli;
using McpDocServer.Indexer.Services;
using McpDocServer.IntegrationTests.Indexing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.IntegrationTests.Retrieval;

public sealed class EnvironmentAwareRetrievalTests
{
    [Fact]
    public async Task SamePackageCanBeSelectedByEnvironmentVersionAndSourceOrder()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"mcp-doc-environment-retrieval-{Guid.NewGuid():N}");
        var qaPrimary = Path.Combine(root, "qa-primary");
        var qaSecondary = Path.Combine(root, "qa-secondary");
        var production = Path.Combine(root, "production");
        var databasePath = Path.Combine(root, "index", "docs.db");

        FixtureNuGetPackage.Create(
            qaPrimary,
            "2.0.0",
            "# QA primary\n\nQA primary version 2.0.0.");
        FixtureNuGetPackage.Create(
            qaSecondary,
            "2.0.0",
            "# QA secondary\n\nQA secondary version 2.0.0.");
        FixtureNuGetPackage.Create(
            qaSecondary,
            "2.1.0",
            "# QA secondary\n\nQA recommended version 2.1.0.");
        FixtureNuGetPackage.Create(
            production,
            "1.0.0",
            "# Production\n\nProduction version 1.0.0.");

        try
        {
            using var provider = CreateProvider(
                qaPrimary,
                qaSecondary,
                production,
                databasePath);
            var summaries = await provider.GetRequiredService<IIndexCoordinator>()
                .IndexAllAsync(CancellationToken.None);
            Assert.Equal(3, summaries.Count);
            Assert.All(summaries, summary => Assert.Equal("succeeded", summary.Status));

            var resolver = provider.GetRequiredService<IResolveLibraryHandler>();
            var all = await resolver.HandleAsync(
                new ResolveLibraryRequest(FixtureNuGetPackage.PackageId),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, all.Status);
            Assert.Equal(2, all.Data!.Matches.Count);

            var qaMatch = Assert.Single(all.Data.Matches, match =>
                match.Environment.Equals("qa", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(
                $"nuget:qa/{FixtureNuGetPackage.PackageId}",
                qaMatch.LibraryId);
            Assert.Equal("qa-secondary", qaMatch.SourceId);
            Assert.Equal("2.1.0", qaMatch.RecommendedVersion);

            var productionMatch = Assert.Single(all.Data.Matches, match =>
                match.Environment.Equals("production", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("production-feed", productionMatch.SourceId);
            Assert.Equal("1.0.0", productionMatch.RecommendedVersion);

            var filtered = await resolver.HandleAsync(
                new ResolveLibraryRequest(
                    FixtureNuGetPackage.PackageId,
                    Environment: "QA"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, filtered.Status);
            Assert.Equal("qa", Assert.Single(filtered.Data!.Matches).Environment);

            var versionsHandler = provider.GetRequiredService<IListVersionsHandler>();
            var legacy = await versionsHandler.HandleAsync(
                new ListVersionsRequest($"nuget:{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal("production", legacy.ResolvedContext!.Environment);
            Assert.Equal("production-feed", legacy.ResolvedContext.SourceId);

            var qaVersions = await versionsHandler.HandleAsync(
                new ListVersionsRequest($"nuget:qa/{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal("qa", qaVersions.ResolvedContext!.Environment);
            Assert.Equal("qa-secondary", qaVersions.ResolvedContext.SourceId);
            Assert.Equal("2.1.0", qaVersions.Data!.RecommendedVersion);

            var docsHandler = provider.GetRequiredService<IQueryDocsHandler>();
            var qaPrimaryDocs = await docsHandler.HandleAsync(
                new QueryDocsRequest(
                    $"nuget:qa/{FixtureNuGetPackage.PackageId}",
                    "QA primary",
                    Version: "2.0.0"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, qaPrimaryDocs.Status);
            Assert.Equal("qa-primary", qaPrimaryDocs.ResolvedContext!.SourceId);
            Assert.Contains(qaPrimaryDocs.Evidence, item =>
                item.Text.Contains("QA primary version", StringComparison.Ordinal));
            Assert.DoesNotContain(qaPrimaryDocs.Evidence, item =>
                item.Text.Contains("QA secondary version", StringComparison.Ordinal));

            var isolated = await docsHandler.HandleAsync(
                new QueryDocsRequest(
                    $"nuget:qa/{FixtureNuGetPackage.PackageId}",
                    "Production",
                    Version: "1.0.0"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.NotFound, isolated.Status);
            Assert.Equal("version_not_found", Assert.Single(isolated.Errors).Code);

            var legacyExact = await docsHandler.HandleAsync(
                new QueryDocsRequest(
                    $"nuget:{FixtureNuGetPackage.PackageId}",
                    "QA primary",
                    Version: "2.0.0"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, legacyExact.Status);
            Assert.Equal("qa", legacyExact.ResolvedContext!.Environment);
            Assert.Equal("qa-primary", legacyExact.ResolvedContext.SourceId);

            var missingEnvironment = await versionsHandler.HandleAsync(
                new ListVersionsRequest(
                    $"nuget:staging/{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.NotFound, missingEnvironment.Status);
            Assert.Equal(
                "environment_not_found",
                Assert.Single(missingEnvironment.Errors).Code);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(
        string qaPrimary,
        string qaSecondary,
        string production,
        string databasePath)
    {
        var values = new Dictionary<string, string?>
        {
            ["McpDocServer:DatabasePath"] = databasePath,
            ["McpDocServer:Retrieval:EnvironmentOrder:0"] = "production",
            ["McpDocServer:Retrieval:EnvironmentOrder:1"] = "qa",
            ["McpDocServer:Retrieval:SourceOrder:0"] = "qa-primary",
            ["McpDocServer:Retrieval:SourceOrder:1"] = "qa-secondary",
            ["McpDocServer:Retrieval:SourceOrder:2"] = "production-feed",
            [$"McpDocServer:RecommendedVersions:{FixtureNuGetPackage.PackageId}"] = "1.0.0",
            [$"McpDocServer:RecommendedVersions:nuget:qa/{FixtureNuGetPackage.PackageId}"] = "2.1.0",
            ["McpDocServer:Indexing:MaxCompressionRatio"] = "10000"
        };
        AddSource(values, 0, "qa-primary", "qa", qaPrimary);
        AddSource(values, 1, "qa-secondary", "qa", qaSecondary);
        AddSource(values, 2, "production-feed", "production", production);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMcpDocServerCore(configuration);
        services.AddIndexerCli(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static void AddSource(
        IDictionary<string, string?> values,
        int index,
        string name,
        string environment,
        string serviceIndex)
    {
        var prefix = $"McpDocServer:NuGetSources:{index}";
        values[$"{prefix}:Name"] = name;
        values[$"{prefix}:Environment"] = environment;
        values[$"{prefix}:ServiceIndex"] = serviceIndex;
        values[$"{prefix}:PackageIds:0"] = FixtureNuGetPackage.PackageId;
        values[$"{prefix}:MaxVersionsPerPackage"] = "10";
        values[$"{prefix}:MaxPackages"] = "10";
    }
}
