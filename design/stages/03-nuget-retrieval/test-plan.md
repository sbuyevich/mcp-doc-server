# Stage 3 MCP Inspector Test Plan

## Prerequisites

- Run commands from the repository root so relative paths resolve correctly.
- Confirm `data/docs.db` exists and `NuGet.Versioning` has been indexed.
- Restart MCP Inspector after rebuilding or changing the server.

Launch the Inspector:

```powershell
npx -y @modelcontextprotocol/inspector dotnet run --project .\src\McpDocServer.Host\McpDocServer.Host.csproj
```

In the Inspector, select the stdio transport and connect. Server logs should
appear without being written to the MCP protocol stream.

Status values used below:

- `PASS`: observed result matches the expectation.
- `FAIL`: observed result differs from the expectation.
- `PENDING`: not tested yet.
- `N/A`: intentionally outside Stage 3.

## Test Results

| ID | Scenario | Status | Notes |
|---|---|---|---|
| MCP-01 | Connect and discover tools | PENDING | |
| MCP-02 | Resolve an exact package ID | PASS | `NuGet.Versioning` resolved |
| MCP-03 | Resolve a descriptive query | PENDING | |
| MCP-04 | Accept a JSON-wrapped resolve query | PENDING | Regression test |
| MCP-05 | List indexed versions | PENDING | |
| MCP-06 | Get an exact public symbol | PENDING | |
| MCP-07 | Query version-specific documentation | PENDING | |
| MCP-08 | Read an artifact resource | PENDING | |
| MCP-09 | Read a symbol resource | PENDING | |
| MCP-10 | Return `not_found` for a missing symbol | PENDING | |
| MCP-11 | Return an error for an unknown library | PENDING | |
| MCP-12 | Exclude prerelease versions by default | PENDING | |
| MCP-13 | Repeat calls deterministically | PENDING | |
| MCP-14 | OpenAPI tool remains unavailable | N/A | Stage 4 |

## MCP Inspector Tests

### MCP-01: Connect and discover tools

Open **Tools** and confirm these tools are present:

- `resolve_library`
- `list_versions`
- `query_docs`
- `get_symbol`
- `find_api_operation`

Open **Resources** and confirm templates exist for artifact and symbol URIs.

Expected: all five tools have input and output schemas, and the server remains
connected.

### MCP-02: Resolve an exact package ID

Call `resolve_library`:

```json
{
  "query": "NuGet.Versioning",
  "includePrerelease": false,
  "limit": 10
}
```

Expected:

- `status` is `ok`.
- A match has `libraryId` equal to `nuget:NuGet.Versioning`.
- `displayName` is `NuGet.Versioning`.
- `recommendedVersion` is populated.
- `confidence` is high for the exact ID match.

### MCP-03: Resolve a descriptive query

Call `resolve_library` with a concept rather than the package ID:

```json
{
  "query": "semantic version parsing",
  "includePrerelease": false,
  "limit": 10
}
```

Expected: `NuGet.Versioning` is returned when indexed package metadata or
documentation contains matching terms.

### MCP-04: Accept a JSON-wrapped resolve query

In the `query` field, pass this entire value as a string:

```json
{"query":"NuGet.Versioning","includePrerelease":false,"limit":10}
```

Expected: the server unwraps the value and returns the same package match as
MCP-02. The JSON object must not appear in a `library_not_found` message.

### MCP-05: List indexed versions

Call `list_versions`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "includePrerelease": false
}
```

Expected:

- `status` is `ok`.
- Versions are sorted in descending semantic-version order.
- Prerelease versions are absent.
- Each entry reports `listed`, `prerelease`, `deprecated`, and `indexed`.
- `recommendedVersion` and `recommendedVersionReason` are internally
  consistent.

Record a returned stable version as `<version>` for the remaining tests.

### MCP-06: Get an exact public symbol

Call `get_symbol`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "symbol": "NuGet.Versioning.NuGetVersion",
  "version": "<version>",
  "targetFramework": "net8.0",
  "includePrerelease": false
}
```

Expected:

- `status` is `ok`.
- The result identifies `NuGet.Versioning.NuGetVersion`.
- `signature`, `documentation`, `assembly`, and target frameworks are present.
- The citation uses a `nuget://` URI for the selected version.

### MCP-07: Query version-specific documentation

Call `query_docs`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "question": "How do I parse a semantic version?",
  "version": "<version>",
  "targetFramework": "net8.0",
  "maxResults": 8,
  "includePrerelease": false
}
```

Expected:

- `status` is `ok` or `insufficient_evidence`, depending on indexed content.
- `resolvedContext.version` equals `<version>`.
- Evidence does not mix content from another package version.
- Every returned evidence item has a stable `nuget://` citation.
- No signature, example, or documentation is invented.

### MCP-08: Read an artifact resource

Copy an artifact citation returned by `query_docs`. Open **Resources**, enter
the URI, and read it.

Expected: the resource content matches the evidence and is served from the
local index without contacting NuGet.

### MCP-09: Read a symbol resource

Copy the symbol citation returned by `get_symbol` and read it under
**Resources**.

Expected: the resource identifies the same package, version, framework, symbol,
signature, and documentation returned by the tool.

### MCP-10: Missing symbol

Call `get_symbol`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "symbol": "Definitely.Missing",
  "version": "<version>"
}
```

Expected: `status` is `not_found`, the response contains a machine-readable
error, and no symbol details are fabricated.

### MCP-11: Unknown library

Call `list_versions`:

```json
{
  "libraryId": "nuget:Definitely.Missing.Package",
  "includePrerelease": false
}
```

Expected: `status` is `not_found` with a machine-readable library error. The
server remains connected.

### MCP-12: Prerelease filtering

Run `list_versions` once with `includePrerelease` set to `false` and once with
it set to `true`.

Expected: stable results remain unchanged. A prerelease appears only in the
second response when a prerelease version has been indexed.

### MCP-13: Determinism

Repeat MCP-02, MCP-05, MCP-06, and MCP-07 without changing the index.

Expected: result ordering, selected version, statuses, and citation URIs remain
the same between runs.

## Automated Verification

Run the complete suite after the Inspector checks:

```powershell
dotnet test .\McpDocServer.slnx --no-restore
```

Expected: all unit and integration tests pass.

Stage 3 is complete when all applicable rows are `PASS`, citations resolve
through MCP Resources, responses remain version-isolated, and missing evidence
produces explicit statuses rather than generated content.

# Stage 3 MCP Inspector Test Plan

## Prerequisites

- Run commands from the repository root so relative paths resolve correctly.
- Confirm `data/docs.db` exists and `NuGet.Versioning` has been indexed.
- Restart MCP Inspector after rebuilding or changing the server.

Launch the Inspector:

```powershell
npx -y @modelcontextprotocol/inspector dotnet run --project .\src\McpDocServer.Host\McpDocServer.Host.csproj
```

In the Inspector, select the stdio transport and connect. Server logs should
appear without being written to the MCP protocol stream.

Status values used below:

- `PASS`: observed result matches the expectation.
- `FAIL`: observed result differs from the expectation.
- `PENDING`: not tested yet.
- `N/A`: intentionally outside Stage 3.

## Test Results

| ID | Scenario | Status | Notes |
|---|---|---|---|
| MCP-01 | Connect and discover tools | PENDING | |
| MCP-02 | Resolve an exact package ID | PASS | `NuGet.Versioning` resolved |
| MCP-03 | Resolve a descriptive query | PENDING | |
| MCP-04 | Accept a JSON-wrapped resolve query | PENDING | Regression test |
| MCP-05 | List indexed versions | PENDING | |
| MCP-06 | Get an exact public symbol | PENDING | |
| MCP-07 | Query version-specific documentation | PENDING | |
| MCP-08 | Read an artifact resource | PENDING | |
| MCP-09 | Read a symbol resource | PENDING | |
| MCP-10 | Return `not_found` for a missing symbol | PENDING | |
| MCP-11 | Return an error for an unknown library | PENDING | |
| MCP-12 | Exclude prerelease versions by default | PENDING | |
| MCP-13 | Repeat calls deterministically | PENDING | |

## MCP Inspector Tests

### MCP-01: Connect and discover tools

Open **Tools** and confirm these tools are present:

- `resolve_library`
- `list_versions`
- `query_docs`
- `get_symbol`

Open **Resources** and confirm templates exist for artifact and symbol URIs.

Expected: all four tools have input and output schemas, and the server remains
connected.

### MCP-02: Resolve an exact package ID

Call `resolve_library`:

```json
{
  "query": "NuGet.Versioning",
  "includePrerelease": false,
  "limit": 10
}
```

Expected:

- `status` is `ok`.
- A match has `libraryId` equal to `nuget:NuGet.Versioning`.
- `displayName` is `NuGet.Versioning`.
- `recommendedVersion` is populated.
- `confidence` is high for the exact ID match.

### MCP-03: Resolve a descriptive query

Call `resolve_library` with a concept rather than the package ID:

```json
{
  "query": "semantic version parsing",
  "includePrerelease": false,
  "limit": 10
}
```

Expected: `NuGet.Versioning` is returned when indexed package metadata or
documentation contains matching terms.

### MCP-04: Accept a JSON-wrapped resolve query

In the `query` field, pass this entire value as a string:

```json
{"query":"NuGet.Versioning","includePrerelease":false,"limit":10}
```

Expected: the server unwraps the value and returns the same package match as
MCP-02. The JSON object must not appear in a `library_not_found` message.

### MCP-05: List indexed versions

Call `list_versions`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "includePrerelease": false
}
```

Expected:

- `status` is `ok`.
- Versions are sorted in descending semantic-version order.
- Prerelease versions are absent.
- Each entry reports `listed`, `prerelease`, `deprecated`, and `indexed`.
- `recommendedVersion` and `recommendedVersionReason` are internally
  consistent.

Record a returned stable version as `<version>` for the remaining tests.

### MCP-06: Get an exact public symbol

Call `get_symbol`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "symbol": "NuGet.Versioning.NuGetVersion",
  "version": "<version>",
  "targetFramework": "net8.0",
  "includePrerelease": false
}
```

Expected:

- `status` is `ok`.
- The result identifies `NuGet.Versioning.NuGetVersion`.
- `signature`, `documentation`, `assembly`, and target frameworks are present.
- The citation uses a `nuget://` URI for the selected version.

### MCP-07: Query version-specific documentation

Call `query_docs`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "question": "How do I parse a semantic version?",
  "version": "<version>",
  "targetFramework": "net8.0",
  "maxResults": 8,
  "includePrerelease": false
}
```

Expected:

- `status` is `ok` or `insufficient_evidence`, depending on indexed content.
- `resolvedContext.version` equals `<version>`.
- Evidence does not mix content from another package version.
- Every returned evidence item has a stable `nuget://` citation.
- No signature, example, or documentation is invented.

### MCP-08: Read an artifact resource

Copy an artifact citation returned by `query_docs`. Open **Resources**, enter
the URI, and read it.

Expected: the resource content matches the evidence and is served from the
local index without contacting NuGet.

### MCP-09: Read a symbol resource

Copy the symbol citation returned by `get_symbol` and read it under
**Resources**.

Expected: the resource identifies the same package, version, framework, symbol,
signature, and documentation returned by the tool.

### MCP-10: Missing symbol

Call `get_symbol`:

```json
{
  "libraryId": "nuget:NuGet.Versioning",
  "symbol": "Definitely.Missing",
  "version": "<version>"
}
```

Expected: `status` is `not_found`, the response contains a machine-readable
error, and no symbol details are fabricated.

### MCP-11: Unknown library

Call `list_versions`:

```json
{
  "libraryId": "nuget:Definitely.Missing.Package",
  "includePrerelease": false
}
```

Expected: `status` is `not_found` with a machine-readable library error. The
server remains connected.

### MCP-12: Prerelease filtering

Run `list_versions` once with `includePrerelease` set to `false` and once with
it set to `true`.

Expected: stable results remain unchanged. A prerelease appears only in the
second response when a prerelease version has been indexed.

### MCP-13: Determinism

Repeat MCP-02, MCP-05, MCP-06, and MCP-07 without changing the index.

Expected: result ordering, selected version, statuses, and citation URIs remain
the same between runs.

## Automated Verification

Run the complete suite after the Inspector checks:

```powershell
dotnet test .\McpDocServer.slnx --no-restore
```

Expected: all unit and integration tests pass.

Stage 3 is complete when all applicable rows are `PASS`, citations resolve
through MCP Resources, responses remain version-isolated, and missing evidence
produces explicit statuses rather than generated content.
