# AGENTS.md

Instructions for coding agents in this repository. Detail lives in `docs/`; read the matching file before changing the code it covers.

## What this repo is

This is the **Octo SDK**, a .NET framework for building distributed mesh services with real-time communication, ETL data pipelines, and adaptive services.

## Read before you change

<!-- >>> generated: routing -->
| When you change | Read first |
|---|---|
| `src/Sdk.ServiceClient/ServiceClient.cs`, `src/Sdk.ServiceClient/**/*ClientOptions.cs`, `src/Communication.Contracts/DataTransferObjects/RtEntityToDtoMapper.cs`, `**/*.csproj`, `Octo.Sdk.sln` | `docs/architecture.md` |
| `src/Sdk.ServiceClient/AssetRepositoryServices/**`, `src/Sdk.ServiceClient/TenantLifecycle/**`, `tests/Sdk.ServiceClient.Tests/AssetRepositoryServices/**`, `tests/Sdk.ServiceClient.Tests/TenantLifecycle/**` | `docs/asset-services-client.md` |
| `src/Sdk.ServiceClient/BotServices/**`, `src/Sdk.ServiceClient/AssetRepositoryServices/StreamData/**`, `tests/Sdk.ServiceClient.Tests/BotServices/**` | `docs/bot-and-stream-data-clients.md` |
| `src/Sdk.ServiceClient/CommunicationControllerServices/CommunicationServicesClient.cs`, `src/Sdk.ServiceClient/CommunicationControllerServices/ICommunicationServicesClient.cs` | `docs/communication-services-client.md` |
| `src/Sdk.ServiceClient/IdentityServices/**`, `tests/Sdk.ServiceClient.Tests/IdentityServices/**` | `docs/identity-services-client.md` |
| `src/Sdk.SourceGeneration/**`, `tests/Sdk.SourceGeneration.Tests/**`, `src/Sdk.ServiceClient/AssetRepositoryServices/Tenants/Methods/**` | `docs/method-clients.md` |
| `src/Sdk.ServiceClient/SignalRClient.cs`, `src/Sdk.ServiceClient/CommunicationControllerServices/*HubClient.cs`, `src/Communication.Contracts/Hubs/**`, `src/Communication.Contracts/MessageObjects/**` | `docs/signalr-and-hubs.md` |
| `src/Sdk.SourceGeneration/**`, `tests/Sdk.SourceGeneration.Tests/**`, `src/Sdk.ServiceClient/AssetRepositoryServices/Tenants/Methods/**` | `docs/source-generation.md` |
| `src/Sdk.ServiceClient/Authentication/**`, `tests/Sdk.ServiceClient.Tests/Authentication/**` | `docs/tenant-tokens-and-authentication.md` |
<!-- <<< end generated: routing -->

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

Versions come only from the pipeline (AB#6291): `Directory.Build.props` has no release fallback, so a
Debug/Release build without `-p:OctoVersion=X.Y.Z` (or `-p:OctoNugetPrivateServer=<feed>` for the main
line `0.1.*`) fails fast with MSBuild error `OCTO0001`. Use `-c DebugL` locally.

### Configuration

- Environment variables use `OCTO_` prefix
- Build configurations: `Debug`, `Release`, `DebugL` (local with version 999.0.0)
- Targets: `net10.0` and `netstandard2.0`
- `TreatWarningsAsErrors` is enabled globally

### Testing

- Unit tests: xUnit v3 with FakeItEasy for mocking, Bogus for test data
- System tests in `Sdk.ServiceClient.SystemTests` require running services

## Before you commit

## Rules
