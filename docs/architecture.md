---
description: Project structure of the SDK libraries, tenant-scoped service client routing and the RtEntityToDtoMapper access rules.
applies_to: src/Sdk.ServiceClient/ServiceClient.cs, **/*ClientOptions.cs, **/RtEntityToDtoMapper.cs, **/*.csproj, Octo.Sdk.sln
---

# Architecture and service client routing

## Project Structure

**Core Libraries (src/)**
- **Sdk.ServiceClient** - REST and SignalR client proxies for mesh services. Contains `ServiceClient` (REST via RestSharp), `SignalRClient<TOptions>` (real-time), and domain clients (`IdentityServicesClient`, `BotServicesClient`, etc.)
- **Sdk.Common** - Small shared helpers: instance secret encryption (`IInstanceSecretCrypto`) and `AssemblyMetadataReader`
- **Communication.Contracts** - Shared DTOs and SignalR hub interfaces (`IAdapterHub`, `IAdapterHubCallbacks`, `IAdapterPoolHub`, `IOperatorHub`)
- **Sdk.SourceGeneration** - C# incremental source generator that produces query/mutation DTOs from Construction Kit YAML schemas

The adapter framework, the ETL pipeline and the simulation adapter are not part of this repository; they live in
`octo-communication-sdk` (see [pipeline-and-adapter-framework.md](pipeline-and-adapter-framework.md)).

**Tenant-Scoped Service Client Routing**

Service clients that support tenant-scoped API routing have a `TenantId` property on their options class. `BuildServiceUri()` routes to `{tenantId}/v1` (tenant API). Clients that require `TenantId` throw `ServiceConfigurationMissingException` if it is not set. A new tenant-scoped client follows the same pattern: a `TenantId` option, required, routed to `{tenantId}/v1`; `BotServicesClient` takes the tenant per call on purpose ([bot-and-stream-data-clients.md](bot-and-stream-data-clients.md)).

| Client | Options Class | TenantId Required |
|--------|--------------|-------------------|
| `AssetServicesClient` | `AssetServiceClientOptions.TenantId` | Yes (required) |
| `IdentityServicesClient` | `IdentityServiceClientOptions.TenantId` | Yes (required) |
| `CommunicationServicesClient` | `CommunicationServiceClientOptions.TenantId` | Yes (required) |
| `ReportingServicesClient` | `ReportingServicesClientOptions.TenantId` | Yes (required) |
| `StreamDataServicesClient` | `StreamDataServiceClientOptions.TenantId` | Yes (required) |
| `AiServicesClient` | `AiServiceClientOptions.TenantId` | Yes (required) |
| `BotServicesClient` | `BotServiceClientOptions` | No option — tenant per call |

**Breaking change** — the enable/disable (and the whole StreamData) lifecycle
endpoints are tenant-scoped only. `CommunicationServicesClient`, `ReportingServicesClient`
and `StreamDataServicesClient` now **require** `TenantId` (they throw
`ServiceConfigurationMissingException` when it is blank) and route to `{tenantId}/v1`.
The former `system/v1` fallback and the `tenantId` query parameter on `Enable`/`Disable`
and on every StreamData archive/rollup/computed-column call have been removed; the tenant
travels in the URL path. `StreamDataServicesClient` moved from `api/v1` to `{tenantId}/v1`.
The `tenantId` method arguments are kept (used for `ArgumentValidation`) so call sites are
unchanged.

`ReconfigureLogLevelAsync` on the Communication and Reporting clients still targets the
**system-scoped** `DiagnosticsController` (`system/v1/diagnostics/reconfigureLogLevel`): it
builds that one request against an absolute system URL so the tenant-scoped client base URI
does not misroute it to `{tenantId}/v1/diagnostics` (which would 404).

## `YamlToJsonConverter` — bounded conversion of caller-supplied YAML

`YamlToJsonConverter` (`Communication.Contracts/Serialization`) converts a pipeline definition written in YAML into the
JSON document a JSON Schema validates; the Communication Controller's deploy-time validation and the MCP server's
pipeline validation use it on definitions a caller sends. Because YAML aliases share one node but the conversion copies
it to every use site, and because the YAML loader builds its tree recursively, the converter scans the token stream
iteratively before loading and refuses a document above the limits of `YamlToJsonConverterOptions` (default 200,000
nodes after alias expansion, 256 levels of nesting, no recursive or forward aliases) with
`YamlDocumentLimitExceededException`. The message names the limit and never quotes the document, so a host may log it or
return it. JSON input (`ToJsonNodeAutoDetect`) goes through `JsonNode.Parse`, whose default depth limit is 64 and which
has no aliases. Tests: `Communication.Contracts.Tests/Serialization/YamlToJsonConverterLimitsTests`.

## `RtEntityToDtoMapper` — Hidden attributes are never mapped

`RtEntityToDtoMapper` (`Communication.Contracts/DataTransferObjects`) is the SDK's entity-to-DTO mapper for services
that read entities from the tenant repository and return them (today: octo-mcp-service). The repository does not know
CK attribute access, so the mapper enforces it: an attribute with `access: Hidden` (password hashes, security stamps,
authenticator tokens of `System.Identity/User`) is skipped on the entity and inside records and record arrays
(`CkTypeAttributeGraph.IsReadableOutsideMethod`, the same check the engine's export converter `RtEntityToTcDtoConverter`
uses). A Hidden Secret is skipped too, without its read state. `MethodOnly` and `ReadOnly` attributes are readable and
stay mapped. There is no opt-out: a service that needs a Hidden value reads it from the entity itself and never returns
it. Tests: `Communication.Contracts.Tests/DataTransferObjects/RtEntityToDtoMapperAccessTests` (fixed dummy values, both
JSON serializers).
