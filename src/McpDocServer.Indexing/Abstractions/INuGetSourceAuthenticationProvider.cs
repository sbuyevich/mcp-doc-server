namespace McpDocServer.Indexing.Abstractions;

public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
