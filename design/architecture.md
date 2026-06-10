# Solution Architecture

## Overview

McpDocServer separates package index production from MCP retrieval:

```mermaid
flowchart TD
    Client[MCP client]
    Host[McpDocServer.Host]
    Configuration[McpDocServer.Configuration]
    Application[McpDocServer.Application]
    Infrastructure[McpDocServer.Infrastructure]
    Indexer[McpDocServer.Indexer]
    NuGet[NuGet source]
    Database[(SQLite and FTS5)]

    Client -->|stdio or Streamable HTTP| Host
    Host --> Configuration
    Host --> Application
    Host --> Infrastructure
    Infrastructure --> Application
    Infrastructure --> Database
    Indexer --> NuGet
    Indexer --> Database
```

The Host is retrieval-only. The Indexer is a separate one-shot console
application and the sole index writer.

## Projects

### McpDocServer.Configuration

Contains Host transport and retrieval option contracts and validation. It has
no project references.

### McpDocServer.Application

Contains MCP wire contracts, retrieval models and abstractions, and retrieval
services. It has no project references and does not depend on SQLite, NuGet, or
MCP transport implementations.

### McpDocServer.Infrastructure

Implements Application retrieval abstractions:

- Read-only SQLite access for packages, versions, documents, and symbols.
- FTS5 query construction and search.
- Local dependency diagnostics.

Infrastructure references only Application. It contains no index writer,
NuGet download, archive processing, or MCP transport behavior.

### McpDocServer.Host

Is the MCP executable and retrieval composition root:

- Loads and validates Host configuration.
- Registers Application and retrieval Infrastructure services.
- Selects stdio or stateless Streamable HTTP.
- Exposes four MCP tools and NuGet resource templates.
- Opens the SQLite index read-only.

The Host does not contact NuGet sources or register Indexer services.

### McpDocServer.Indexer

Is a self-contained console executable containing:

- Indexer configuration and validation.
- Source-neutral models, abstractions, and indexing orchestration.
- NuGet discovery, metadata lookup, and bounded package download.
- Safe archive inspection and metadata-only symbol extraction.
- Document chunking and SHA-256 hashing.
- SQLite schema migration, atomic publication, FTS5 writes, and run history.

Each invocation indexes every configured source once and exits:

- Exit `0` for success or no configured sources.
- Exit `1` for partial success, failure, invalid configuration, or
  cancellation.

Recurring execution is delegated to an external scheduler. Only one Indexer
process may write to a given SQLite database at a time.

## Dependency Rules

```text
Application    -> no project references
Configuration  -> no project references
Indexer        -> no project references
Infrastructure -> Application
Host           -> Application + Configuration + Infrastructure
Tests          -> projects required by each scenario
```

Architecture tests enforce this graph and verify that the former indexing
projects are absent.

## Indexing Flow

```mermaid
sequenceDiagram
    participant Indexer
    participant Coordinator as IndexCoordinator
    participant Source as IPackageSourceClient
    participant Processor as IPackageProcessor
    participant Store as IIndexStore
    participant DB as SQLite/FTS5

    Indexer->>Coordinator: IndexAllAsync
    Coordinator->>Store: Initialize or migrate database
    Coordinator->>Source: Discover package versions
    loop Each package version
        Coordinator->>Source: Download bounded package
        Coordinator->>Processor: Extract index data
    end
    Coordinator->>Store: Publish source atomically
    Store->>DB: Replace changed rows and update FTS
```

Package content hashes and deterministic IDs make repeated runs idempotent.
Unchanged package content is not rewritten. `index_runs` intentionally records
each execution, while canonical package, version, artifact, and symbol rows are
not duplicated.

## Retrieval Flow

```mermaid
sequenceDiagram
    participant Client as MCP client
    participant Tool as Host MCP tool
    participant Handler as Retrieval handler
    participant Store as INuGetReadStore
    participant DB as SQLite/FTS5

    Client->>Tool: MCP request
    Tool->>Handler: Typed request contract
    Handler->>Store: Version-scoped query
    Store->>DB: Read-only SQL or FTS5 search
    DB-->>Store: Indexed records
    Store-->>Handler: Retrieval models
    Handler-->>Tool: Response with citations
    Tool-->>Client: Structured MCP result
```

Qualified `nuget:{environment}/{packageId}` identifiers never cross
environments. Legacy identifiers select by `EnvironmentOrder` and
`SourceOrder`. All evidence remains isolated to one selected package version.

## Composition

- `Application.AddApplication()` registers retrieval handlers and policies.
- `Infrastructure.AddRetrievalInfrastructure()` registers read-only SQLite
  retrieval implementations.
- `Host.AddMcpDocServerCore()` binds Host configuration and composes retrieval.
- `Indexer.AddIndexer(configuration)` binds Indexer configuration and composes
  the complete indexing pipeline.
- `Host.WithMcpDocServerTools()` publishes tools and resources.

## Testing Strategy

- Unit tests cover configuration, archive safety, chunking, symbol extraction,
  version selection, serialization, and architecture rules.
- Integration tests index local fixture packages into temporary SQLite
  databases and exercise retrieval end to end.
- Child-process tests verify Indexer exit codes and Host stdio/HTTP behavior.
- Idempotency tests run indexing repeatedly and verify canonical rows remain
  unique while run history grows.

## Extension Guidelines

- Keep MCP wire shapes in `Application.Contracts`.
- Add retrieval integrations behind Application abstractions.
- Add indexing behavior within the Indexer vertical boundary.
- Never execute package assemblies.
- Preserve package-version isolation and citation traceability.
