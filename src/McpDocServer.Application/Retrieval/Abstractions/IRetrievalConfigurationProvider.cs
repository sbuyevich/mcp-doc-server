using McpDocServer.Application.Retrieval.Models;

namespace McpDocServer.Application.Retrieval.Abstractions;

public interface IRetrievalConfigurationProvider
{
    RetrievalSettings GetSettings();
}
