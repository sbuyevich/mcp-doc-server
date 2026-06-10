using McpDocServer.Indexer.Models;

namespace McpDocServer.Indexer.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
