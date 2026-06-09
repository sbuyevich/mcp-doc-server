# Stage 3 Implementation Plan: NuGet Retrieval

## Objective

Replace the NuGet placeholder handlers with deterministic, version-aware retrieval backed by the Stage 2 SQLite index. Implement library resolution, documentation search, symbol lookup, version listing, stable citations, and read-only MCP resources while leaving the OpenAPI tool as a placeholder.

## Key Decisions

- Preserve the existing four tool names and common response envelope.
- Keep retrieval orchestration in `McpDocServer.Application` and SQLite queries in `McpDocServer.Infrastructure`.
- Continue using direct `Microsoft.Data.Sqlite` with parameterized SQL.
  - Formula.SimpleRepo does not document the required FTS5/BM25 query behavior.
- Use `NuGet.Versioning` for all version parsing, equality, and ordering.
- Use canonical library IDs in the existing documented format:

  ```text
  nuget:{packageId}
  ```

- Package IDs are case-insensitive. Responses use the package ID casing stored in the selected source.
- If the same package ID exists in multiple sources, source precedence is:
  1. Earlier entry in configured `NuGetSources`.
  2. Exact configured recommended version availability.
  3. Listed stable availability.
  4. Source name ordinal comparison as the deterministic tie-breaker.
- Once a source is selected, version resolution and evidence retrieval stay within that source. Versions or evidence from different sources are never merged silently.
- Resource URI path segments use percent-encoded source name, package ID, version, paths, and symbol names.

## Configuration And Limits

Add `RetrievalOptions` under `McpDocServer`:

- `DefaultMaxResults`: `8`
- `MaxResults`: `25`
- `MaxResponseBytes`: `102400`
- `QueryTimeout`: `00:00:05`
- `MinimumEvidenceScore`: `0.15`
- `AmbiguousSymbolLimit`: `10`

Move retrieval-specific limits out of `IndexingOptions`. Keep binding compatibility for the existing `Indexing:DefaultMaxResults` and `Indexing:RequestTimeout` values for one stage by using them only when the new retrieval values are absent.

Validate all limits as positive, require `MinimumEvidenceScore` to be between `0` and `1`, and cap requested result counts at `RetrievalOptions.MaxResults`.

`RecommendedVersions` remains keyed by case-insensitive package ID. A configured recommendation is used only when that exact normalized version exists in the selected source.

## Application Interfaces

Add retrieval ports:

- `INuGetReadStore`
  - Resolves library candidates.
  - Reads indexed versions.
  - Searches document chunks.
  - Finds symbols and related members.
  - Reads exact artifact/symbol resource content.
- `IVersionResolver`
  - Applies version precedence and returns the selected version plus reason.
- `ICitationFactory`
  - Creates stable NuGet resource URIs and labels.
- `IResponseBudget`
  - Trims evidence deterministically to the configured byte budget.
- `IRetrievalConfigurationProvider`
  - Supplies database path, source precedence, recommendations, and retrieval limits.

Add internal source-neutral read models for library candidates, indexed versions, document hits, symbol hits, resource documents, and version-resolution results.

Replace only these placeholder registrations:

- `IResolveLibraryHandler`
- `IQueryDocsHandler`
- `IGetSymbolHandler`
- `IListVersionsHandler`

Keep `IFindApiOperationHandler` registered to its existing placeholder.

## SQLite Migration And Read Store

Increment the SQLite schema to version 2 without rebuilding indexed content.

Add indexes:

- `libraries(normalized_package_id)`
- `library_versions(library_id, is_listed, is_prerelease, version)`
- `document_chunks(library_version_id, kind, member_name)`
- `symbols(library_version_id, fully_qualified_name, target_framework)`
- `symbols(library_version_id, containing_type)`

Add an FTS5 table for library discovery:

```text
libraries_fts(package_id, title, description, summary, tags, document_text)
```

Populate or refresh one row per source-specific library during package publishing. Aggregate bounded package documentation text so descriptive package resolution can match indexed docs without scanning every chunk.

Migration behavior:

- Creating schema v2 backfills `libraries_fts` from existing `libraries`, latest indexed metadata, and document chunks.
- Future Stage 2 publishes refresh the affected library FTS row in the same transaction.
- Retrieval opens SQLite in read-only mode and never creates an empty database.
- Missing database, unsupported schema, or unavailable FTS5 returns a machine-readable `index_unavailable` tool error rather than an MCP protocol failure.

## Library Resolution

Implement deterministic candidate retrieval in two passes:

1. Exact/prefix package ID query against `libraries`.
2. FTS5 query against `libraries_fts`.

Normalize user queries by:

- trimming whitespace
- splitting on non-alphanumeric package separators
- quoting FTS tokens
- appending prefix matching only to tokens with at least two characters
- never inserting raw user text into SQL or FTS syntax

Score each source-specific candidate:

- Exact package ID: `1.00`
- Package ID prefix: `0.90`
- All package ID tokens matched: `0.80`
- FTS/BM25 metadata or documentation match: normalize to `0.35` through `0.75`
- Configured recommended version available: `+0.05`
- First configured source: `+0.03`, decreasing by source order
- Latest selected version deprecated: `-0.10`

Clamp confidence to `0..1`, deduplicate by package ID using source precedence, order by score descending then package ID ordinal-ignore-case, and apply the request limit.

Return:

- `status: ok` when at least one match exists.
- `status: not_found` with `library_not_found` when no match exists.
- Stable `libraryId`, display name, description, recommendation, and confidence.

`resolve_library` considers prerelease versions only when `IncludePrerelease` is true.

## Version Resolution

Apply this order within the selected source:

1. Exact `version` request.
2. Optional project-context version.
3. Configured `RecommendedVersions` entry.
4. Latest stable listed indexed version.
5. Latest listed prerelease only when prerelease is allowed.

Extend `QueryDocsRequest` and `GetSymbolRequest` with optional:

```text
ProjectVersion
IncludePrerelease
```

Add matching optional MCP tool parameters without renaming existing parameters.

Selection reasons are stable strings:

- `requested`
- `project_context`
- `configured_recommendation`
- `latest_stable`
- `latest_prerelease`

Failure behavior:

- Invalid semantic version: `not_found`, error `invalid_version`.
- Requested/project version not indexed: `not_found`, error `version_not_found`.
- Configured recommendation missing: add warning `recommended_version_not_indexed`, then continue to latest stable.
- Only prerelease versions available while prerelease is disallowed: `not_found`, error `stable_version_not_found`.

## Documentation Retrieval

Resolve the library and one concrete version before searching.

Search two result sets:

- Symbols:
  - exact fully qualified/simple/member name matches
  - prefix/token matches
- Documents:
  - FTS5 `MATCH` restricted to package ID and resolved version
  - join back to canonical `document_chunks`

Ranking:

- Exact fully qualified symbol: `1.00`
- Exact simple symbol/member name: `0.95`
- Symbol prefix/token match: `0.80`
- XML documentation linked to a matched symbol: `0.75`
- README: BM25 normalized score plus `0.10`
- Other XML documentation: BM25 normalized score plus `0.05`
- Other Markdown/text: normalized BM25 score
- Deprecated selected version: multiply by `0.85` and add a warning
- Target-framework mismatch: exclude the symbol; framework-neutral documents remain eligible

Merge and sort by score descending, kind priority, citation URI, then text ordinal. Remove duplicate content hashes before applying result and byte limits.

Response behavior:

- `ok` when at least one result meets `MinimumEvidenceScore`.
- `insufficient_evidence` when the library/version exists but no sufficiently relevant result remains.
- `not_found` for an unknown library/version.
- `UsageExample` remains empty because repository examples are out of scope.
- Populate `Data.Fragments`, `Data.Symbols`, common `Evidence`, `Citations`, `ResolvedContext`, warnings, and errors consistently.

Use UTF-8 serialized size estimates while adding results. Stop before `MaxResponseBytes`; add warning `response_truncated` when evidence is omitted.

## Symbol Lookup

Lookup order within one resolved version:

1. Case-sensitive fully qualified exact match.
2. Case-insensitive fully qualified exact match.
3. Exact final type/member segment.
4. Fully qualified suffix match.
5. Contains match.

Apply the optional target framework before ambiguity decisions. A symbol with no target framework is compatible with every target.

Outcome:

- One best exact/unambiguous result: `status: ok`, populate `Data.Symbol`.
- Multiple candidates at the winning match tier: `status: insufficient_evidence`, leave `Data.Symbol` null, populate `Data.Candidates`, add warning `ambiguous_symbol`.
- No candidates: `status: not_found`, error `symbol_not_found`.

Documentation is joined through `symbols.xml_documentation_member` to `document_chunks.member_name`. Return all target frameworks for identical symbol identity/signature rows and include related members sharing the same containing type, limited to ten.

Extend `SymbolDetails` with:

- `RelatedMembers`

Each related member contains fully qualified name, kind, and signature. Candidate and selected symbol citations use the same stable symbol URI format.

## Version Listing

Resolve `libraryId` to its selected source, read every indexed version, parse with `NuGetVersion`, filter prerelease according to the request, and sort descending with `VersionComparer.VersionRelease`.

Every row reports stored listed, prerelease, deprecated, published, and indexed state. For Stage 3, `Indexed` is always true because only indexed versions are returned.

Recommendation order:

1. Valid configured recommendation.
2. Latest stable listed version.
3. Latest listed prerelease when `IncludePrerelease` is true.

Recommendation reasons:

- `configured_recommendation`
- `latest_stable`
- `latest_prerelease`

Unknown libraries return `not_found`. A known library with no versions matching the prerelease filter returns `ok` with an empty version list and warning `no_matching_versions`.

## Citations And MCP Resources

Implement `ICitationFactory` URI templates:

```text
nuget://{source}/{packageId}/{version}/artifact/{path}
nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}
```

README citations use their actual artifact path rather than a synthetic name. XML member evidence uses the artifact URI plus the member name as citation location.

Add `[McpServerResourceType]` resource handlers and register with `WithResources<T>()`:

- Artifact template:

  ```text
  nuget://{source}/{packageId}/{version}/artifact/{path}
  ```

  Returns the reconstructed text from ordered document chunks with MIME type based on artifact kind.

- Symbol template:

  ```text
  nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}
  ```

  Returns plain text containing the stored signature and XML documentation.

Resource reads:

- Decode and validate every URI segment.
- Query exact indexed records only.
- Respect cancellation and `QueryTimeout`.
- Throw an MCP not-found exception for an unknown URI.
- Never fetch NuGet feeds during retrieval.

## Timeouts And Determinism

Each handler creates a linked cancellation token with `RetrievalOptions.QueryTimeout`. Pass it through all SQLite operations.

Use explicit SQL ordering and final in-memory tie-breakers for every result. Do not use current time, random values, or database row order in ranking.

Open read connections with:

- `Mode=ReadOnly`
- `Pooling=false`
- foreign keys enabled
- a short busy timeout

## Tests

Extend fixture indexing to contain:

- two packages with overlapping descriptive terms
- at least two versions of one package
- a prerelease version
- a deprecated or unlisted version record
- duplicate simple symbol names in different namespaces
- README and XML docs with distinguishable version text
- symbols for more than one target framework

Unit tests:

- library ID parsing and URI encoding
- source precedence
- deterministic score ordering
- FTS token escaping
- all version-resolution branches
- NuGet semantic-version ordering
- response byte-budget truncation
- symbol match tiers and ambiguity
- target-framework filtering

Integration tests:

- exact and descriptive package resolution
- configured recommendation selection
- requested version isolation
- query-doc ranking and citations
- unknown/insufficient-evidence statuses
- exact symbol with XML documentation
- ambiguous symbol candidates
- version ordering and prerelease filtering
- artifact and symbol MCP resource reads
- missing database behavior
- tool calls through the in-memory MCP transport
- repeated queries produce byte-for-byte equivalent structured results

Performance tests use a seeded local SQLite database and assert:

- exact symbol lookup under 300 ms
- documentation query under 1 second
- version listing under 200 ms

Use generous CI setup/warm-up separation and measure the retrieval call, not fixture indexing.

Contract tests snapshot representative `ok`, `not_found`, `insufficient_evidence`, and ambiguous responses for all four tools.

## Work Packages

1. Add retrieval configuration, compatibility fallback, and validation.
2. Add schema-v2 migration, retrieval indexes, and library FTS maintenance.
3. Add retrieval read models, ports, library ID parser, and citation factory.
4. Implement SQLite read store and safe FTS query builder.
5. Implement version resolver and source precedence.
6. Implement library resolution handler.
7. Implement documentation query handler and response budgeting.
8. Implement symbol lookup handler.
9. Implement version listing handler.
10. Add artifact and symbol MCP resources.
11. Replace NuGet placeholder registrations while retaining the OpenAPI placeholder.
12. Expand fixtures, contract snapshots, integration tests, and performance checks.
13. Update README with Stage 3 tool/resource examples and Inspector testing steps.
14. Run build/test and verify deterministic output.

## Definition Of Done

- The four NuGet MCP tools return real indexed results.
- Exact versions and source boundaries are respected.
- All returned evidence has a stable citation.
- Artifact and symbol resources are readable over MCP.
- Missing and ambiguous evidence produce explicit statuses.
- Responses respect configured result, timeout, and byte limits.
- `dotnet build .\McpDocServer.slnx` succeeds.
- `dotnet test .\McpDocServer.slnx` succeeds.
