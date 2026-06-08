# MCP Documentation Server

A .NET 10 Model Context Protocol server for helping coding agents discover and
correctly use internal NuGet packages and OpenAPI-generated C# clients.

Stage 1 provides the server skeleton, validated configuration, and five stable
tool contracts. Indexing and retrieval are implemented in later stages, so all
tool calls currently return a structured `not_ready` response.

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

During Stage 1, a successful invocation returns:

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
      "RefreshInterval": "01:00:00",
      "MaxPackageBytes": 104857600,
      "MaxDocumentBytes": 20971520,
      "DefaultMaxResults": 10,
      "RequestTimeout": "00:00:30"
    }
  }
}
```

Environment variables use double underscores:

```powershell
$env:McpDocServer__DatabasePath = "C:\mcp-doc-server\data\docs.db"
```

Source collections may be empty in Stage 1. Startup validates configured names,
URIs, paths, versions, intervals, and limits, but it does not contact NuGet
feeds, retrieve OpenAPI documents, scan repositories, or create the database.

Do not place feed credentials or API tokens in these configuration objects.
Future source integrations will obtain secrets from approved credential
providers.

## Design

- [Product specification](design/spec.md)
- [Stage 1 BRD](design/stages/01-skeleton/brd.md)
- [Stage 1 implementation plan](design/stages/01-skeleton/plan.md)
