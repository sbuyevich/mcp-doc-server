using System.Security.Cryptography;
using McpDocServer.Indexing.Abstractions;

namespace McpDocServer.Infrastructure.Indexing;

internal sealed class Sha256ContentHasher : IContentHasher
{
    public string Hash(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    public async Task<string> HashAsync(Stream stream, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
