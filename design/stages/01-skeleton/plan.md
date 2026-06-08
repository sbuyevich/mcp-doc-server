# Stage 1 Implementation Plan: Skeleton

## 1. Objective

Implement the Stage 1 BRD as a buildable .NET 10 solution with:

- A working MCP server over stdio.
- Five discoverable placeholder tools.
- Stable request and response contracts.
- Validated configuration.
- Startup diagnostics.
- Unit, contract, and stdio integration tests.
- Local run instructions.

This stage establishes contracts and boundaries only. It must not connect to
NuGet feeds, read OpenAPI documents, or create the search database.

## 2. Technical Decisions

### 2.1 Runtime and solution format

- Target `net10.0`.
- Use `McpDocServer.slnx`.
- Use SDK-style projects.
- Use Central Package Management in `Directory.Packages.props`.
- Enable nullable reference types, implicit usings, analyzers, and warnings as
  errors for project code in `Directory.Build.props`.

Package versions must be the latest stable versions compatible with .NET 10 at
implementation time. Do not use preview packages unless the stable MCP SDK
cannot satisfy the BRD.

### 2.2 Host model

Use the .NET Generic Host:

```csharp
var builder = Host.CreateApplicationBuilder(args);
```

Register the official MCP C# SDK with:

- `AddMcpServer()`
- `WithStdioServerTransport()`
- Explicit tool registration or assembly discovery

Prefer explicit registration of the five tool classes so accidental attributed
classes cannot become part of the public MCP surface.

All console logging must be configured for standard error. Standard output is
reserved exclusively for MCP protocol messages.

### 2.3 Project ownership

```text
Domain
  No project dependencies.

Application
  Depends on Domain.
  Owns tool contracts, use-case interfaces, and placeholder implementations.

Infrastructure
  Depends on Application and Domain.
  Contains future source and storage implementations.
  Stage 1 provides only registration scaffolding and local dependency checks.

Host
  Depends on Application and Infrastructure.
  Owns MCP attributes, transport setup, options binding, logging, and startup.
```

MCP SDK types and attributes must remain in `McpDocServer.Host`. Application
contracts must be usable without an MCP transport.

### 2.4 Tool registration and schema

Create one DI-backed MCP tool class per public tool:

- `ResolveLibraryTool`
- `QueryDocsTool`
- `GetSymbolTool`
- `FindApiOperationTool`
- `ListVersionsTool`

Each MCP method must:

- Use the exact snake-case public name from the specification.
- Include a concise tool description.
- Include descriptions and defaults for every input parameter.
- Map method parameters into an Application request record.
- Pass the supplied `CancellationToken` to the Application handler.
- Return the corresponding response contract.

The SDK-generated input schema must be treated as a contract and covered by
snapshot-style assertions.

### 2.5 Placeholder behavior

Stage 1 handlers return a normal, successful MCP invocation containing:

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

The placeholder result is not an MCP protocol error. This distinction lets an
agent understand that the server is healthy but the capability has not been
implemented.

Use the MCP SDK's current structured-result mechanism when available. If the
stable SDK only exposes text content for attributed POCO returns, serialize the
same contract as JSON text and test its exact shape. Keep this adaptation in
the Host so Application contracts do not change.

### 2.6 Configuration validation

Bind the `McpDocServer` configuration section to strongly typed options and use
`ValidateOnStart`.

Stage 1 permits empty source collections so the server can run without
contacting external systems. Validation must still enforce:

- Non-empty database path.
- Unique, non-empty source names.
- Absolute NuGet service-index URIs.
- Valid OpenAPI URI or local path values.
- Valid repository root paths.
- Positive byte, timeout, and result limits.
- Positive refresh interval.
- Non-empty package prefixes when supplied.
- Valid recommended-version keys and values.

Do not include username, password, token, or API-key properties in these
options.

### 2.7 Startup diagnostics

Implement startup diagnostics as a hosted service that writes structured logs
to standard error.

Checks:

- Configuration binding and validation completed.
- All five MCP tools are registered.
- Application and infrastructure services resolve from DI.
- The configured database path is syntactically valid and its parent path can
  be resolved.

Stage 1 diagnostics must not:

- Create the database.
- Contact any configured source.
- Download packages or documents.
- Write diagnostic output to standard output.

## 3. Target Repository Layout

```text
McpDocServer.slnx
Directory.Build.props
Directory.Packages.props
global.json
src/
  McpDocServer.Domain/
    McpDocServer.Domain.csproj
  McpDocServer.Application/
    Contracts/
      Common/
        Citation.cs
        EvidenceItem.cs
        ResolvedContext.cs
        ToolError.cs
        ToolResponse.cs
        ToolResultStatus.cs
        ToolWarning.cs
      FindApiOperation/
        FindApiOperationRequest.cs
        FindApiOperationResponse.cs
      GetSymbol/
        GetSymbolRequest.cs
        GetSymbolResponse.cs
      ListVersions/
        ListVersionsRequest.cs
        ListVersionsResponse.cs
      QueryDocs/
        QueryDocsRequest.cs
        QueryDocsResponse.cs
      ResolveLibrary/
        ResolveLibraryRequest.cs
        ResolveLibraryResponse.cs
    Abstractions/
      IFindApiOperationHandler.cs
      IGetSymbolHandler.cs
      IListVersionsHandler.cs
      IQueryDocsHandler.cs
      IResolveLibraryHandler.cs
    Placeholders/
      PlaceholderToolHandlers.cs
    DependencyInjection.cs
    McpDocServer.Application.csproj
  McpDocServer.Infrastructure/
    Diagnostics/
      ILocalDependencyCheck.cs
      LocalDependencyCheck.cs
    DependencyInjection.cs
    McpDocServer.Infrastructure.csproj
  McpDocServer.Host/
    Configuration/
      IndexingOptions.cs
      McpDocServerOptions.cs
      NuGetSourceOptions.cs
      OpenApiSourceOptions.cs
      RepositorySourceOptions.cs
      McpDocServerOptionsValidator.cs
    Diagnostics/
      StartupDiagnosticsHostedService.cs
    Tools/
      FindApiOperationTool.cs
      GetSymbolTool.cs
      ListVersionsTool.cs
      QueryDocsTool.cs
      ResolveLibraryTool.cs
    DependencyInjection.cs
    Program.cs
    appsettings.json
    McpDocServer.Host.csproj
tests/
  McpDocServer.UnitTests/
    Application/
      PlaceholderToolHandlerTests.cs
    Configuration/
      McpDocServerOptionsValidatorTests.cs
    Contracts/
      ToolContractSerializationTests.cs
    Architecture/
      ProjectDependencyTests.cs
    McpDocServer.UnitTests.csproj
  McpDocServer.IntegrationTests/
    Mcp/
      ToolDiscoveryTests.cs
      PlaceholderInvocationTests.cs
      StdioProtocolTests.cs
    Startup/
      InvalidConfigurationTests.cs
      StartupDiagnosticsTests.cs
    Snapshots/
      tool-schemas/
    McpDocServer.IntegrationTests.csproj
```

Small related records may share a file if that produces clearer code. The
ownership and test coverage are more important than matching every filename
literally.

## 4. Work Plan

### Work Package 1: Pin the toolchain

Tasks:

1. Add `global.json` targeting an installed stable .NET 10 SDK with an
   appropriate roll-forward policy.
2. Add `Directory.Build.props` with:
   - `TargetFramework` set to `net10.0` where practical.
   - Nullable enabled.
   - Implicit usings enabled.
   - Latest stable C# language version supported by the SDK.
   - .NET analyzers enabled.
   - Warnings treated as errors for project compilation.
3. Add `Directory.Packages.props`.
4. Add the official stable MCP package and Microsoft hosting/options packages.
5. Add the selected test framework and `Microsoft.NET.Test.Sdk`.

Verification:

- `dotnet --info` reports a compatible .NET 10 SDK.
- Package restore succeeds without preview dependencies.

### Work Package 2: Create solution boundaries

Tasks:

1. Create the four production projects and two test projects.
2. Add all projects to `McpDocServer.slnx`.
3. Add project references:
   - Application to Domain.
   - Infrastructure to Application and Domain.
   - Host to Application and Infrastructure.
   - Unit tests to Domain, Application, Infrastructure, and Host as needed.
   - Integration tests to Host and Application.
4. Prevent reverse references and cycles.
5. Add minimal `DependencyInjection` extension classes to Application,
   Infrastructure, and Host.

Verification:

- The empty solution builds.
- An architecture test confirms Domain has no project references.
- An architecture test confirms Application does not reference Host or
  Infrastructure.

### Work Package 3: Define common result contracts

Tasks:

1. Define `ToolResultStatus` with JSON values:
   - `not_ready`
   - `ok`
   - `not_found`
   - `insufficient_evidence`
2. Define common records for:
   - Resolved library/source/version context.
   - Evidence.
   - Citations.
   - Warnings.
   - Errors.
3. Define a generic or shared response envelope.
4. Ensure collections serialize as empty arrays rather than `null`.
5. Define a single factory for placeholder `not_ready` responses.

Verification:

- Every common contract round-trips through `System.Text.Json`.
- Enum values serialize in snake case as specified.
- The placeholder response matches the expected JSON shape.

### Work Package 4: Define five tool contracts

Tasks:

1. Create request and response records for all five tools.
2. Carry forward the fields defined in the main specification.
3. Add lightweight input invariants to Application handlers, including:
   - Required query, question, library ID, or symbol values.
   - Positive result limits.
   - Optional version and target framework values.
4. Do not perform package, OpenAPI, or version resolution in this stage.
5. Add XML documentation to public contracts because these become long-lived
   boundaries for later stages.

Verification:

- All requests and responses serialize with `System.Text.Json`.
- Representative JSON examples match the main specification.
- No contract contains credential fields.

### Work Package 5: Implement placeholder application handlers

Tasks:

1. Define one asynchronous handler interface per tool.
2. Include `CancellationToken` on every handler method.
3. Implement placeholder handlers that return `not_ready`.
4. Register handlers through `AddApplication()`.
5. Use one consistent error code and stage-specific message.

Verification:

- Every handler returns `not_ready`.
- Empty evidence, citation, warning, and data fields are consistent.
- A pre-cancelled token is observed where asynchronous work occurs.

### Work Package 6: Implement options and validation

Tasks:

1. Create the root and nested options records.
2. Bind the `McpDocServer` section.
3. Implement `IValidateOptions<McpDocServerOptions>`.
4. Register validation with `ValidateOnStart`.
5. Add a safe default `appsettings.json`:
   - Local database path.
   - Empty source collections.
   - Conservative positive limits.
6. Confirm no source client is registered or invoked at startup.

Verification:

- Valid default configuration starts.
- Missing database path fails.
- Duplicate source names fail.
- Invalid URIs, paths, intervals, and limits fail with field-specific
  messages.
- Empty source collections are valid.

### Work Package 7: Implement MCP tool adapters

Tasks:

1. Create five attributed MCP tool classes.
2. Inject the corresponding Application handler through the constructor.
3. Assign the exact MCP tool names.
4. Add tool and parameter descriptions.
5. Map MCP parameters into request records.
6. Pass the MCP request cancellation token to the handler.
7. Adapt Application responses into the SDK's supported structured result.
8. Register exactly these five tools with the MCP server builder.

Verification:

- Tool discovery returns exactly five tools.
- Names are exact and stable.
- Generated input schemas include descriptions, optionality, and defaults.
- Each tool invocation returns the expected placeholder contract.

### Work Package 8: Assemble the stdio host

Tasks:

1. Use `Host.CreateApplicationBuilder(args)`.
2. Configure console logging to standard error for every log level.
3. Register:
   - Options and validation.
   - Application services.
   - Infrastructure scaffolding.
   - Startup diagnostics.
   - MCP server, stdio transport, and tools.
4. Build and run with `RunAsync`.
5. Ensure host cancellation flows to MCP and hosted services.
6. Keep `Program.cs` small by delegating registration to extension methods.

Verification:

- The process starts and waits for MCP input.
- Graceful cancellation exits within the test timeout.
- Normal startup does not contact network sources.
- No log text appears on standard output.

### Work Package 9: Add startup diagnostics

Tasks:

1. Implement an `IHostedService` for one-time startup checks.
2. Resolve and verify all five Application handlers.
3. Verify all expected tool registrations through the chosen SDK registration
   mechanism or a matching internal registration descriptor.
4. Validate the local database path without creating a database.
5. Emit a structured startup-complete log event with check counts.
6. Emit failure logs with actionable check names.

Verification:

- Successful startup logs all required diagnostic categories.
- A failed local dependency check prevents or clearly marks unsuccessful
  startup according to severity.
- Diagnostics are visible on standard error only.

### Work Package 10: Add automated tests

Implement the test matrix in Section 5.

Testing should use:

- Direct unit tests for contracts, handlers, and validation.
- An in-memory MCP transport for fast discovery and invocation tests.
- One child-process test using `StdioClientTransport` to prove the published
  host works over real stdin/stdout.

Avoid asserting internal SDK implementation details. Assert the protocol
surface observed by an MCP client.

### Work Package 11: Document local operation

Update `README.md` with:

- Purpose of the project.
- .NET SDK prerequisite.
- Restore, build, and test commands.
- Local stdio run command.
- Example MCP client configuration.
- Available placeholder tools.
- Explanation of `not_ready`.
- Configuration section and environment-variable naming.
- Explicit statement that Stage 1 does not contact configured sources.

Verification:

- A developer can follow the README from a clean checkout.
- The documented MCP command launches the built Host project.

### Work Package 12: Final acceptance pass

Tasks:

1. Restore from a clean state.
2. Build the full solution.
3. Run all tests.
4. Launch the host through the same command documented in the README.
5. List tools through an MCP client.
6. Invoke all five tools.
7. Inspect captured stdout and stderr.
8. Test one invalid configuration case.
9. Review package references for unexpected preview or transport packages.

The stage is complete only when every BRD acceptance criterion is evidenced by
an automated test or a documented repeatable check.

## 5. Test Matrix

| Test | Level | BRD coverage |
| --- | --- | --- |
| Solution builds with no warnings | Build | AC-1, NFR |
| All test projects pass | Test | AC-2 |
| Domain has no outward project dependency | Architecture | FR-1 |
| Application does not reference Host or Infrastructure | Architecture | FR-1 |
| Common response serializes to expected JSON | Unit | FR-3, NFR |
| Each request and response contract serializes | Unit | FR-3, NFR |
| Placeholder handlers return `not_ready` | Unit | FR-3, AC-5 |
| Default configuration validates | Unit | FR-4 |
| Invalid database path fails validation | Unit | FR-4, AC-7 |
| Duplicate or invalid source settings fail validation | Unit | FR-4, AC-7 |
| In-memory MCP client discovers exactly five tools | Integration | AC-4 |
| Tool schemas contain expected names, types, and defaults | Contract | FR-3, AC-4 |
| In-memory MCP client invokes all placeholder tools | Integration | AC-5 |
| Child-process MCP client connects over stdio | Integration | AC-3 |
| Child-process stdout contains only MCP protocol traffic | Integration | AC-6 |
| Child-process logs appear on stderr | Integration | AC-6 |
| Host exits promptly after cancellation | Integration | FR-2 |
| Startup does not make network requests | Integration | NFR |
| Startup diagnostics report required checks | Integration | FR-5 |

## 6. Contract Snapshot Policy

Store normalized snapshots for the five discovered tool schemas.

Snapshots must ignore fields that the SDK legitimately varies, such as protocol
capability ordering, while preserving:

- Tool name.
- Tool description.
- Required inputs.
- Input types.
- Defaults.
- Parameter descriptions.

Any schema snapshot change requires deliberate review because it changes how
agents call the server.

## 7. BRD Traceability

| BRD requirement | Planned implementation |
| --- | --- |
| FR-1 Solution structure | Work Packages 1-2 |
| FR-2 MCP hosting | Work Packages 7-8 |
| FR-3 Tool contracts | Work Packages 3-5 and 7 |
| FR-4 Configuration | Work Package 6 |
| FR-5 Diagnostics | Work Package 9 |
| Nullable and analyzers | Work Package 1 |
| Cancellation | Work Packages 5, 7, and 8 |
| JSON serialization | Work Packages 3-4 |
| No source access at startup | Work Packages 6, 8, and 10 |
| Run instructions | Work Package 11 |
| Acceptance criteria | Work Packages 10 and 12 |

## 8. Risks and Mitigations

### MCP SDK structured-result behavior

Risk: The stable SDK version may represent attributed POCO returns differently
from the desired response shape.

Mitigation: Confirm behavior in the first MCP integration test. Keep any
`CallToolResult` or JSON-content adaptation in Host and preserve the
Application response contracts.

### Tool schema drift

Risk: Renaming method parameters or changing defaults silently changes the
agent-facing JSON schema.

Mitigation: Snapshot the five schemas and require deliberate snapshot updates.

### Stdio contamination

Risk: Console logging or diagnostics written to stdout can break the MCP
connection.

Mitigation: Configure logging before host construction completes and include a
real child-process test that separates and inspects stdout and stderr.

### Over-designing future stages

Risk: Stage 1 may introduce database, NuGet, or OpenAPI abstractions before
their behavior is understood.

Mitigation: Define only the five use-case handler boundaries and the minimal
local dependency-check abstraction. Defer source and storage interfaces to
Stage 2.

### Brittle process tests

Risk: Child-process stdio tests can hang on failures.

Mitigation: Use bounded startup, invocation, cancellation, and shutdown
timeouts. Always dispose the MCP client and terminate the owned process during
test cleanup.

## 9. Recommended Execution Order

Execute work packages in this order:

1. Toolchain and solution.
2. Common contracts.
3. Tool-specific contracts.
4. Placeholder handlers.
5. Configuration and validation.
6. MCP tool adapters.
7. Host and diagnostics.
8. Unit and in-memory integration tests.
9. Real stdio process test.
10. README and final acceptance pass.

Commit-sized checkpoints should align with these boundaries so failures and
contract changes are easy to isolate.

## 10. Definition of Done

Stage 1 is done when:

- The .NET 10 solution restores and builds without warnings.
- All automated tests pass.
- A real MCP client launches the Host over stdio.
- Exactly five tools are discoverable with reviewed schemas.
- Every tool returns the structured `not_ready` contract.
- Standard output contains only MCP protocol traffic.
- Invalid configuration fails during startup with useful messages.
- Startup diagnostics confirm configuration, registration, DI, and local
  dependency readiness.
- Startup performs no NuGet, OpenAPI, or repository access.
- The README accurately documents local setup and use.
- No Stage 2 implementation has been pulled into the skeleton.
