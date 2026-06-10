using McpDocServer.Indexer.Models;

namespace McpDocServer.Infrastructure.Indexing.Abstractions;

internal interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
