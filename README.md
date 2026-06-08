# MCP Documentation Server

A .NET 10 Model Context Protocol server for helping coding agents discover and
correctly use internal NuGet packages and OpenAPI-generated C# clients.

Stage 2 adds safe NuGet package indexing into SQLite and FTS5. The server can
discover configured package versions, index package metadata, README and XML
documentation, and inspect public assembly symbols without executing package
code.

NuGet retrieval tools remain placeholders until Stage 3, so tool calls still
return a structured `not_ready` response.

## Prerequisites

- .NET SDK 10.0.204 or a compatible 10.0 patch selected by `global.json`.

## Build and test

```powershell
dotnet restore .\McpDocServer.slnx
dotnet build .\McpDocServer.slnx --no-restore
dotnet test .\McpDocServer.slnx --no-build --no-restore
```

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

## Tools

- `resolve_library`: find an internal package or generated client.
- `query_docs`: retrieve version-aware documentation evidence.
- `get_symbol`: inspect a public type or member.
- `find_api_operation`: find an OpenAPI operation and generated-client mapping.
- `list_versions`: list indexed versions and the recommended version.

Until Stage 3, a successful tool invocation returns:

```json
{
  "status": "not_ready",
  "data": null,
  "resolvedContext": null,
  "evidence": [],
  "citations": [],
  "warnings": [],
  "errors": [
    {
      "code": "stage_not_implemented",
      "message": "This capability is planned for a later stage."
    }
  ]
}
```

`not_ready` is a normal tool result, not an MCP protocol error.

## Configuration

Configuration is read from `appsettings.json`, environment variables, and
command-line configuration using standard .NET configuration rules.

The root section is `McpDocServer`:

```json
{
  "McpDocServer": {
    "DatabasePath": "data/docs.db",
    "RecommendedVersions": {},
    "NuGetSources": [],
    "OpenApiSources": [],
    "RepositorySources": [],
    "Indexing": {
      "RunOnStartup": false,
      "RefreshInterval": "01:00:00",
      "MaxPackageBytes": 104857600,
      "MaxDocumentBytes": 20971520,
      "MaxArchiveEntries": 10000,
      "MaxExtractedBytes": 524288000,
      "MaxCompressionRatio": 200,
      "MaxDocumentChars": 4000,
      "DefaultMaxResults": 10,
      "RequestTimeout": "00:00:30",
      "PackageDownloadTimeout": "00:02:00"
    }
  }
}
```

Environment variables use double underscores:

```powershell
$env:McpDocServer__DatabasePath = "C:\mcp-doc-server\data\docs.db"
```

Source collections may be empty. Startup validates configured names, URIs,
paths, versions, intervals, and limits. NuGet sources are contacted only when
`Indexing:RunOnStartup` is `true`.

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

## Design

- [Product specification](design/spec.md)
- [Stage 1 BRD](design/stages/01-skeleton/brd.md)
- [Stage 1 implementation plan](design/stages/01-skeleton/plan.md)
- [Stage 2 BRD](design/stages/02-nuget-indexing/brd.md)
- [Stage 2 implementation plan](design/stages/02-nuget-indexing/plan.md)
