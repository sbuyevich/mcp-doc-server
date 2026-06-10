using McpDocServer.Indexing.Models;

namespace McpDocServer.Indexing.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
