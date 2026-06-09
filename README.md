# MCP Documentation Server

A .NET 10 Model Context Protocol server for helping coding agents discover and
correctly use internal NuGet packages, including generated API-client packages.

Stage 3 adds version-aware NuGet retrieval over the SQLite/FTS5 index. The
server can resolve packages, select versions, query README and XML
documentation, inspect public symbols, and return stable citations without
executing package code.

## Prerequisites

- .NET SDK 10.0.204 or a compatible 10.0 patch selected by `global.json`.

## Build and test

```powershell
dotnet restore .\McpDocServer.slnx
dotnet build .\McpDocServer.slnx --no-restore
dotnet test .\McpDocServer.slnx --no-build --no-restore
```

## Run the indexing worker

The Worker is the only process that writes the index. Run it continuously:

```powershell
dotnet run --project .\src\McpDocServer.Indexing.Worker\McpDocServer.Indexing.Worker.csproj
```

It indexes immediately, then waits for `Indexing:RefreshInterval` after each
completed run before starting the next one. To perform one refresh and exit:

```powershell
dotnet run --project .\src\McpDocServer.Indexing.Worker\McpDocServer.Indexing.Worker.csproj -- --once
```

One-shot execution exits `0` after a successful run or when no sources are
configured. It exits `1` after a failed or partially successful run.

## Run over stdio

```powershell
dotnet run --project .\src\McpDocServer.Host\McpDocServer.Host.csproj
```

Standard output is reserved for MCP protocol messages. Application and startup
logs are written to standard error.

Example MCP client configuration:

```json
{
  "mcpServers": {
    "internal-docs": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\mcp-doc-server\\src\\McpDocServer.Host\\McpDocServer.Host.csproj"
      ]
    }
  }
}
```

For a client launched outside the repository, use an absolute project path.

## Run over Streamable HTTP

Start the same host in local HTTP mode:

```powershell
dotnet run --project .\src\McpDocServer.Host\McpDocServer.Host.csproj `
  -- --McpDocServer:Transport=http
```

Connect a Streamable HTTP MCP client to:

```text
http://127.0.0.1:5034/mcp
```

HTTP mode is stateless and intentionally unauthenticated for local development.
Configuration validation restricts it to a loopback HTTP URL. Shared-network
binding, TLS, authentication, authorization, and CORS remain Stage 5 work.

## Test with MCP Inspector

For stdio, launch Inspector from the repository root:

```powershell
npx -y @modelcontextprotocol/inspector dotnet run --project .\src\McpDocServer.Host\McpDocServer.Host.csproj
```

The host loads `appsettings.json` from its executable directory, independent of
the caller's working directory. Configure an absolute `DatabasePath` when the
index should live outside the repository.

To inspect HTTP mode, start the server separately with the HTTP command above,
select **Streamable HTTP** in Inspector, and connect to
`http://127.0.0.1:5034/mcp`.

After connecting:

1. Open **Tools** and call `resolve_library` with an indexed package ID.
2. Pass the returned `nuget:{packageId}` value to `list_versions`.
3. Call `query_docs` or `get_symbol` with a concrete version.
4. Open **Resources** to read cited artifact and symbol URIs.

## Tools

- `resolve_library`: find an indexed NuGet package by ID or descriptive text.
- `query_docs`: retrieve ranked, version-isolated documentation evidence.
- `get_symbol`: inspect a public type or member, including XML documentation.
- `list_versions`: list indexed semantic versions and the recommendation.

The tools return structured `ok`, `not_found`, or `insufficient_evidence`
results.

## Resources

Tool citations can be read through these MCP resource templates:

```text
nuget://{source}/{packageId}/{version}/artifact/{path}
nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}
```

Resources are served only from the local index; retrieval never contacts the
NuGet feed.

## Configuration

The Host and Worker have separate `appsettings.json` files. Both use the
`McpDocServer` root section and standard .NET environment-variable and
command-line overrides.

Host configuration:

```json
{
  "McpDocServer": {
    "Transport": "stdio",
    "Http": {
      "Url": "http://127.0.0.1:5034",
      "Path": "/mcp"
    },
    "DatabasePath": "data/docs.db",
    "RecommendedVersions": {},
    "Retrieval": {
      "SourceOrder": [ "internal" ],
      "DefaultMaxResults": 8,
      "MaxResults": 25,
      "MaxResponseBytes": 102400,
      "QueryTimeout": "00:00:05",
      "MinimumEvidenceScore": 0.15,
      "AmbiguousSymbolLimit": 10
    }
  }
}
```

Worker configuration:

```json
{
  "McpDocServer": {
    "DatabasePath": "data/docs.db",
    "NuGetSources": [],
    "RepositorySources": [],
    "Indexing": {
      "RefreshInterval": "01:00:00",
      "MaxPackageBytes": 104857600,
      "MaxDocumentBytes": 20971520,
      "MaxArchiveEntries": 10000,
      "MaxExtractedBytes": 524288000,
      "MaxCompressionRatio": 200,
      "MaxDocumentChars": 4000,
      "PackageDownloadTimeout": "00:02:00"
    }
  }
}
```

Environment variables use double underscores:

```powershell
$env:McpDocServer__DatabasePath = "C:\mcp-doc-server\data\docs.db"
```

Source collections may be empty. The Host validates transport and retrieval
settings independently from the Worker, which validates source definitions and
indexing limits. The Host never contacts NuGet sources.

`Retrieval:SourceOrder` controls source precedence without requiring source
definitions in the Host. Give both processes the same `DatabasePath`; when it
is relative, launch both from the same working directory.

Example NuGet source:

```json
{
  "Name": "internal",
  "ServiceIndex": "https://packages.example/v3/index.json",
  "PackagePrefixes": [ "Company." ],
  "PackageIds": [ "Company.Foundation" ],
  "IncludePrerelease": false,
  "IncludeUnlisted": false,
  "MaxVersionsPerPackage": 3,
  "MaxPackages": 100
}
```

`ServiceIndex` may also be a local package-folder path, which is useful for
offline fixtures. Prefix discovery normally cannot find new unlisted packages;
use `PackageIds` when an unlisted package must be indexed.

On a successful run, the database contains package versions, artifacts,
dependencies, target frameworks, public symbols, document chunks, FTS5 rows,
and index-run history. Re-indexing an unchanged archive skips rewriting it. A
failed package update preserves the prior successful version.

Do not place feed credentials or API tokens in these configuration objects.
Future source integrations will obtain secrets from approved credential
providers.

Generated API clients require no special configuration. Publish them as NuGet
packages with a README, XML documentation, and public assemblies, then include
their package IDs or prefixes in a configured NuGet source.

## Design

- [Product specification](design/spec.md)
- [Stage 1 BRD](design/stages/01-skeleton/brd.md)
- [Stage 1 implementation plan](design/stages/01-skeleton/plan.md)
- [Stage 2 BRD](design/stages/02-nuget-indexing/brd.md)
- [Stage 2 implementation plan](design/stages/02-nuget-indexing/plan.md)
- [Stage 3 BRD](design/stages/03-nuget-retrieval/brd.md)
- [Stage 3 implementation plan](design/stages/03-nuget-retrieval/plan.md)
- [Stage 4 BRD](design/stages/04-hardening/brd.md)
- [Stage 5 BRD](design/stages/05-shared-deployment/brd.md)
