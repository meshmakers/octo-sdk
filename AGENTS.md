# AGENTS.md

Instructions for coding agents in this repository. Detail lives in `docs/`; read the matching file before changing the
code it covers.

## What this repo is

The Octo SDK (`octo-sdk`) is the public .NET library set that talks to the Octo Mesh Platform. It builds four packages:
`Meshmakers.Octo.Sdk.ServiceClient` (REST, SignalR and tenant GraphQL clients of the platform services),
`Meshmakers.Octo.Communication.Contracts` (DTOs, hub interfaces and message objects shared with the services),
`Meshmakers.Octo.Sdk.SourceGeneration` (the Roslyn generator for query and mutation DTOs and CK method clients) and
`Meshmakers.Octo.Sdk.Common` (instance secret encryption). The CLI, the MCP server and the services consume them, so a
public signature change reaches every consumer. The adapter framework and the ETL pipeline are in
`octo-communication-sdk`, not here.

## Read before you change

<!-- >>> generated: routing -->
| When you change | Read first |
|---|---|
| `src/Sdk.ServiceClient/ServiceClient.cs`, `**/*ClientOptions.cs`, `**/RtEntityToDtoMapper.cs`, `**/*.csproj`, `Octo.Sdk.sln` | `docs/architecture.md` |
| `src/Sdk.ServiceClient/{AssetRepositoryServices,TenantLifecycle}/**`, `tests/Sdk.ServiceClient.Tests/{AssetRepositoryServices,TenantLifecycle}/**` | `docs/asset-services-client.md` |
| `**/BotServices/**`, `**/AssetRepositoryServices/StreamData/**`, `**/ReadIdleTimeoutStream*.cs` | `docs/bot-and-stream-data-clients.md` |
| `**/CommunicationControllerServices/{I,}CommunicationServicesClient.cs` | `docs/communication-services-client.md` |
| `src/Sdk.ServiceClient/IdentityServices/**`, `tests/Sdk.ServiceClient.Tests/IdentityServices/**` | `docs/identity-services-client.md` |
| `src/Sdk.SourceGeneration/**`, `tests/Sdk.SourceGeneration.Tests/**`, `**/Tenants/Methods/**` | `docs/method-clients.md` |
| `**/SignalRClient.cs`, `**/CommunicationControllerServices/*HubClient.cs`, `**/Communication.Contracts/{Hubs,MessageObjects,DataTransferObjects}/**` | `docs/signalr-and-hubs.md` |
| `src/Sdk.SourceGeneration/**`, `tests/Sdk.SourceGeneration.Tests/**`, `**/Tenants/Methods/**` | `docs/source-generation.md` |
| `**/Authentication/**`, `**/AssetRepositoryServices/System/AssetServicesClient.cs`, `**/TenantScope/**` | `docs/tenant-tokens-and-authentication.md` |
<!-- <<< end generated: routing -->

Background, no path trigger: [docs/pipeline-and-adapter-framework.md](docs/pipeline-and-adapter-framework.md) (code
that lives in `octo-communication-sdk`), [docs/method-client-migration-guide.md](docs/method-client-migration-guide.md)
(per-wave migration from REST clients to CK method clients) and [docs/TEST-CONCEPT.md](docs/TEST-CONCEPT.md) (older
test strategy, partly outdated).

## Build & test

```bash
# Build using local NuGet packages (from ../nuget directory) - the default for local work
dotnet build Octo.Sdk.sln -c DebugL

# Build in Release mode against a published release (version must be passed explicitly)
dotnet build Octo.Sdk.sln -c Release -p:OctoVersion=3.5.1

# Run all tests
dotnet test Octo.Sdk.sln -c DebugL

# Run tests for a specific project
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL

# Run a single test
dotnet test -c DebugL --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

Versions come only from the pipeline: `Directory.Build.props` has no release fallback, so a Debug/Release build without
`-p:OctoVersion=X.Y.Z` (or `-p:OctoNugetPrivateServer=<feed>` for the main line `0.1.*`) fails fast with MSBuild error
`OCTO0001`. Use `-c DebugL` locally.

- Target frameworks: `net10.0`; the source generator targets `netstandard2.0`.
- `TreatWarningsAsErrors` is enabled globally.
- Unit tests use xUnit v3 with FakeItEasy for mocking and Bogus for test data. Wire-level client tests run against a
  loopback listener, not a real service.

## Before you commit

- Build and all tests are green (`dotnet test Octo.Sdk.sln -c DebugL`).
- The documentation changes with the code, in English: the doc named in the table above for the area you touched, the
  entry under `## Unreleased` in `CHANGELOG.md` for every change a package consumer can see, and
  [docs/method-clients.md](docs/method-clients.md) or
  [docs/method-client-migration-guide.md](docs/method-client-migration-guide.md) for method client and obsolescence
  changes. `Communication.Contracts` has no changelog: its change record is the routed doc of the area
  ([docs/signalr-and-hubs.md](docs/signalr-and-hubs.md) for hub and message contracts).
- Run `Test-OctoAgentDocs` (octo-tools) after changing `AGENTS.md` or `docs/`; `Test-OctoAgentDocs -Fix` regenerates
  the routing table. The shim file is generated: edit this file, not the shim.
- A change to a public type, DTO, hub contract or the generator needs the dependants rebuilt against it
  (`Invoke-BuildAll` in octo-tools); publish order for hub contracts is this repository first, then
  `octo-communication-sdk` and the communication controller
  ([docs/signalr-and-hubs.md](docs/signalr-and-hubs.md)).

## Rules

These rules bind new and changed code. Known exceptions in existing code are named in the linked doc.

- **This repository is public.** No work item ids, internal hostnames, customer or tenant names, or secrets in the
  documentation and in new or changed code comments and test names (work item ids stay in `CHANGELOG.md` entries).
  Remove an existing one when you touch its line.
- **Credentials never reach a log property, a request URL or a `DTO.ToString()`.** New client methods send passwords,
  secrets and stored secret hashes in the JSON body only; new DTOs that carry a secret override `ToString` without it.
  A generated secret is returned once, to the caller that asked for it (`ApiSecretDto.ValueClearText` from
  `CreateApiSecretForClient`). Known exceptions: hub payloads that deliver a credential by design (`LeaseDto`), and the
  SignalR `access_token` query parameter, which the protocol requires
  ([docs/identity-services-client.md](docs/identity-services-client.md),
  [docs/communication-services-client.md](docs/communication-services-client.md),
  [docs/signalr-and-hubs.md](docs/signalr-and-hubs.md)).
- **One token, one tenant.** New `AssetServicesClient` methods that address a tenant by name get their token through
  the private `CreateTenantScopeClientAsync` or `ResolveTenantAccessTokenAsync`, never send a token issued for another
  tenant and have no fallback to the original token. Known exceptions: `BotServicesClient`, the typed `TenantClient`
  and the clients with a `TenantId` option rely on the guard of the host
  ([docs/tenant-tokens-and-authentication.md](docs/tenant-tokens-and-authentication.md)).
- **Hidden attributes stay hidden.** `RtEntityToDtoMapper` skips attributes with `access: Hidden`, also inside records;
  there is no opt-out ([docs/architecture.md](docs/architecture.md)).
- **Additive wire contracts.** New members of existing DTOs and messages are optional with neutral defaults; a new hub
  method gets a default implementation, so an older service or client keeps working
  ([docs/signalr-and-hubs.md](docs/signalr-and-hubs.md)).
- **Obsolete only after consumers switched.** A REST method that a CK method client replaces becomes `[Obsolete]` only
  after the first-party consumers (octo-cli, octo-mcp-service) switched, so their warnings-as-errors builds stay green
  ([docs/method-client-migration-guide.md](docs/method-client-migration-guide.md)).
- **Generated code stays stable.** Never re-record a golden just to make a test pass; re-record only for an intended
  generator change or a fixture-model update ([docs/method-clients.md](docs/method-clients.md)).
- **Transfers stream and fail whole.** New upload and download code streams, applies a per-call timeout and a read-idle
  timeout, and never leaves a partial file under the target name (exception: the obsolete buffered
  `DownloadExportRtResultAsync`)
  ([docs/bot-and-stream-data-clients.md](docs/bot-and-stream-data-clients.md),
  [docs/asset-services-client.md](docs/asset-services-client.md)).
