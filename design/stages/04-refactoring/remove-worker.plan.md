# Consolidate Indexing into a One-Shot Console App

## Summary

Delete `McpDocServer.Indexing.Worker` and turn `McpDocServer.Indexing` into the sole index-writing executable. Each invocation indexes all configured sources once and exits, while repeated execution is delegated to CI, Task Scheduler, containers, or another external scheduler.

## Key Changes

### Indexing executable

- Change `McpDocServer.Indexing` to `OutputType=Exe`.
- Move `Program.cs`, `appsettings.json`, configuration binding, and run-result logging into this project.
- Run indexing once per invocation:
  - Exit `0` for success or no configured sources.
  - Exit `1` for failed, partially successful, invalid configuration, or unhandled runs.
  - Respect console cancellation and stop cleanly.
- Remove `--once`, `IndexingBackgroundService`, continuous scheduling, and `RefreshInterval`.
- Rename `IndexingRunExecutor` to `IndexingRunner` with `RunAsync`.

Run with:

```powershell
dotnet run --project .\src\McpDocServer.Indexing\McpDocServer.Indexing.csproj
```

### Vertical project ownership

- Move indexing configuration types from `McpDocServer.Configuration` into `McpDocServer.Indexing.Configuration`.
- Rename `IndexingWorkerOptions` and its validator to `IndexingAppOptions` and `IndexingAppOptionsValidator`.
- Preserve the existing `McpDocServer` JSON structure except for removing `Indexing:RefreshInterval`.
- Move indexing-only implementations from Infrastructure into Indexing:
  - NuGet discovery, authentication, download, and package processing.
  - Archive validation and metadata/symbol extraction.
  - Document chunking and hashing.
  - SQLite schema migration and index publication.
- Organize implementations under `Indexing.NuGet`, `Indexing.Processing`, and `Indexing.Persistence`.
- Replace `AddIndexingWorkerCore()` and the current partial registration methods with `AddIndexing(IConfiguration)`, which registers options, orchestration, and concrete indexing services.
- Keep SQLite schema, stable IDs, environment handling, pruning, and idempotent publication unchanged.

### Project graph and cleanup

- Remove the Worker project from the solution, test references, and filesystem.
- Make Indexing self-contained with no project references; add its required hosting, options, NuGet, and SQLite package references directly.
- Make Infrastructure retrieval-only and dependent only on Application.
- Preserve Host as retrieval-only:

```text
Application    -> no project references
Configuration  -> no project references
Indexing       -> no project references
Infrastructure -> Application
Host           -> Application + Configuration + Infrastructure
```

### Documentation

- Update README, architecture, specification, test plans, and all stage documents to use the Indexing console application.
- Move configuration instructions to `src/McpDocServer.Indexing/appsettings.json`.
- Remove Worker, continuous-refresh, `RefreshInterval`, and `--once` guidance.
- Explain that recurring indexing requires an external scheduler and that only one indexing process should write to a SQLite database at a time.
- Record this implementation plan in `design/stages/04-refactoring/remove-worker.plan.md`.

## Public Surface

- Delete the `McpDocServer.Indexing.Worker` assembly and namespace.
- Move indexing option types from `McpDocServer.Configuration` to `McpDocServer.Indexing.Configuration`.
- Replace `IndexingWorkerOptions` with `IndexingAppOptions`.
- Remove `IndexingOptions.RefreshInterval`.
- Replace `AddIndexingWorkerCore(IConfiguration)` and `AddIndexing()` with `AddIndexing(IConfiguration)`.
- Remove the `--once` command-line flag; one-shot execution becomes the only mode.
- MCP contracts, retrieval behavior, SQLite schema, and citation formats remain unchanged.

## Test Plan

- Update architecture tests for the new project graph and deleted Worker project.
- Move and rename configuration-validator tests; remove refresh-interval validation tests.
- Delete background scheduling and non-overlap tests.
- Update runner tests for no-source success, successful runs, partial failure, full failure, exception handling, and cancellation.
- Rename Worker child-process tests to Indexing process tests and invoke `McpDocServer.Indexing.dll` without `--once`.
- Verify successful indexing creates the database and exits `0`; partial and failed indexing exit `1`.
- Update indexing and retrieval pipeline tests to compose services through `AddIndexing(configuration)`.
- Verify repeated runs remain idempotent and do not duplicate canonical records.
- Search for stale `McpDocServer.Indexing.Worker`, `IndexingWorker`, `RefreshInterval`, and `--once` references outside the removal plan.
- Run:

```powershell
dotnet build .\McpDocServer.slnx
dotnet test .\McpDocServer.slnx
```

## Assumptions

- Indexing is always manually or externally scheduled; no in-process recurring mode remains.
- Exactly one Indexing process writes to a given SQLite database at a time.
- Repository source configuration remains reserved and unchanged.
- No database migration is required.
- This is an intentional source-level and deployment-command breaking change.
