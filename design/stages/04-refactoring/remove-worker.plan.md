# Replace Indexing Worker with `McpDocServer.Indexer`

## Summary

Delete both `McpDocServer.Indexing.Worker` and the old
`McpDocServer.Indexing` identity. Create `McpDocServer.Indexer` as a
self-contained, one-shot console application containing indexing models,
orchestration, configuration, NuGet processing, and SQLite persistence.

Each invocation indexes all configured sources once and exits. Recurring
execution belongs to an external scheduler.

## Key Changes

### Indexer executable

- Rename the project, assembly, root namespace, folders, references, and
  documentation from `McpDocServer.Indexing` to `McpDocServer.Indexer`.
- Make `McpDocServer.Indexer` an executable with `Program.cs` and its own
  `appsettings.json`.
- Run all configured sources once per invocation:
  - Exit `0` for success or no configured sources.
  - Exit `1` for partial success, failure, invalid configuration, or unhandled
    errors.
  - Honor console cancellation.
- Remove `--once`, `IndexingBackgroundService`, continuous mode, and
  `RefreshInterval`.
- Replace `IndexingRunExecutor` with `IndexerRunner.RunAsync()`.

Run with:

```powershell
dotnet run --project .\src\McpDocServer.Indexer\McpDocServer.Indexer.csproj
```

### Vertical project ownership

- Move indexing configuration from `McpDocServer.Configuration` into
  `McpDocServer.Indexer.Configuration`.
- Use `IndexerOptions` and `IndexerOptionsValidator` as the root configuration
  types.
- Retain the `McpDocServer` JSON root and the `Indexing` object for processing
  limits, but remove `RefreshInterval`.
- Move indexing implementations out of Infrastructure and organize them under:
  - `McpDocServer.Indexer.NuGet`
  - `McpDocServer.Indexer.Processing`
  - `McpDocServer.Indexer.Persistence`
- Keep feature contracts under:
  - `McpDocServer.Indexer.Models`
  - `McpDocServer.Indexer.Abstractions`
  - `McpDocServer.Indexer.Services`
- Add `AddIndexer(IConfiguration)` to bind configuration and register all
  indexing services.
- Preserve schema version, stable-ID generation, environment behavior, atomic
  publication, pruning, and idempotency.

### Project graph and cleanup

- Remove `McpDocServer.Indexing.Worker` and `McpDocServer.Indexing` from the
  solution and filesystem.
- Remove indexing packages, registrations, and project references from
  Infrastructure.
- Make Infrastructure retrieval-only:

```text
Application    -> no project references
Configuration  -> no project references
Indexer        -> no project references
Infrastructure -> Application
Host           -> Application + Configuration + Infrastructure
```

- Update all test references, fixture assembly names, XML symbol names,
  namespaces, and expected identifiers from `McpDocServer.Indexing` to
  `McpDocServer.Indexer`.

### Documentation

- Rewrite README, architecture, specification, top-level test plan, and all
  stage documents around the Indexer console application.
- Move configuration instructions to
  `src/McpDocServer.Indexer/appsettings.json`.
- Remove Worker, continuous scheduling, `RefreshInterval`, and `--once`
  instructions.
- Explain that recurring indexing uses an external scheduler and only one
  Indexer process may write to a SQLite database at a time.

## Public Surface

- Delete the `McpDocServer.Indexing.Worker` and `McpDocServer.Indexing`
  assemblies and namespaces.
- Introduce the `McpDocServer.Indexer` assembly and namespace hierarchy.
- Move and rename `IndexingWorkerOptions` to `IndexerOptions`.
- Move its validator to `IndexerOptionsValidator`.
- Remove `IndexingOptions.RefreshInterval`.
- Replace `AddIndexingWorkerCore()` and `AddIndexing()` with
  `AddIndexer(IConfiguration)`.
- Remove the `--once` argument because every execution is one-shot.
- MCP contracts, Host behavior, SQLite schema, qualified library IDs, and
  citations remain unchanged.

## Test Plan

- Update architecture tests for the new project graph and assert the old
  projects are absent.
- Move configuration tests to Indexer and remove scheduling validation.
- Delete background-service repetition, overlap, and interval tests.
- Test `IndexerRunner` for no sources, success, partial success, failure,
  exception handling, logging, and cancellation.
- Rename child-process tests to `IndexerProcessTests`; launch
  `McpDocServer.Indexer.dll` without `--once`.
- Verify success creates the SQLite index and exits `0`; partial and failed
  runs exit `1`.
- Update pipeline tests to compose indexing with
  `AddIndexer(configuration)`.
- Run indexing twice and verify canonical records are not duplicated while
  index-run history records both executions.
- Search for stale `McpDocServer.Indexing.Worker`, `McpDocServer.Indexing`,
  `IndexingWorker`, `RefreshInterval`, and `--once` references outside this
  replacement plan.
- Run:

```powershell
dotnet build .\McpDocServer.slnx
dotnet test .\McpDocServer.slnx
```

## Assumptions

- `Indexer` is the full project, assembly, and namespace identity.
- Terms describing the operation, such as “indexing” and `IndexCoordinator`,
  remain grammatically named; only the product/project identity becomes
  `Indexer`.
- The existing `McpDocServer:Indexing` JSON section remains for processing
  limits.
- Recurring indexing is externally scheduled.
- Exactly one Indexer process writes to a given SQLite database at a time.
- Repository source configuration remains reserved.
- No database migration is required.
- This is an intentional source-level and deployment-command breaking change.
