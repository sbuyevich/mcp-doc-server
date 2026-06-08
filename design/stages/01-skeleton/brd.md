# Stage 1 BRD: Skeleton

## Purpose

Establish a buildable .NET solution and a working MCP server shell that defines
the boundaries and contracts used by every later stage.

## Business Outcome

A developer can run the server locally, connect an MCP client over stdio, and
discover the planned tools even though their backing search capabilities are
not implemented yet.

## Dependencies

None.

## In Scope

- Create the .NET 10 solution and project structure.
- Configure the official Model Context Protocol C# SDK.
- Host the server over stdio.
- Register application services through dependency injection.
- Define configuration objects and startup validation.
- Define MCP tool input and output contracts.
- Expose placeholder tool handlers with explicit `not_ready` responses.
- Add health diagnostics suitable for local startup checks.
- Establish unit and integration test projects.

## Functional Requirements

### FR-1: Solution structure

The solution must contain:

```text
src/
  McpDocServer.Host/
  McpDocServer.Application/
  McpDocServer.Domain/
  McpDocServer.Infrastructure/
tests/
  McpDocServer.UnitTests/
  McpDocServer.IntegrationTests/
```

Project references must follow the ownership boundaries in the main
specification. Domain code must not depend on host or infrastructure code.

### FR-2: MCP hosting

The host must:

- Start over stdio.
- Write logs to standard error.
- Register tools through the MCP SDK and dependency injection.
- Shut down cleanly when cancellation is requested.

### FR-3: Tool contracts

Define stable request and response models for:

- `resolve_library`
- `query_docs`
- `get_symbol`
- `find_api_operation`
- `list_versions`

Each response must support:

- A machine-readable status.
- Resolved source and version information where applicable.
- Evidence and citations.
- Warnings and errors.

Until later stages implement behavior, handlers must return `not_ready` rather
than fabricated data.

### FR-4: Configuration

Define and validate configuration for:

- Database location.
- NuGet sources.
- OpenAPI sources.
- Repository sources.
- Recommended package versions.
- Indexing limits and refresh settings.

Invalid required configuration must fail startup with a useful error.
Credentials must not appear in configuration models intended for tool output.

### FR-5: Diagnostics

The host must provide enough diagnostics to confirm:

- The process started.
- Configuration is valid.
- Tool registration succeeded.
- Required local dependencies are available.

## Non-Functional Requirements

- Nullable reference types and analyzers are enabled.
- All asynchronous operations accept cancellation.
- Tool contracts are serializable with `System.Text.Json`.
- Build output must be free of warnings introduced by the stage.
- No package or OpenAPI source is contacted during normal startup.

## Deliverables

- Buildable solution and projects.
- MCP stdio host.
- Five discoverable tool definitions.
- Configuration and validation models.
- Initial test infrastructure.
- Local run instructions in the repository README.

## Acceptance Criteria

1. `dotnet build` succeeds.
2. `dotnet test` succeeds.
3. An MCP client can start the process over stdio.
4. The client discovers all five tool names and their schemas.
5. Calling any placeholder tool returns a structured `not_ready` response.
6. Logs are emitted to standard error and do not corrupt MCP messages.
7. Invalid configuration causes a clear startup validation failure.

## Out of Scope

- NuGet feed access.
- Database schema and indexing.
- Real search results.
- OpenAPI parsing.
- HTTP transport and authentication.

## Exit Gate

Stage 2 may begin when the solution builds, tests pass, and MCP tool discovery
works reliably over stdio.
