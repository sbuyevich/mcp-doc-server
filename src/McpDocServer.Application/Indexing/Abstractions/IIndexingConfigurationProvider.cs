using McpDocServer.Application.Indexing.Models;

namespace McpDocServer.Application.Indexing.Abstractions;

public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
