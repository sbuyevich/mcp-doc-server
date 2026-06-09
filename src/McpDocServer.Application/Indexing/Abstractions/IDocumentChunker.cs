using McpDocServer.Domain.Indexing;

namespace McpDocServer.Application.Indexing.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
