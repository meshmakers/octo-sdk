# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the entire solution
dotnet build Octo.Sdk.sln

# Build in Release mode
dotnet build Octo.Sdk.sln -c Release

# Build using local NuGet packages (from ../nuget directory)
dotnet build Octo.Sdk.sln -c DebugL

# Run all tests
dotnet test Octo.Sdk.sln

# Run tests for a specific project
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Architecture Overview

This is the **Octo SDK**, a .NET framework for building distributed mesh services with real-time communication, ETL data pipelines, and adaptive services.

### Project Structure

**Core Libraries (src/)**
- **Sdk.ServiceClient** - REST and SignalR client proxies for mesh services. Contains `ServiceClient` (REST via RestSharp), `SignalRClient<TOptions>` (real-time), and domain clients (`IdentityServicesClient`, `BotServicesClient`, etc.)
- **Sdk.Common** - Adapter framework with ETL pipeline orchestration. Key: `IAdapterService` interface, `AdapterBuilder`, `EtlDataOrchestrator`, `IPipelineNode`
- **Sdk.CommunicationAdapter** - Adapter hosting infrastructure using .NET Generic Host with DI
- **Communication.Contracts** - Shared DTOs and SignalR hub interfaces (`IAdapterHub`, `IAdapterHubCallbacks`, `IPoolHub`)
- **Sdk.SourceGeneration** - C# incremental source generator that produces query/mutation DTOs from Construction Kit YAML schemas
- **Sdk.Plug.Simulation** / **Sdk.SimulationNodes** - Data simulation adapter using Bogus library

### Key Patterns

**ETL Pipeline Pattern**
- `EtlDataOrchestrator` executes pipelines via `IPipelineNode` chains
- `IDataContext` is path-only on the routine node-author surface; backed by a single `DataContextImpl` over an `IReadSource` seam — `ElementSource` (zero-copy `JsonElement` read base) for the root, `LayeredSource` (aliases + parent-fallback + sparse `JsonNode` overlay) for iteration children; writes lift the overlay only on mutation. One generic struct-constrained JSONPath walker (`JsonPathWalker` over `IJsonView<TSelf>` with `ElementView`/`NodeView`, non-boxing) drives all reads over both the element base and the node overlay (it replaced the former dual `JsonPathEvaluator`/`JsonNodePath` read walkers — `JsonPathEvaluator` was deleted and `JsonNodePath`'s read path now delegates to the unified walker). `JsonNodePath` is retained for its write/normalize helpers (`Set`/`Remove`/`NormalizePathOrRelative`) plus thin public `Select`/`SelectAll` read wrappers (re-added for adapter call sites that hold a raw `JsonNode`); both wrappers route through `JsonPathWalker.Select` over `NodeView`. Iteration nodes (`ForEachNode`, `ObjectIteratorNode`, `SelectByPathNode`) use `Parallel.ForAsync` over alias-based zero-copy child sub-contexts. The full node-author surface is:
  - `Get<T>(path)` / `GetValue(path)` — typed scalar read / untyped `JsonElement` read
  - `TryGet<T>(path, out value)` — non-throwing typed read (returns false when path is absent/null)
  - `Set<T>(path, value, ...)` — typed write
  - `GetKind(path)` — inspect whether a path holds a value, null, array, object, or is undefined
  - `Length(path)` — element count for arrays
  - `Iterate*Async(path, body)` — iteration over arrays
  - `UpdateMatchesAsync(jsonPath, body)` — multi-match read/write (per-match sub-contexts)
  - `SelectMatches(jsonPath)` — read-only multi-match; returns `IEnumerable<IDataContext>` of detached sub-contexts, one per JSONPath match. Replaces the removed `EnumerateMatches` (which returned raw `JsonNode?` values and exposed STJ internals to callers).
  - Node code must not pass `JsonSerializerOptions` to any `IDataContext` method — all STJ details are internal to the context implementation.
- `SystemTextJsonOptions.Default` (`src/Sdk.Common/EtlDataPipeline/SystemTextJsonOptions.cs`) is the central STJ options bundle used internally — it **preserves** explicit nulls (`DefaultIgnoreCondition = Never`) so node code can distinguish `DataKind.Null` from `DataKind.Undefined`, and carries the CK/Rt converters. (The canonical `RtSystemTextJsonSerializer` it derives from uses `WhenWritingNull`; the pipeline bundle overrides that.) The `options` parameter that was formerly on some `IDataContext` methods has been removed; callers no longer control serialization options.
- Pipeline configuration via YAML/JSON with `NodeName` and `NodeConfiguration` attributes for type discovery
- Reverse-ordered node delegation (middleware-style)

**Identity Provider DTOs**
- `IdentityProviderDto` — abstract base with JSON polymorphism via `[JsonDerivedType]`
- Concrete types: `GoogleIdentityProviderDto`, `FacebookIdentityProviderDto`, `MicrosoftIdentityProviderDto`, `AzureEntraIdProviderDto`, `MicrosoftAdProviderDto`, `OpenLdapProviderDto`, `OctoTenantIdentityProviderDto`
- `OctoTenantIdentityProviderDto` — cross-tenant authentication provider with `ParentTenantId` property
- `IdentityProviderTypesDto` enum discriminator: Google=0, Microsoft=1, MicrosoftAzureAd=2, MicrosoftActiveDirectory=3, OpenLdap=4, Facebook=5, OctoTenant=6
- Base DTO includes `AllowSelfRegistration` (bool) and `DefaultGroupRtId` (string?) properties

**Group DTOs**
- `GroupDto` — group with `Id`, `GroupName`, `GroupDescription`, `RoleIds`, `MemberUserIds`, `MemberExternalUserIds`, `MemberGroupIds`
- `CreateGroupDto` — creation DTO with `GroupName` (required), `GroupDescription`, `RoleIds`
- `UpdateGroupDto` — update DTO with `GroupName` (required), `GroupDescription`
- `GroupsResult` — wrapper for collection responses

**External Tenant User Mapping DTOs**
- `ExternalTenantUserMappingDto` — mapping with `Id`, `SourceTenantId`, `SourceUserId`, `SourceUserName`, `RoleIds`, `GroupNames`
- `CreateExternalTenantUserMappingDto` — creation DTO with `SourceTenantId`, `SourceUserId`, `SourceUserName` (all required), `RoleIds`
- `UpdateExternalTenantUserMappingDto` — update DTO with `RoleIds`

**Identity Services Client Methods**
- Groups: `GetGroups`, `GetGroup`, `GetGroupByName`, `CreateGroup`, `UpdateGroup`, `DeleteGroup`, `UpdateGroupRoles`, `AddUserToGroup`, `RemoveUserFromGroup`, `AddGroupToGroup`, `RemoveGroupFromGroup`
- External Tenant User Mappings: `GetExternalTenantUserMappings` (with skip/take/sourceTenantId), `GetExternalTenantUserMapping`, `CreateExternalTenantUserMapping`, `UpdateExternalTenantUserMapping`, `DeleteExternalTenantUserMapping`
- Admin Provisioning: `GetAdminProvisioningMappings`, `CreateAdminProvisioningMapping`, `ProvisionCurrentUser`, `DeleteAdminProvisioningMapping`

**Tenant-Scoped Service Client Routing**

Service clients that support tenant-scoped API routing have a `TenantId` property on their options class. `BuildServiceUri()` routes to `{tenantId}/v1` (tenant API). Clients that require `TenantId` throw `ServiceConfigurationMissingException` if it is not set.

| Client | Options Class | TenantId Required |
|--------|--------------|-------------------|
| `AssetServicesClient` | `AssetServiceClientOptions.TenantId` | Yes (required) |
| `IdentityServicesClient` | `IdentityServiceClientOptions.TenantId` | Yes (required) |
| `CommunicationServicesClient` | `CommunicationServiceClientOptions.TenantId` | Optional (falls back to `system/v1`) |
| `ReportingServicesClient` | `ReportingServicesClientOptions.TenantId` | Optional (falls back to `system/v1`) |
| `BotServicesClient` | `BotServiceClientOptions` | Not yet (system only) |

### Authenticator Client — `RequestClientCredentialsTokenAsync`

`AuthenticatorClient.RequestClientCredentialsTokenAsync` accepts optional `clientId` / `clientSecret` parameters that, when supplied, override the values configured on `AuthenticatorOptions`. This lets callers (e.g. `octo-cli`'s non-interactive login) authenticate as a different OAuth client without rebuilding the DI graph. When `AuthenticatorOptions.TenantId` is set, the method automatically appends `acr_values=tenant:{TenantId}` to the token request — same pattern as the device and refresh flows. The identity service's `OidcTenantResolutionMiddleware` reads this to scope the per-tenant `ClientStore` lookup.

**SignalR Communication**
- Bidirectional: Server-side `IAdapterHub` ↔ Client-side `IAdapterHubCallbacks`
- Adapter lifecycle: Register → Receive config → Pre-update notifications → Send results

**Source Generation**
- Input: `ck-*.yaml` and `ck-*.cache.json` Construction Kit model files
- Output: `Rt{TypeName}QueryDto.g.cs`, `Rt{TypeName}MutationDto.g.cs`, enum classes
- Generated namespace: `DataTransferObjects.{ModelId}.v{Version}`

**Pipeline Schema Generation**
- `AdapterBuilder` and `WebAdapterBuilder` support `--generate-pipeline-schema <output-path>` CLI parameter
- `NodeSchemaRegistry` discovers all registered pipeline nodes and generates a JSON Schema (`pipeline-schema.json`) describing available node configurations
- All enum values in the schema use CONSTANT_CASE format (e.g. `NOT_EQUALS`, `DATE_TIME`)
- MSBuild target `GeneratePipelineSchema` in `Sdk.Plug.Simulation.csproj` runs after Build using `dotnet exec "$(TargetPath)"` to auto-generate the schema
- Incremental: only regenerates when the binary changes
- Opt-out: set MSBuild property `GeneratePipelineSchema=false`

### Configuration

- Environment variables use `OCTO_` prefix
- Build configurations: `Debug`, `Release`, `DebugL` (local with version 999.0.0)
- Targets: `net10.0` and `netstandard2.0`
- `TreatWarningsAsErrors` is enabled globally

### Testing

- Unit tests: xUnit v3 with FakeItEasy for mocking, Bogus for test data
- System tests in `Sdk.ServiceClient.SystemTests` require running services
- **Pipeline parity suite** (`tests/Sdk.Common.PipelineParityTests/`): the authoritative Newtonsoft-oracle contract for STJ pipeline behaviour — read/write JSONPath parity, read-after-`Set`/multi-match operation parity (`OperationParityTests`), the overlay write-then-read predicate matrix (`OverlayWriteThenReadParityTests`), encoding byte-parity (`EncodingParityTests`, `UnsafeRelaxedJsonEscaping`), and attribute-value CLR-type round-trip parity. Newtonsoft defines correctness; documented irreducible divergences live in `AttributeValueParityCorpus`.
- **Allocation/memory gates** (`tests/Sdk.Common.Tests/EtlDataPipeline/`): `ForEachMemoryBenchmark`, `DataContextBigDocReadAllocationGate`, `TypedGetAllocationGate`, and `DataContextChildSelectMatchesTests` read process-wide GC counters, so they are isolated in the `[CollectionDefinition("AllocationGates", DisableParallelization = true)]` collection (`AllocationGatesCollection.cs`) with frozen absolute ceilings — relative ratios flaked under xUnit parallel execution.
