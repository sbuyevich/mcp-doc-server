namespace McpDocServer.Application.Contracts.ResolveLibrary;

/// <summary>
/// Request to resolve an internal package or generated client from a name or concept.
/// </summary>
public sealed record ResolveLibraryRequest(string Query, bool IncludePrerelease = false, int Limit = 10);
