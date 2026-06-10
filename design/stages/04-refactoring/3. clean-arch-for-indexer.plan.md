# Keep Indexer Implementations in Infrastructure

## Summary

Keep `McpDocServer.Indexer` as the inner indexing feature library containing
models, abstractions, and orchestration. Move concrete NuGet, processing,
hashing, and SQLite implementations into Infrastructure.

Create `McpDocServer.Indexer.Cli` as the one-shot console composition root.
This avoids a circular dependency while preserving Clean Architecture
dependency direction.

## Key Changes

### Indexer library

- Change `McpDocServer.Indexer` from an executable to a class library.
- Keep these namespaces in the library:
  - `McpDocServer.Indexer.Models`
  - `McpDocServer.Indexer.Abstractions`
  - `McpDocServer.Indexer.Services`
- Keep `IIndexCoordinator` and `IndexCoordinator` in
  `McpDocServer.Indexer.Services`.
- Keep use-case ports such as `IIndexStore`, `IPackageSourceClient`, and
  `IPackageProcessor` in `McpDocServer.Indexer.Abstractions`.
- Remove hosting, executable configuration, and concrete implementation
  packages from the library.
- Add `AddIndexer()` to register Indexer-owned application services such as
  `IIndexCoordinator`.

### Infrastructure implementations

- Move concrete implementations into:
  - `McpDocServer.Infrastructure.Indexing.NuGet`
  - `McpDocServer.Infrastructure.Indexing.Processing`
  - `McpDocServer.Infrastructure.Indexing.Persistence`
- Infrastructure owns implementation helpers including content hashing,
  document chunking, archive safety, metadata extraction, and NuGet
  authentication.
- Move `Microsoft.Data.Sqlite`, `NuGet.Packaging`, and `NuGet.Protocol`
  package references to Infrastructure.
- Add `AddIndexingInfrastructure()` to register all implementations of
  Indexer-owned ports.
- Keep `AddRetrievalInfrastructure()` separate so Host composition remains
  retrieval-only.

### CLI composition root

- Add `McpDocServer.Indexer.Cli` as the one-shot console executable.
- Move the following from Indexer into the CLI:
  - `Program`
  - `IndexerRunner`
  - Indexer options and validation
  - `OptionsIndexingConfigurationProvider`
  - `appsettings.json`
- The CLI references Indexer and Infrastructure and composes:
  - CLI configuration and runner services
  - `AddIndexer()`
  - `AddIndexingInfrastructure()`
- Preserve the current one-shot behavior:
  - Exit `0` for success or no configured sources.
  - Exit `1` for partial success, failure, invalid configuration,
    cancellation, or an unhandled error.
  - Use an external scheduler for recurring indexing.

Run with:

```powershell
dotnet run --project .\src\McpDocServer.Indexer.Cli\McpDocServer.Indexer.Cli.csproj
```

### Project graph

```text
Application    -> no project references
Configuration  -> no project references
Indexer        -> no project references
Infrastructure -> Application + Indexer
Host           -> Application + Configuration + Infrastructure
Indexer.Cli    -> Indexer + Infrastructure
```

- Add `McpDocServer.Indexer.Cli` to the solution.
- Keep Host free of Indexer references and indexing registrations.
- Update test project references according to the types each test consumes.

## Public Surface

- `McpDocServer.Indexer` changes from an executable to a class library.
- The indexing command moves to `McpDocServer.Indexer.Cli`.
- Concrete namespaces change from:

```text
McpDocServer.Indexer.NuGet
McpDocServer.Indexer.Processing
McpDocServer.Indexer.Persistence
```

to:

```text
McpDocServer.Infrastructure.Indexing.NuGet
McpDocServer.Infrastructure.Indexing.Processing
McpDocServer.Infrastructure.Indexing.Persistence
```

- Indexer models, abstractions, service interfaces, database schema, stable
  IDs, environment selection, and indexing behavior remain unchanged.
- MCP contracts and Host behavior remain unchanged.

## Test Plan

- Update architecture tests to enforce the new project graph and prevent
  circular references.
- Move CLI configuration, runner, and child-process tests to target
  `McpDocServer.Indexer.Cli`.
- Update implementation tests to use Infrastructure indexing namespaces.
- Verify `AddIndexer()` registers orchestration but no concrete adapters.
- Verify `AddIndexingInfrastructure()` resolves every Indexer port.
- Verify Host composition contains no `IIndexCoordinator`, indexing source
  client, package processor, or index writer.
- Preserve indexing pipeline, migration, environment, idempotency, failure,
  and cancellation coverage.
- Update fixture assembly and XML symbol expectations only where the moved
  concrete types affect them; Indexer model names remain stable.
- Update README, architecture, specification, test plans, commands, and
  configuration paths for `McpDocServer.Indexer.Cli`.
- Run stale namespace searches, `git diff --check`,
  `dotnet build .\McpDocServer.slnx`, and
  `dotnet test .\McpDocServer.slnx`.

## Assumptions

- `McpDocServer.Indexer` is the inner feature boundary and does not reference
  Infrastructure or hosting packages.
- `McpDocServer.Indexer.Cli` is the only indexing composition root.
- Indexing configuration remains owned by the CLI rather than the shared Host
  Configuration project.
- Infrastructure remains one project containing both retrieval and indexing
  adapters.
- Exactly one CLI process writes to a SQLite database at a time.
- No database or MCP contract migration is required.
