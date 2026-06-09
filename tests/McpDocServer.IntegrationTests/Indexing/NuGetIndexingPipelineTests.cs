using McpDocServer.Indexing.Services;
using McpDocServer.Indexing.Worker;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpDocServer.IntegrationTests.Indexing;

public sealed class NuGetIndexingPipelineTests
{
    [Fact]
    public async Task LocalPackageIsIndexedIntoSqliteAndFtsIdempotently()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-doc-server-tests-{Guid.NewGuid():N}");
        var feed = Path.Combine(root, "feed");
        var databasePath = Path.Combine(root, "index", "docs.db");
        FixtureNuGetPackage.Create(feed);

        try
        {
            using var provider = CreateProvider(feed, databasePath);
            var coordinator = provider.GetRequiredService<IIndexCoordinator>();

            var first = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));

            Assert.Equal("succeeded", first.Status);
            Assert.Equal(1, first.Discovered);
            Assert.Equal(1, first.Indexed);
            Assert.Equal(1, first.Changed);
            Assert.Equal(0, first.Unchanged);

            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Pooling=False");
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM dependencies;") > 0);
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM target_frameworks;") > 0);
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM symbols;") > 0);
            Assert.True(await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM document_chunks_fts WHERE document_chunks_fts MATCH 'fixture';") > 0);

            var second = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));
            Assert.Equal(0, second.Changed);
            Assert.Equal(1, second.Unchanged);
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));

            FixtureNuGetPackage.ReplaceWithUnsafeArchive(feed);
            var failed = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));

            Assert.Equal("failed", failed.Status);
            Assert.Single(failed.Errors);
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));
            Assert.True(await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM document_chunks_fts WHERE document_chunks_fts MATCH 'fixture';") > 0);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(string feed, string databasePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpDocServer:DatabasePath"] = databasePath,
                ["McpDocServer:NuGetSources:0:Name"] = "fixture",
                ["McpDocServer:NuGetSources:0:ServiceIndex"] = feed,
                ["McpDocServer:NuGetSources:0:PackageIds:0"] = FixtureNuGetPackage.PackageId,
                ["McpDocServer:NuGetSources:0:MaxVersionsPerPackage"] = "1",
                ["McpDocServer:NuGetSources:0:MaxPackages"] = "10",
                ["McpDocServer:Indexing:MaxCompressionRatio"] = "10000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIndexingWorkerCore(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
