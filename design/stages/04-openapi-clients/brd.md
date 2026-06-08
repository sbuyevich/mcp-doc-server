# Stage 4 BRD: OpenAPI Clients

## Purpose

Index OpenAPI documents and connect API operations to the corresponding
symbols in generated C# client packages.

## Business Outcome

A coding agent can describe an API action and receive the matching endpoint,
request and response models, and generated C# client method with evidence.

## Dependencies

- Stage 3 completed.
- A representative OpenAPI document.
- A generated client package or source tree for that document.
- Selection of the first supported client generator.

## In Scope

- Parse approved local OpenAPI 3.x documents.
- Persist operations, parameters, request bodies, responses, and schemas.
- Search operations by ID, route, tag, and intent keywords.
- Define the generated-client adapter abstraction.
- Implement one generator adapter.
- Correlate operations with generated client methods and models.
- Implement `find_api_operation`.
- Extend `resolve_library` and `query_docs` to include OpenAPI-backed clients.

## Functional Requirements

### FR-1: OpenAPI ingestion

The indexer must extract:

- Service title and document version.
- HTTP paths and methods.
- Operation IDs, tags, summaries, and descriptions.
- Parameters and request bodies.
- Response status codes and schemas.
- Component schemas and references.
- Deprecation markers and relevant vendor extensions.

Documents must be parsed using a standards-aware OpenAPI parser.

### FR-2: Reference safety

External references must only resolve from configured local roots or approved
hosts. Circular, missing, or invalid references must produce diagnostics
without corrupting the previous successful index.

### FR-3: Operation search

`find_api_operation` must support:

- Exact operation ID.
- HTTP method and path.
- Service and tag filtering.
- Keywords from summary and description.
- Intent-oriented token search.

Exact operation ID and route matches must outrank prose matches.

### FR-4: Generator adapter

Define `IClientGeneratorAdapter` so generator-specific mapping is isolated from
the core domain.

The first adapter must identify:

- Generated client type.
- Generated method.
- Request and response C# types.
- Generator name and version when available.
- Mapping confidence.
- Evidence used for the mapping.

The adapter must be selected from the generator used by the representative
internal client.

### FR-5: Mapping confidence

Mappings must be classified as:

- `exact`: confirmed by generator metadata or stable operation metadata.
- `high`: deterministic convention with all expected symbols present.
- `suggested`: partial or heuristic match.
- `unmapped`: no reliable match.

Suggested mappings must be labeled clearly and must not be presented as facts.

### FR-6: Tool output

`find_api_operation` must return:

- Service and document version.
- HTTP method and path.
- Operation ID, summary, and deprecation state.
- Parameters, request body, and responses.
- Generated client type and method when mapped.
- Request and response C# types when mapped.
- Mapping confidence and generator.
- Citations to OpenAPI and generated symbols.

### FR-7: OpenAPI resources

Expose stable resources such as:

```text
openapi://{service}/{version}/operation/{operationId}
```

OpenAPI and generated-code citations must remain distinguishable.

## Non-Functional Requirements

- Parsing supports cancellation and configured size limits.
- OpenAPI source data is treated as untrusted content.
- Search remains deterministic for an unchanged index.
- Generator-specific behavior is fully contained in its adapter.
- Invalid operations do not prevent valid operations from being indexed when
  the parser can safely continue.

## Deliverables

- OpenAPI source configuration and indexer.
- OpenAPI persistence records and FTS integration.
- Operation search service.
- `IClientGeneratorAdapter`.
- First production generator adapter and fixtures.
- Implemented `find_api_operation`.
- Extended library and documentation queries.
- Unit, integration, mapping, and contract tests.

## Acceptance Criteria

1. A representative OpenAPI 3.x document is indexed successfully.
2. An operation resolves by exact operation ID.
3. An operation resolves by intent keywords.
4. Parameters, request body, responses, and schemas are returned correctly.
5. At least one operation maps to the correct generated client method.
6. Its request and response C# types are correct.
7. Mapping confidence and generator identity are returned.
8. Missing or uncertain mappings are labeled rather than invented.
9. OpenAPI and generated-symbol citations resolve to indexed evidence.
10. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Support for every client generator.
- Client generation.
- Remote OpenAPI retrieval unless explicitly pulled into this stage.
- Repository-wide usage example indexing.
- HTTP MCP transport and enterprise authentication.

## Exit Gate

Stage 5 may begin when an MCP client can move from a natural-language API
intent to a cited endpoint and a verified generated C# client method.
