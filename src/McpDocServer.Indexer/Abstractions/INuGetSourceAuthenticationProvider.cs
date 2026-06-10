namespace McpDocServer.Indexer.Abstractions;

public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
