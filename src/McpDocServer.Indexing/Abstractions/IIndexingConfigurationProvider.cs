using McpDocServer.Indexing.Models;

namespace McpDocServer.Indexing.Abstractions;

public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
