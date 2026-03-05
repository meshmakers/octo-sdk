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
- `IDataContext` provides mutable JSON-based data context (JToken)
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
