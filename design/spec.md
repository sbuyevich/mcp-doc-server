# Internal Documentation MCP Server

## 1. Summary

Build a .NET MCP server that helps coding agents discover and correctly use:

- Internal NuGet packages.
- C# clients generated from OpenAPI documents.
- Real usage examples from approved source repositories.

The server indexes package metadata, XML documentation, README files, OpenAPI
operations and schemas, generated-client symbols, and selected code examples.
It exposes focused MCP tools that return version-aware answers with citations
to the indexed source.

The product should behave like an internal, organization-specific Context7:
an agent identifies a library or client, asks a concrete implementation
question, and receives relevant documentation and examples for the correct
version.

## 2. Goals

1. Reduce hallucinated package APIs, method names, and request models.
2. Make internal documentation available through a standard MCP interface.
3. Keep every answer traceable to a package, OpenAPI document, or repository
   location.
4. Distinguish package and client versions so agents do not combine
   incompatible APIs.
5. Support private feeds and documents without exposing their credentials.
6. Provide useful exact lookup before introducing embedding-based search.

## 3. Non-Goals

The first release will not:

- Generate or publish NuGet packages.
- Generate OpenAPI clients.
- Execute code from indexed packages.
- Load package assemblies into the server process.
- Modify source repositories.
- Replace an API portal or general-purpose documentation website.
- Answer questions from model knowledge when no indexed evidence exists.
- Automatically support every OpenAPI client generator in the first release.

## 4. Primary Users

### Coding agent

Needs to find the correct package, type, method, endpoint, or example while
editing an application.

### Platform team

Configures approved package feeds, OpenAPI sources, repositories, indexing
rules, authentication, and retention.

### Package or API owner

Publishes documentation and expects agents to use the current supported API.

## 5. Core User Flows

### 5.1 Find a package

1. The agent provides a name or concept such as `customer events`.
2. The server returns matching internal packages and available versions.
3. The result includes package ID, description, feed, version, target
   frameworks, deprecation state, and confidence.

### 5.2 Ask how to use a package

1. The agent supplies a package ID, optional version, and a natural-language
   question.
2. The server searches documentation, symbols, and examples for that package
   version.
3. The response contains concise excerpts, symbol signatures, and citations.

### 5.3 Find a generated client operation

1. The agent supplies a service/client name and an intent such as
   `create a refund`.
2. The server finds matching OpenAPI operations.
3. When a generator adapter can correlate the operation, the response also
   gives the generated C# client type, method, request type, response type, and
   package version.

### 5.4 Inspect a symbol

1. The agent supplies a qualified or partial symbol name.
2. The server returns the symbol kind, namespace, signature, documentation,
   containing package, version, and related examples.

### 5.5 Handle missing evidence

If no reliable source is found, the server returns an explicit `not_found` or
`insufficient_evidence` result. It must not invent an API.

## 6. Source Types

### 6.1 NuGet feeds

Use NuGet v3 feeds configured by administrators. The indexer should use the
official NuGet client APIs rather than constructing feed URLs manually.

For each selected package version, ingest:

- Package ID and normalized version.
- `.nuspec` metadata and dependencies.
- Supported target frameworks.
- Package README.
- XML documentation files.
- Documentation files explicitly included in the package.
- Public symbols from managed assemblies using metadata-only inspection.
- Deprecation, listed/unlisted, and prerelease state when available.
- Repository and project URLs when present.

Candidate NuGet client resources include `PackageSearchResource`,
`PackageMetadataResource`, `RegistrationResourceV3`, and
`FindPackageByIdResource`.

### 6.2 OpenAPI documents

Support OpenAPI documents from:

- Approved local files or directories.
- Approved HTTPS endpoints.
- Files embedded in or associated with NuGet packages.

Ingest:

- Service title and document version.
- Paths and HTTP methods.
- `operationId`, summary, description, and tags.
- Parameters and request bodies.
- Response status codes and schemas.
- Component schemas and references.
- Deprecation markers and vendor extensions.

Documents must be parsed through an OpenAPI parser. References may only resolve
from approved locations.

### 6.3 Generated C# clients

Generated clients are generator-specific. The server will define an
`IClientGeneratorAdapter` interface that maps OpenAPI operations to generated
C# symbols.

An adapter may inspect:

- Generated source files.
- XML documentation.
- Assembly metadata.
- Generator manifests or configuration.
- Stable generator-specific naming conventions.

An adapter must report a mapping confidence. Low-confidence mappings are shown
as suggestions, not facts.

The MVP must include one adapter selected from the organization's actual
generator. Other generators remain extension points.

### 6.4 Usage repositories

Optionally index approved source repositories for examples:

- `.cs` files under configured roots.
- Unit and integration tests.
- Sample applications.
- Markdown documentation.

Examples must record repository, revision, path, line range, and referenced
package version where it can be determined. Generated folders, build output,
secrets, and configured exclusions must not be indexed.

## 7. Version Resolution

All searchable records belong to a concrete source version.

Resolution rules:

1. Use an exact requested version when present.
2. Otherwise use a version supplied in project context.
3. Otherwise use the configured recommended version.
4. Otherwise use the latest stable, listed version.
5. Include prerelease versions only when explicitly requested or configured.

Every response must state which version was searched. Results from different
versions must not be merged without labeling each result.

## 8. MCP Interface

The server will use the official Model Context Protocol C# SDK. Tool classes
are registered through dependency injection and exposed with the SDK's MCP
tool attributes.

### 8.1 `resolve_library`

Find the best package or generated client for a name or concept.

Input:

```json
{
  "query": "customer events",
  "includePrerelease": false,
  "limit": 10
}
```

Output:

```json
{
  "matches": [
    {
      "libraryId": "nuget:Company.Customer.Client",
      "kind": "nuget",
      "displayName": "Company.Customer.Client",
      "recommendedVersion": "4.2.0",
      "description": "Customer service client",
      "confidence": 0.97
    }
  ]
}
```

### 8.2 `query_docs`

Answer a concrete question using records from one library and version.

Input:

```json
{
  "libraryId": "nuget:Company.Customer.Client",
  "version": "4.2.0",
  "question": "How do I register and call the customer search client?",
  "targetFramework": "net10.0",
  "maxResults": 8
}
```

Output fields:

- Resolved library and version.
- Evidence-ranked answer fragments.
- Relevant symbols and signatures.
- Usage examples.
- Citations.
- Warnings for deprecated or incompatible APIs.
- Result status: `ok`, `not_found`, or `insufficient_evidence`.

The server returns evidence, not an ungrounded model-generated tutorial.
Clients or agents may synthesize the final explanation from that evidence.

### 8.3 `get_symbol`

Find and describe a type or member.

Input fields:

- `libraryId`
- `version`
- `symbol`
- Optional `targetFramework`

Output fields:

- Fully qualified name.
- Symbol kind.
- C#-style signature.
- XML documentation.
- Containing assembly and package.
- Availability by target framework.
- Related members and examples.
- Citations.

### 8.4 `find_api_operation`

Find OpenAPI operations and their generated-client mappings.

Input fields:

- `service` or `libraryId`
- `query`
- Optional `version`
- Optional `httpMethod`

Output fields:

- HTTP method and path.
- `operationId`.
- Summary and deprecation state.
- Parameters, request body, and responses.
- Generated client type and method when mapped.
- Request and response C# types when mapped.
- Mapping confidence and generator name.
- Citations to the OpenAPI source and generated code.

### 8.5 `list_versions`

List indexed versions and identify the recommended version.

Input fields:

- `libraryId`
- Optional `includePrerelease`

Output fields:

- Versions in descending semantic-version order.
- Listed, prerelease, deprecated, and indexed states.
- Publication time when available.
- Recommended version and reason.

### 8.6 Resources

Expose exact indexed artifacts as read-only MCP resources where supported:

```text
nuget://{feed}/{packageId}/{version}/readme
nuget://{feed}/{packageId}/{version}/symbol/{qualifiedName}
openapi://{service}/{version}/operation/{operationId}
repo://{repository}/{revision}/{path}
```

Tool citations should use these stable URIs.

## 9. Search and Ranking

The MVP uses deterministic retrieval:

1. Exact package, operation ID, and qualified-symbol matches.
2. Prefix and token matches.
3. SQLite FTS5/BM25 full-text ranking.
4. Source-quality boosts.
5. Version and target-framework filters.

Ranking priorities:

- Exact symbols and operation IDs outrank prose.
- Documentation from the selected version outranks other versions.
- Package-authored documentation outranks inferred metadata.
- Repository examples from tests or samples outrank arbitrary call sites.
- Deprecated sources receive a penalty and a warning.

Optional semantic/vector search may be added later behind an
`ISemanticSearchProvider`. It is not required for the MVP.

## 10. Indexing Pipeline

Each source follows this pipeline:

1. Discover configured artifacts.
2. Fetch with authentication and cache controls.
3. Validate type, size, and source allowlist.
4. Parse into normalized records.
5. Extract searchable text, symbols, and relationships.
6. Split large documents on semantic boundaries.
7. Compute a content hash.
8. Upsert changed records in one transaction.
9. Remove records no longer present according to retention policy.
10. Emit metrics and a source-level indexing result.

Indexing must be incremental and idempotent. A failed source update must leave
the previous successful index available.

## 11. Data Model

Use SQLite for the MVP, including FTS5 for searchable content.

Core entities:

- `Source`: configured feed, OpenAPI location, or repository.
- `Library`: package or service identity.
- `LibraryVersion`: normalized version and lifecycle metadata.
- `Artifact`: README, XML doc, assembly, OpenAPI document, or source file.
- `DocumentChunk`: searchable text with location metadata.
- `Symbol`: namespace, type, method, property, parameter, or model.
- `ApiOperation`: method, path, operation ID, request, and responses.
- `ClientMapping`: operation-to-generated-symbol relationship and confidence.
- `Example`: code excerpt and referenced symbols.
- `IndexRun`: status, counts, duration, and errors.

All evidence records include:

- Stable internal ID.
- Source ID.
- Library and version.
- Content hash.
- Citation URI.
- Last indexed time.

## 12. Architecture

Target .NET 10 for a new implementation.

Suggested projects:

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

Responsibilities:

- `Host`: MCP transport, authentication, configuration, health endpoints, and
  dependency injection.
- `Application`: tool handlers, queries, indexing orchestration, and ranking.
- `Domain`: source-neutral records, version rules, and contracts.
- `Infrastructure`: NuGet, OpenAPI, source-code, SQLite, and generator adapters.

Important interfaces:

```csharp
public interface ISourceIndexer;
public interface IDocumentStore;
public interface ILibraryResolver;
public interface IDocumentSearch;
public interface ISymbolReader;
public interface IOpenApiReader;
public interface IClientGeneratorAdapter;
public interface ICitationFactory;
```

## 13. Hosting and Transport

Support two host modes:

- `stdio` for local development and desktop agent clients.
- Streamable HTTP for shared internal deployments.

The HTTP server should use stateless MCP sessions unless a future feature
requires server-to-client requests. It should expose health and readiness
endpoints separately from the MCP endpoint.

Console logging in stdio mode must go to standard error so it cannot corrupt
MCP messages.

## 14. Configuration

Configuration is loaded from standard .NET configuration providers.

Example:

```json
{
  "McpDocServer": {
    "DatabasePath": "data/docs.db",
    "RecommendedVersions": {
      "Company.Customer.Client": "4.2.0"
    },
    "NuGetSources": [
      {
        "Name": "internal",
        "ServiceIndex": "https://packages.example/v3/index.json",
        "PackagePrefixes": [ "Company." ]
      }
    ],
    "OpenApiSources": [
      {
        "Name": "customer-api",
        "Location": "https://apis.example/customer/openapi.json"
      }
    ],
    "RepositorySources": [],
    "Indexing": {
      "RefreshInterval": "01:00:00",
      "MaxPackageBytes": 104857600,
      "MaxDocumentBytes": 20971520
    }
  }
}
```

Credentials must come from environment variables, managed identity, an
approved NuGet credential provider, or a secret store. Credentials must never
be persisted in the index or returned by MCP tools.

## 15. Security

1. Authenticate HTTP MCP clients with the organization's standard identity
   provider.
2. Apply authorization to tools and sources; a caller may only retrieve
   evidence they are allowed to access.
3. Use an allowlist for remote hosts and local roots.
4. Block arbitrary URLs supplied in tool calls.
5. Prevent ZIP path traversal and decompression bombs.
6. Enforce package, document, result, and timeout limits.
7. Parse assemblies as metadata; do not execute or load package code.
8. Do not resolve OpenAPI references outside approved roots and hosts.
9. Exclude secrets, build output, and configured repository paths.
10. Treat all indexed content as untrusted data, including text that resembles
    agent instructions.
11. Audit indexing and retrieval by source, caller, and result status without
    logging sensitive document bodies.

## 16. Observability

Provide:

- Structured logs.
- OpenTelemetry traces and metrics.
- Health and readiness checks.
- Index duration and failure counts by source.
- Indexed packages, versions, symbols, operations, and chunks.
- Tool latency, result count, and result status.
- Cache hit rate and source-fetch duration.

Do not use package or document contents as high-cardinality metric labels.

## 17. Testing

### Unit tests

- Semantic-version selection.
- Package and symbol resolution.
- Search ranking.
- Citation URI generation.
- OpenAPI operation extraction.
- Generator mapping and confidence.
- Chunking and content hashing.
- Security validation for paths and archives.

### Integration tests

- Index a fixture NuGet v3 feed.
- Download and parse a fixture `.nupkg`.
- Parse OpenAPI 3.x fixture documents and references.
- Index a generated-client fixture.
- Query SQLite FTS and retrieve citations.
- Invoke each MCP tool through an in-memory or test transport.
- Verify failed reindexing preserves the previous index.
- Verify source authorization prevents cross-source retrieval.

### Contract tests

Snapshot the JSON schema and representative output for every MCP tool.

## 18. Performance Targets

For an initial deployment of 10,000 package versions and 1,000 OpenAPI
documents:

- Exact symbol lookup p95: under 300 ms.
- Documentation query p95: under 1 second.
- Version listing p95: under 200 ms.
- Incremental reindex: process only changed artifacts.
- Default tool response: no more than 100 KB.
- All operations support cancellation and configured timeouts.

These targets exclude upstream download time during indexing.

## 19. MVP Scope

The first usable release includes:

1. .NET 10 host using the official MCP C# SDK.
2. Stdio transport.
3. One configured NuGet v3 feed.
4. NuGet metadata, README, XML docs, and metadata-only symbol indexing.
5. Local-file OpenAPI indexing.
6. One generated-client adapter.
7. SQLite FTS5 storage and deterministic ranking.
8. `resolve_library`, `query_docs`, `get_symbol`,
   `find_api_operation`, and `list_versions`.
9. Stable citations and explicit missing-evidence responses.
10. Unit and integration tests using local fixtures.

HTTP transport, enterprise authentication, repository indexing, and semantic
search may follow after the local MVP is validated.

## 20. Milestones

### Milestone 1: Skeleton

- Create solution and project boundaries.
- Configure MCP stdio hosting and dependency injection.
- Implement configuration validation and health diagnostics.
- Define tool contracts and return models.

### Milestone 2: NuGet indexing

- Query the configured v3 feed.
- Download packages safely.
- Parse metadata, README, XML docs, and assembly symbols.
- Persist records and full-text indexes.

### Milestone 3: NuGet retrieval

- Implement library/version resolution.
- Implement documentation and symbol queries.
- Return stable citations.

### Milestone 4: OpenAPI clients

- Parse OpenAPI documents.
- Implement operation search.
- Implement and test the first generator adapter.
- Connect operations to generated C# symbols.

### Milestone 5: Hardening

- Add incremental refresh, failure recovery, limits, telemetry, and security
  tests.
- Run acceptance scenarios with a coding agent.

### Milestone 6: Shared deployment

- Add Streamable HTTP transport.
- Add authentication, per-source authorization, and deployment manifests.

## 21. MVP Acceptance Criteria

The MVP is accepted when:

1. An MCP client can connect over stdio and discover all five tools.
2. A configured internal package can be resolved by exact ID and by a term
   from its description.
3. A caller can request an exact package version and receive only evidence
   from that version.
4. A public type and method can be found with their signatures and XML docs.
5. A natural-language package question returns ranked, cited evidence.
6. An OpenAPI operation can be found by operation ID and by intent keywords.
7. At least one generated-client operation maps to the correct C# method and
   request/response types.
8. Every result identifies its source and version.
9. Unknown APIs return `not_found` or `insufficient_evidence` without invented
   members.
10. Package code is never executed during indexing.
11. The solution builds and all automated tests pass.

## 22. Open Decisions

The following decisions should be made before Milestone 4:

- Which OpenAPI generator must the first adapter support?
- Where are generated clients stored: NuGet packages, source repositories, or
  both?
- Which private-feed authentication mechanism is standard in the target
  environment?
- Are package owners expected to mark a recommended version explicitly?
- Which repositories are approved as example sources?
- Is source-level authorization required for the first shared deployment?
