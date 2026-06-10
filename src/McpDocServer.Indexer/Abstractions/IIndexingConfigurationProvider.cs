using McpDocServer.Indexer.Models;

namespace McpDocServer.Indexer.Abstractions;

public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
