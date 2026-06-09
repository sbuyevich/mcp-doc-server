using System.ComponentModel;
using System.Text.Json;
using McpDocServer.Application.Retrieval.Services;
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
    [Description("Finds the best indexed NuGet package for a name or concept.")]
    public Task<ResolveLibraryResponse> ResolveLibraryAsync(
        [Description("Package name, client name, or implementation concept to resolve.")] string query,
        [Description("Whether prerelease package versions may be considered.")] bool includePrerelease = false,
        [Description("Maximum number of library matches to return.")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(query, includePrerelease, limit);

        return handler.HandleAsync(
            request,
            cancellationToken);
    }

    private static ResolveLibraryRequest CreateRequest(
        string query,
        bool includePrerelease,
        int limit)
    {
        try
        {
            using var document = JsonDocument.Parse(query);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("query", out var nestedQuery)
                || nestedQuery.ValueKind != JsonValueKind.String)
            {
                return new ResolveLibraryRequest(query, includePrerelease, limit);
            }

            query = nestedQuery.GetString()!;
            if (root.TryGetProperty("includePrerelease", out var nestedPrerelease)
                && nestedPrerelease.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                includePrerelease = nestedPrerelease.GetBoolean();
            }

            if (root.TryGetProperty("limit", out var nestedLimit)
                && nestedLimit.TryGetInt32(out var parsedLimit))
            {
                limit = parsedLimit;
            }
        }
        catch (JsonException)
        {
            // A normal search query does not need to be valid JSON.
        }

        return new ResolveLibraryRequest(query, includePrerelease, limit);
    }
}
