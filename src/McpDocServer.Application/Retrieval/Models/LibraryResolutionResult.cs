namespace McpDocServer.Application.Retrieval.Models;

public sealed record LibraryResolutionResult(
    LibraryResolutionStatus Status,
    ResolvedLibrarySelection? Selection = null);
