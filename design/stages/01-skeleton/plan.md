# Stage 1 Implementation Plan: Skeleton

## Objective

Create the .NET solution, stdio MCP host, four NuGet documentation tool
contracts, configuration validation, startup diagnostics, and test foundation.
This stage establishes boundaries without contacting feeds or creating the
search index.

## Technical Decisions

- Target .NET 10 with nullable reference types, analyzers, and warnings as
  errors.
- Use the official Model Context Protocol C# SDK.
- Reserve standard output for MCP protocol traffic and send logs to standard
  error.
- Use explicit tool registration so the public MCP surface is reviewed.
- Use `System.Text.Json` contracts with stable property names.
- Keep dependencies directed from Host to Application and Infrastructure, with
  Domain independent of both.

## Project Structure

```text
src/
  McpDocServer.Domain/
  McpDocServer.Application/
  McpDocServer.Infrastructure/
  McpDocServer.Host/
tests/
  McpDocServer.UnitTests/
  McpDocServer.IntegrationTests/
```

## Configuration

Define:

- `McpDocServerOptions`
- `NuGetSourceOptions`
- `RepositorySourceOptions`
- `IndexingOptions`
- `RetrievalOptions`

Validate database paths, unique source names, NuGet service indexes, package
selection rules, repository roots, semantic recommended versions, and positive
limits.

## MCP Contracts

Expose exactly:

- `resolve_library`
- `query_docs`
- `get_symbol`
- `list_versions`

Common responses carry status, data, resolved context, evidence, citations,
warnings, and errors. Request handlers accept cancellation tokens.

## Startup And Registration

1. Bind and validate configuration during host startup.
2. Register Application and Infrastructure services through extension methods.
3. Register all four tool classes explicitly.
4. Configure stdio transport.
5. Resolve required handlers during startup diagnostics.
6. Validate local database path readiness without opening remote sources.
7. Log the tool count, local-check count, NuGet source count, and database path.

## Tests

- Architecture tests enforce project dependency direction.
- Configuration tests cover valid defaults, invalid paths and limits, duplicate
  source names, invalid feed locations, missing selection rules, and invalid
  semantic versions.
- Contract tests verify stable JSON shapes and round trips.
- MCP integration tests verify exactly four discoverable tools with input and
  output schemas.
- Stdio tests verify child-process connection and standard-error logging.
- Startup tests verify clean startup and clear validation failures.

## Completion Criteria

- `dotnet build` and `dotnet test` pass.
- Exactly four tools are discoverable over stdio.
- Startup diagnostics validate registration and local dependencies.
- No NuGet or repository source is contacted by skeleton diagnostics.
- README instructions launch the host without corrupting protocol output.
