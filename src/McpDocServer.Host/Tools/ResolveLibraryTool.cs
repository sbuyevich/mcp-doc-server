using System.ComponentModel;
using McpDocServer.Application.Abstractions;
using McpDocServer.Application.Contracts.ResolveLibrary;
using ModelContextProtocol.Server;

namespace McpDocServer.Host.Tools;

[McpServerToolType]
public sealed class ResolveLibraryTool(IResolveLibraryHandler handler)
{
    [McpServerTool(
        Name = "resolve_library",
        UseStructuredContent = true,
        OutputSchemaType = typeof(ResolveLibraryResponse))]
    [Description("Finds the best internal NuGet package or generated client for a name or concept.")]
    public Task<ResolveLibraryResponse> ResolveLibraryAsync(
        [Description("Package name, client name, or implementation concept to resolve.")] string query,
        [Description("Whether prerelease package versions may be considered.")] bool includePrerelease = false,
        [Description("Maximum number of library matches to return.")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return handler.HandleAsync(
            new ResolveLibraryRequest(query, includePrerelease, limit),
            cancellationToken);
    }
}
