# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

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

## Architecture Overview

This is the **Octo SDK**, a .NET framework for building distributed mesh services with real-time communication, ETL data pipelines, and adaptive services.

### Project Structure

**Core Libraries (src/)**
- **Sdk.ServiceClient** - REST and SignalR client proxies for mesh services. Contains `ServiceClient` (REST via RestSharp), `SignalRClient<TOptions>` (real-time), and domain clients (`IdentityServicesClient`, `BotServicesClient`, etc.)
- **Sdk.Common** - Adapter framework with ETL pipeline orchestration. Key: `IAdapterService` interface, `AdapterBuilder`, `EtlDataOrchestrator`, `IPipelineNode`
- **Sdk.CommunicationAdapter** - Adapter hosting infrastructure using .NET Generic Host with DI
- **Communication.Contracts** - Shared DTOs and SignalR hub interfaces (`IAdapterHub`, `IAdapterHubCallbacks`, `IPoolHub`)
- **Sdk.SourceGeneration** - C# incremental source generator that produces query/mutation DTOs from Construction Kit YAML schemas
  - Range-retaining models (CK v2, compiler default since AB#6675) reference dependencies major-qualified (`System@2/Name-1`) while the restored compile cache holds concrete ids (`System-2.5.0/Name-1`). `CkDtoModelBinder` (AB#6715) binds the references to the cache before any DTO is generated (same step as the engine's model generator); a reference that stays unbound is reported as `OM1010`. Tests: `RangeRetentionDtoGenerationTests` (fixtures in `tests/Sdk.SourceGeneration.Tests/Fixtures` are real `octo-ckc Compile` output).
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
- Groups: `GetGroups`, `GetGroup`, `GetGroupByName`, `CreateGroup`, `UpdateGroup`, `DeleteGroup`, `UpdateGroupRoles`, `AddUserToGroup`, `RemoveUserFromGroup`, `AddGroupToGroup`, `RemoveGroupFromGroup`, `AddClientToGroup`, `RemoveClientFromGroup`
- Client roles (AB#4183): `GetClientDirectRoles`, `UpdateClientRoles`, `AddRoleToClient`, `RemoveRoleFromClient`. `GroupDto` carries `MemberClientIds`; `ClientDto` carries `RtId` (read-only)
- External Tenant User Mappings: `GetExternalTenantUserMappings` (with skip/take/sourceTenantId), `GetExternalTenantUserMapping`, `CreateExternalTenantUserMapping`, `UpdateExternalTenantUserMapping`, `DeleteExternalTenantUserMapping`
- Admin Provisioning: `GetAdminProvisioningMappings`, `CreateAdminProvisioningMapping`, `ProvisionCurrentUser`, `DeleteAdminProvisioningMapping`

**CK Model Catalog DTOs (LibraryStatus / CheckUpgrade, AB#6868)**
- `CkModelLibraryStatusItemDto` carries the tenant upgrade guard result of the offered catalog version (read-only on this DTO; the import calls below enforce it): `IsBlocked`, `Blockers` (`CkUpgradeBlockerDto`: `Dependent`, `Update`, `Kind`, `Subject`, `Code`, `Message`, `DependentIsInUpdateSet`, `SuggestedDependentUpdate`, `ResolveHint`) and `IsRangeRetaining` (installed model declares dependency ranges). A blocked row keeps `HasUpdate = true` and has `NeedsAction = false`.
- `UpgradeCheckResponseDto` carries `IsBlocked` and `Blockers` as well; `HasBreakingChanges` stays the model's own migration verdict.

**CK Model Catalog import enforcement (tenant upgrade guard, AB#6870)**
- `ImportFromCatalogRequestDto` and `ImportFromCatalogBatchRequestDto` carry `ForceBreakingUpdate` (default `false`; override of the guard, needs the data model management policy; ignored by the read-only calls). `DependencyResolutionResponseDto` (one tree) and `BatchDependencyResolutionResponseDto` (`ModelsToImport` as one set) carry `IsBlocked` / `Blockers` so a preview agrees with the refusal.
- `IAssetServicesClient.ImportFromCatalogAsync` (new, single model, returns `TransferModelResponseDto`) and `ImportFromCatalogBatchAsync` turn the service's `409` problem details with `code = CK_UPGRADE_BLOCKED` into `CkUpgradeBlockedException : ServiceClientResultException` (`Blockers`, `UpdateSet`, `Detail`); another 409 stays a plain `ServiceClientResultException`. A batch is refused as a whole.
- Tests: `ImportFromCatalogUpgradeGuardTests` (loopback service).
- **File import (AB#6966)**: `ImportCkModelAsync(tenantId, path)` now also throws `CkUpgradeBlockedException` when the service refuses the file (a breaking
  file, `System` included); the additive overload `ImportCkModelAsync(tenantId, path, bool forceBreakingUpdate)` sends the form member
  `forceBreakingUpdate=true` next to the file (needs the data model management policy, else 403). Both go through the tenant-scope client like
  `ImportFromCatalogAsync`. An older service ignores the member (and does not guard). `CheckUpgradeAsync` already carries `IsBlocked` / `Blockers`
  and, since the service change, judges `System` too. Tests: `ImportFromCatalogUpgradeGuardTests` (`ImportCkModelAsync_*`; `LoopbackHttpService.Bodies`).
- All new members are optional on the wire: against an older asset repository they keep their neutral defaults (`false` / empty), and older clients ignore them. `Kind` is a string and may gain values.

**Blueprint apply enforcement (tenant upgrade guard, AB#6871)**
- `BlueprintApplyRequestDto` and `BlueprintUpdateRequestDto` carry `ForceBreakingUpdate` (default `false`; needs the data model management policy; the install's `Force` still only re-applies seed data). `BlueprintUpdatePreviewDto` and `BlueprintUpdateResultDto` carry `IsBlocked`, `CkUpdateSet` and `Blockers` (`CkUpgradeBlockerDto`, the entries of the library status); `BlueprintApplyResultDto` carries `CkUpdateSet` and the forced `Blockers`.
- `IAssetServicesClient.ApplyBlueprintAsync(tenantId, BlueprintApplyRequestDto)` (new overload; the `(tenantId, blueprintId, force)` overload calls it) and `ApplyBlueprintUpdateAsync` turn the service's `409` with `code = CK_UPGRADE_BLOCKED` into `CkUpgradeBlockedException` (same type as the imports); another 409 stays a `ServiceClientResultException`. The preview and a dry run answer 200 with `IsBlocked`.
- Tests: `BlueprintUpgradeGuardClientTests` (loopback service). Members are optional on the wire; against an older service they read as `false` / empty.

**Tenant-Scoped Service Client Routing**

Service clients that support tenant-scoped API routing have a `TenantId` property on their options class. `BuildServiceUri()` routes to `{tenantId}/v1` (tenant API). Clients that require `TenantId` throw `ServiceConfigurationMissingException` if it is not set.

| Client | Options Class | TenantId Required |
|--------|--------------|-------------------|
| `AssetServicesClient` | `AssetServiceClientOptions.TenantId` | Yes (required) |
| `IdentityServicesClient` | `IdentityServiceClientOptions.TenantId` | Yes (required) |
| `CommunicationServicesClient` | `CommunicationServiceClientOptions.TenantId` | Yes (required — AB#4287) |
| `ReportingServicesClient` | `ReportingServicesClientOptions.TenantId` | Yes (required — AB#4287) |
| `StreamDataServicesClient` | `StreamDataServiceClientOptions.TenantId` | Yes (required — AB#4287) |
| `BotServicesClient` | `BotServiceClientOptions` | No option — tenant per call (AB#5060) |

**AB#4287 breaking change** — the enable/disable (and the whole StreamData) lifecycle
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

### Bot Services Client — tenant job routes (AB#5060)

`BotServicesClient` is the one tenant-addressed client **without** a `TenantId` option, deliberately.
Its five tenant-scoped job verbs — `StartDumpRepositoryAsync`, `RestoreRepositoryWithTusAsync`,
`StartExportArchiveDataAsync`, `StartImportArchiveDataWithTusAsync`, `StartRunFixupScriptAsync` —
build an absolute `{tenantId}/v1/jobs/…` URL **per call** from their own first argument
(`BuildTenantJobUri`), instead of routing through the cached base URI. Three reasons:

- **The target tenant changes per call.** Backing up a *child* tenant with the parent
  administrator's token is what the tenant routes were added for (the controller carries
  `[AllowParentTenantAdministration]`), and `octo-cli` passes the target as its own `-tid` argument.
- **`ServiceUri` is computed once and cached** (`ServiceClient.cs`), and `octo-cli` registers
  `IBotServicesClient` as a **singleton** — an options tenant would freeze the first tenant seen for
  the process lifetime, and a single-tenant unit test would not notice.
- **Public signatures stay unchanged**, so consumers migrate by taking the package, not by editing
  call sites.

What deliberately stays on `system/v1` (the client's base URI, unchanged): `GetImportJobStatus`,
both download variants and `ReconfigureLogLevelAsync` — they address a job instance or the service
process, not a tenant.

The **tus upload sink** moved onto the tenant route in stage 3 (`BuildTusUploadUri` →
`{tenantId}/v1/tus-upload`). It used to be `system/v1/tus-upload` with the tenant sent as upload
metadata, which the service's transport gate never saw and which bound nothing — the file was stored
flat under its tus file id and no consumer read the metadata back. The service now stages uploads
under the tenant's own directory. Both tus methods therefore build *two* tenant URLs in one body,
the upload and the job start; they are pinned separately in
`tests/Sdk.ServiceClient.Tests/BotServices/` because a regression to the system path would be a
silently ungated upload. `tests/Sdk.ServiceClient.Tests/BotServices/` pins all of it against
a loopback `HttpListener` (`LoopbackHttpService`) — the only way to observe the URL, since
`ServiceClient` builds its own `RestClient` and the tus flow uses a bare `HttpClient`. That fixture
is a **class fixture**: `HttpListener.Prefixes.Add` costs ~5 s per instance on macOS.

### Bot Services Client — tus upload in parts (AB#6786 / AB#6970)

`RestoreRepositoryWithTusAsync` and `StartImportArchiveDataWithTusAsync` (octo-cli `Restore` / `ImportArchiveData`,
and the MCP tools, which call these two methods) upload through the private `TusUploader` and never put more than
`BotServiceClientOptions.TusPartSizeBytes` into one request. The upload itself is created with BirdMessenger
(`TusCreateAsync`); the data goes out with our own `HEAD` / `PATCH` loop.

- **Why not BirdMessenger's `TusPatchAsync`:** its default upload type streams the whole file in **one** `PATCH`,
  whatever `UploadBufferSize` says. The ingress of staging-1, prod-1 and prod-2 refuses a body above 100m with 413 and
  BirdMessenger did not report it, so octo-cli failed with "Upload file for tus file ID … is empty (0 bytes)" on a
  3.48 GB dump. Do not go back to it.
- **Part size:** `TusPartSizeBytes`, default **50 MiB** (as Refinery Studio), accepted range 1 MiB to 90 MiB. A value
  outside it throws `ArgumentOutOfRangeException` when it is set (bound from configuration or assigned), because a part
  above the ingress body limit (100 MiB, 300 MiB on test-2) is refused with 413 and never reaches the service.
- **Parts and offsets:** parts are aligned to multiples of the part size (also after a resume from an odd offset), so
  the part numbers are exact and no body is larger than the part size. The first request is a `HEAD`, so an upload that
  already holds bytes continues at the server's offset. The body is streamed from the file with a known
  `Content-Length`; `Expect: 100-continue` lets an ingress answer 413 before the body is sent.
- **Failures:** a refusal (any 4xx except 408/409) throws `TusUploadException` at once with `StatusCode`, `PartNumber`,
  `PartCount`, `Offset` and `PartSizeBytes` in the exception, and no file content or response body in the message; 413
  names `TusPartSizeBytes` as the remedy. A lost connection, a timeout (30 min per request), 5xx, 408 and 409 are retried
  up to five times with a growing delay; before each retry the client reads the server's offset (`HEAD`) and continues
  from there, so nothing already stored is sent again. A connection closed while the body is sent is reported as such
  (`StatusCode` null) with the same hint, because an ingress may close instead of answering.
- **Token:** the bearer token is read from the access token object for every `HEAD` and `PATCH`, so a token renewed during a long
  upload is used from then on. A `HEAD` answered 5xx/408 while resuming counts as a failed attempt, like a lost connection.
- **Progress:** `progressCallback` receives the fraction of the file delivered, about once per MiB.
- Tests: `BotServicesClientTusUploadTests` against `TusTestServer` (loopback tus server with a request body limit,
  offset and running hash): 1 GiB through a 100 MiB limit with the defaults (21 parts, identical file), body size and
  continuous offsets for both methods, 413 with part and offset, resume after a lost answer, 503 retry, option range.

### Bot Services Client — dump download is all or nothing (AB#6969)

`DownloadDumpToFileAsync` (used by octo-cli `Dump`, `ExportArchiveData` and the export commands) writes the body to
`<target>.<guid>.part` next to the target and moves it over the target only after the whole body arrived. A connection that
breaks off (reset, or a clean end before the announced `Content-Length`) throws an `IOException` ("The download of job '…' broke
off after N bytes: …"), deletes the part file and leaves an existing target untouched; octo-cli ends with a non-zero exit code.
Before this fix the target was opened with `FileMode.Create` and a broken download left the truncated file under its real name
(and destroyed a previous file of that name). A response without `Content-Length` (chunked) can only fail on a broken
connection; a complete chunked end cannot be told from success. Tests: `BotServicesClientDownloadTests` (raw TCP server that
announces 8 MiB and sends 3 MiB, then FIN or RST).

### Bot Services Client — restore outcome in `JobDto` (AB#6969)

`GetImportJobStatus` returns `JobDto` with optional `Outcome` (`Succeeded` | `SucceededWithWarnings`, the job status stays `Succeeded`),
`FailedArchives` (`JobFailedArchiveDto`: `ArchiveRtId`, `Reason`) and `NotCheckedRollups` (`JobNotCheckedRollupDto`: `RollupRtId`,
`Cause` NoLiveTable|NeverActivated|SourceMissing|ArchiveFailed, `Reason`, `IsBlocking`; false only for NeverActivated). Derived `HasWarnings`
is true for `SucceededWithWarnings`, a failed archive or a blocking rollup. All additive: an older bot leaves them `null`. Tests:
`BotServicesClientTests` (`GetImportJobStatus_*`).

### Stream Data Services Client — rollup validate and repair (AB#6968)

`IStreamDataServicesClient` has four methods for the operator check after a restore:

- `ValidateRollupsAsync(tenantId, rollupRtIds?)` (`GET {tenant}/v1/streamdata/rollups/validate`, repeated `rollupRtId`, none = all
  rollups) returns `RollupConsistencyReportDto`. Read only.
- `PlanRollupRepairAsync(tenantId, rollupRtIds?)` (`POST …/rollups/repair?dryRun=true`) returns `RollupRepairReportDto`: the planned
  steps per rollup, nothing changed.
- `EnqueueRollupRepairAsync(tenantId, rollupRtIds?)` (`POST …/rollups/repair?dryRun=false`, **202**) returns `RollupRepairRequestDto`
  (`RequestId`, `State` Pending|Running|Done|Failed, `PlannedActions`, `Report` once finished). **The call runs nothing**: the server
  executes the repair in the background, so a large recompute is never bound to an HTTP request; a second request while one is active
  returns the active one.
- `GetRollupRepairRequestAsync(tenantId, requestId)` (`GET …/rollups/repair/{requestId}`) follows the request until `IsFinished`.

Check, kind, status and state are strings so a value this client does not know still deserializes.
`RollupConsistencyEntryDto` carries `IsNotChecked`, `IsBlockedByNotChecked` and `NotChecked` (`RollupNotCheckedDto`: `Cause` NoLiveTable|NeverActivated|SourceMissing, `Reason`, `IsBlocking`), `RollupConsistencyReportDto` carries `HasNotChecked` (any) and `HasBlockingNotChecked`: a rollup the validator could not check is never silently ok. A blocking gap makes `IsOk` false; `NeverActivated` is informational (`IsBlocking` false) and keeps `IsOk` true. All additive with neutral defaults, so an older server reads as `false`/`null`. Consumers: octo-cli
`ValidateRollups` / `RepairRollups` / `GetRollupRepairStatus`, MCP `validate_rollups` / `repair_rollups` / `get_rollup_repair_status`.
Tests: `RollupConsistencyClientTests` (loopback).

The deprecated `system/v1/jobs/…?tenantId=` actions are no longer addressed by the SDK at all, and
stage three of AB#5060 removed them from the service. Do not reintroduce a call to them.

### Communication Services Client — `RotateServiceAccountSecretAsync` (AB#5032 / AB#5048)

`ICommunicationServicesClient.RotateServiceAccountSecretAsync(adapterRtId)` backs
`POST {tenantId}/v1/adapter/{adapterRtId}/serviceAccount/rotateSecret` and replaces the client
secret of the adapter's pipeline service account. Three things about it are deliberate:

- **The route takes a plain runtime object ID**, not a composite `RtEntityId` like the adapter read
  endpoints. `Adapter` is polymorphic, so the controller resolves it through the tenant's adapter
  list rather than by `RtEntityId`.
- **`RotateServiceAccountSecretResultDto` carries no secret**, mirroring the controller DTO
  one-to-one. The plaintext lives in exactly two places (the tenant's `ServiceAccountConfiguration`
  entity and the identity client's hash); returning it would add a third that ends up in proxy logs,
  shell history and CI output. `Communication.Contracts.Tests` pins that, the same way the
  controller pins it server-side — do not add a convenience property.
- **`RequiresPipelineRedeploy` must be surfaced, not swallowed.** The adapter freezes the
  credentials into the pipeline's `GlobalConfiguration` at *pipeline registration* and never
  refreshes them, so a running pipeline keeps presenting the withdrawn secret until its data flows
  are redeployed. A caller that drops the flag produces "rotation done, still broken".

A 2xx without a body throws instead of returning a default DTO: every other null-object fallback in
this client stands for an empty-but-valid state, whereas an undescribable rotation would be reported
to the user as "nothing left to do" for a secret that was in fact replaced.

Rotation cannot be done from a blueprint — the secret attribute is runtime state, so blueprint
import/export no longer touches a live secret. This client method (and `octo-cli`'s
`RotateAdapterServiceAccountSecret`) is the supported path.

### Communication Services Client — `SwitchServiceAccountsToImpersonationAsync` (AB#5537 / AB#6642)

`ICommunicationServicesClient.SwitchServiceAccountsToImpersonationAsync(apply, redeployDataFlows)`
backs `POST {tenantId}/v1/serviceAccount/switchToImpersonation?apply=..&redeployDataFlows=..` and
returns `ServiceAccountImpersonationSwitchReportDto` (mirror of the controller's report; no secret
or token in any member — pinned in `Communication.Contracts.Tests`). `apply=false` is a dry run.
`apply=true` clears stored secrets and invalidates the old ones, so callers confirm first; the server
answers HTTP 409 (`ServiceClientResultException.HttpStatusCode`) unless secret clearing is allowed
on the installation. A 2xx without a body throws, like the rotation. Consumer: `octo-cli`
`SwitchServiceAccountsToImpersonation`.

### Communication Services Client — the adapter pool queue (AB#4924 §10)

`GetAdapterPoolQueueAsync(adapterPoolRtId)` and `CancelQueuedExecutionAsync(adapterPoolRtId,
executionId)` back `GET`/`DELETE {tenantId}/v1/adapterPool/{id}/queue[/{executionId}]`. The leasing
concept (§5) requires the queue to be operable from Refinery Studio, `octo-cli` **and** the MCP
server — the plan lists those three repos, but both the CLI and the MCP server reach the controller
through this client, so the contract lives here. Three things are deliberate:

- **`AdapterPoolQueueEntryDto` has no global rank field, and adding one would be a lie.** The pool
  serves borrowing tenants round-robin, so the truthful answer is `PositionInTenant` +
  `TenantsAheadInRotation`: item 1 of the tenant whose turn is next runs before item 2 of the tenant
  being served now. `Communication.Contracts.Tests` pins the absence of a rank-shaped member the
  same way it pins "no secret" on the rotation result — it is the field a well-meaning change adds
  first, because it is the number a user asks for.
- **`ExecutionClass` is an `int`, not an enum**, for the same reason as
  `NodeDescriptorDto.ExecutionClass`: a value a reading side does not know must deserialize rather
  than throw.
- **`CancelQueuedExecutionAsync` turns `409` and `404` into outcomes, not exceptions.**
  `AdapterPoolQueueCancellationOutcome.AlreadyLeased` means the execution is already running and
  nothing was cancelled — stopping it is *interrupting a running pipeline*, a different operation
  (concept §5). Every surface has to be able to say which of the two it just did not do; an
  exception would collapse that into "the call failed, try again". Anything else still throws.

`AdapterPoolQueueEntryDto` is the **single** declaration of that wire shape: the controller
serialises this very type. Increment 7 had a controller-local copy because no client existed yet, and
increment 8 deleted it — two declarations of one shape drift, and a second one named identically also
collided inside the controller, which imports both namespaces (`CS0104`). The derived `IsLeased` is
`[JsonIgnore]`d for that reason: the controller must not gain a response field from a convenience
property. `Communication.Contracts.Tests` deserialises the controller's exact wire shape, so a silent
drift fails here rather than in a surface.

### Asset Services Client — files API (AB#6229, epic AB#6171)

`IAssetServicesClient.Files` (`IFilesClient`, implementation `AssetRepositoryServices/Files/FilesClient.cs`)
is the typed client of the platform file system (System.Files). It shares the asset client's endpoint,
`TenantId` and access token and is created lazily, once per `AssetServicesClient`.

- **Bytes over REST** `{endpoint}/{tenant}/v1/files/…`: `GetCapabilitiesAsync`, `UploadAsync(root, path, stream,
  contentType, conflict, createFolders)` (`PUT {root}/{segments}`), `UploadToFolderAsync(folderRtId, name, …)`
  (`POST items/{rtId}/content?name=`), `ReplaceContentAsync(fileRtId, …)` (`PUT items/{rtId}/content`),
  `DownloadAsync(root, path)` / `DownloadByIdAsync(rtId)` / `DownloadZipAsync(ZipRequestDto | rtIds)` → `FileDownload`
  (stream + file name + content type + length + ETag; dispose it), `GetFolderStatsAsync(root, path)` /
  `GetFolderStatsByIdAsync(rtId)`, `GetLinkedCountsAsync(rtIds)` (`POST linked-counts`: links per entry = every
  association except System/ParentChild, any role/direction; hidden entries omitted; > 500 rtIds sent in batches of
  500). `FilesCapabilitiesDto` carries `Available`/`Reason` (`SYSTEM_FILES_MISSING` when the tenant has no
  System.Files; an older server omits both and `Available` reads true), `MaxDeleteEntries` and `LinkedCounts`.
  Conflict query values are `fail|replace|keepBoth`, `createFolders=true` only when set.
- **Metadata over the tenant GraphQL** `{endpoint}/tenants/{tenant}/GraphQL` (decision Q1, no REST for metadata):
  `ListRootsAsync`, `GetEntryAsync(root, path)`, `GetEntryByIdAsync`, `ListChildrenAsync(root, path)` /
  `ListChildrenByIdAsync` (paged, `first`/`after`), `CreateRootAsync(name, wellKnownName)` (role `FileManagement`),
  `CreateFolderAsync(root, parentPath, name)` / `CreateFolderInAsync(parentRtId, name)`, `RenameAsync`, `MoveAsync`
  (`parent` DELETE of the **first** current parent + CREATE of the target in one mutation — the server enforces one
  parent per entry, INVALID_REQUEST otherwise), `DeleteAsync` (`runtimeEntities.delete`, recursive server-side, all or nothing),
  `GetDeleteImpactAsync` (containers: entry + REST folder stats; files: `LinkedCount` via linked-counts, `Stats` null —
  the stats endpoint is folder-only; `LinkedCount` null when the server has no linked-counts endpoint).
  Path resolution mirrors the server: `MATCH_REG_EX "(?i)^name$"` per segment, an exact (ordinal) match wins, none →
  `PATH_NOT_FOUND`/`ROOT_NOT_FOUND`, several → `AMBIGUOUS_PATH`. The lookup (`ChildLookupQuery`) sends **no
  `sortOrder`**: the engine's sorted association path with `fieldFilter` + `first` counts the unfiltered associations
  as totalCount and answers INCOMPLETE_SLICE once a folder has more than one child. Plain listings stay sorted by name.
- **Timeouts and connections:** the `HttpClient` has no timeout of its own; every call gets a per-call timeout —
  `AssetServiceClientOptions.FileTransferTimeout` (default 1 h, ≤ 0 = none) for uploads (whole body) and downloads/zip
  (until the response headers; the body stream is never cut off), `MaxTimeout` for metadata, stats and capabilities.
  A timeout throws `TimeoutException`, a caller cancellation stays `OperationCanceledException`. The default
  constructor uses a process-wide `HttpClient` over one `SocketsHttpHandler` (`PooledConnectionLifetime` 5 min; one per
  `ServerCertificateTrust` mode), because the MCP server creates an asset client per tool call; `Dispose` does not
  close it. `AssetServicesClient.Files` is a `Lazy<FilesClient>` (thread-safe).
- **Deliberately a plain `HttpClient`, not RestSharp and not `TenantClient`:** uploads/downloads must stream (100 MB),
  the upload body must not dispose the caller's stream (`UploadContent`, not `StreamContent`), and the error codes
  must reach the caller — RestSharp buffers, and `TenantClient.CheckResult` flattens GraphQL errors to text and drops
  `extensions.code`. The bearer token is read from the token object per request.
- **Errors:** `FilesApiException : ServiceClientResultException` with `Code` (`FileErrorCodes`: ROOT_NOT_FOUND,
  PATH_NOT_FOUND, AMBIGUOUS_PATH, NAME_CONFLICT, INVALID_NAME, RESERVED_NAME, PROTECTED_ROOT, FORBIDDEN, NOT_A_FOLDER,
  NOT_A_FILE, MOVE_INTO_ITSELF, LIMIT_EXCEEDED, INVALID_REQUEST, UNKNOWN), `HttpStatusCode`, `Detail`, `IsGraphQlError`.
  REST: problem-details `code`, else a code-shaped `title`, else derived from the status (an ingress HTML 413 →
  LIMIT_EXCEEDED; a 400 without code → INVALID_REQUEST). GraphQL (HTTP 200 or 400): `errors[0].extensions.code`,
  status derived from the code — LIMIT_EXCEEDED is the delete cap there (MaxDeleteEntries, default 2,000 entries) and
  maps to **409, not 413**. GraphQL codes of the file guards: NAME_CONFLICT, INVALID_NAME, RESERVED_NAME,
  PROTECTED_ROOT, MOVE_INTO_ITSELF, FORBIDDEN (root creation without `FileManagement`, and data-permission denial
  on create/update/delete of System.Files entities, no rtIds named), INVALID_REQUEST (single-parent
  rule on moves), LIMIT_EXCEEDED (delete cap). 401 → `UnauthorizedServiceAccessException`.
- **Client-side guards:** every path segment is URL-encoded on its own, empty segments are dropped (like the server),
  `.`/`..` segments throw `ArgumentException` before anything is sent, roots named like the literal route segments
  (`capabilities`, `zip`, `items`, `stats`) are refused on path routes, rtIds must be 24 hex characters.
- **Download file name:** `ContentDispositionParser` reads `filename*` (RFC 5987, UTF-8 / ISO-8859-1) first, then
  `filename`; a malformed `filename*` falls back to `filename`.

Tests: `tests/Sdk.ServiceClient.Tests/AssetRepositoryServices/Files/` (recording `HttpMessageHandler`, no server).

### Per-tenant tokens — guard and exchange (AB#6836)

A command that addresses tenant X never sends a token whose `tenant_id` claim is not X. Authority of a
root-tenant operator in a child tenant comes only from explicit provisioning there
(`ProvisionCurrentUser -ttid X`, also for new tenants); identity gives ancestor users no implicit admin role.

- **Choke point:** `AssetServicesClient.CreateTenantScopeClientAsync(tenantId)` builds every client that
  addresses a tenant by name (library status, catalog import, blueprints). It reads the token's `tenant_id`
  (`AccessTokenInspector`, no signature check, the service validates). Equal: token used as before, no exchange.
  Other tenant: exchanged through `ITenantTokenProvider`; without a provider the call fails with
  `TenantTokenMismatchException` before anything is sent. A token whose tenant cannot be read (blank, opaque)
  behaves as before. There is **no fallback** to the original token.
- **Exchange:** `AuthenticatorClient.ExchangeTokenForTenantAsync` (`ITokenExchangeClient`) posts RFC 8693
  (`acr_values=tenant:X`, client from the authenticator options, no refresh token in the answer). Same checks
  as octo-mcp-service `TenantTokenExchanger` (inputs present, successful answer, access token present) plus:
  the issued token must carry the requested tenant.
- **Cache:** `TenantTokenProvider` keeps one token per tenant in an `ITenantTokenStore` (default in memory),
  renews 60 s before expiry and when the subject token changed, silently, from the subject token.
  A host may persist the store where it already protects the subject token (octo-cli does).
- **Denial:** `TenantTokenExchangeDeniedException` carries `Cause` (`TenantTokenDenialCause`) and names the
  tenant and, for not provisioned, the step `ProvisionCurrentUser -ttid X`. `AuthenticatorClient.MapExchangeError`
  maps the OAuth errors of identity (`unauthorized_client` with "cross-tenant" = access denied, without = client
  lacks the grant; `invalid_grant` on `subject_token` = sign in again; `tenant_not_provisioned` once identity
  sends it, AB#6839). A 403 on a tenant scope call becomes `Cause=Forbidden` with the same hint. Messages never
  contain token content.
- Other clients that take the tenant in the route (`BotServicesClient` tenant jobs/secrets, communication, the typed GraphQL
  `TenantClient`) use the tenant of their options and are guarded by the host (octo-cli checks token tenant vs
  context tenant); they are not routed through the exchange.
- Tests: `Authentication/TenantTokenProviderTests`, `TenantTokenExchangeRequestTests`,
  `AssetRepositoryServices/TenantScope/TenantScopeTokenTests` (wire level, loopback).

### Raw GraphQL on a tenant — `SendGraphQlAsync` (AB#7027)

`IAssetServicesClient.SendGraphQlAsync(tenantId, query, variablesJson?, operationName?, ct)` posts one GraphQL document to
`{endpoint}/tenants/{tenantId}/GraphQL` and returns `GraphQlRawResponse` (`Content` = the body exactly as the service sent
it, `StatusCode`, derived `HasErrors` = non-empty top-level `errors` array). It exists for callers that want the untouched
JSON (octo-cli `GraphQl`, scripts) and not a typed DTO; typed use stays `ITenantClient.SendQueryAsync<TDto>`.

- **Same choke point as the REST routes (AB#6836):** the token comes from `ResolveTenantAccessTokenAsync`, which
  `CreateTenantScopeClientAsync` uses too. A token issued for another tenant is exchanged (cached per tenant) or, without a
  provider, refused with `TenantTokenMismatchException` before anything is sent. No fallback to the original token. The
  typed `TenantClient` is not routed through the exchange; this method is the supported way to reach a child tenant's GraphQL.
- **Errors are answers:** a body with GraphQL errors comes back as a result with `HasErrors = true`, with HTTP 200 and with 400
  (a rejected document). 401 throws `UnauthorizedServiceAccessException`, 403 `TenantTokenExchangeDeniedException`
  (`Forbidden`, names the tenant and `ProvisionCurrentUser -ttid`), any other failure without a GraphQL body a
  `ServiceClientResultException`.
- `variablesJson` must be a JSON object, else `ArgumentException` before any request; the tenant id is URL-escaped in the route.
- Tests: `AssetRepositoryServices/TenantScope/GraphQlRawRequestTests` (loopback listener: route, body, untouched answer,
  exchanged tenant token on the wire, canary: neither the context token nor its text appears in any exception).

### Authenticator Client — `RequestClientCredentialsTokenAsync`

`AuthenticatorClient.RequestClientCredentialsTokenAsync` accepts optional `clientId` / `clientSecret` parameters that, when supplied, override the values configured on `AuthenticatorOptions`. This lets callers (e.g. `octo-cli`'s non-interactive login) authenticate as a different OAuth client without rebuilding the DI graph. When `AuthenticatorOptions.TenantId` is set, the method automatically appends `acr_values=tenant:{TenantId}` to the token request — same pattern as the device and refresh flows. The identity service's `OidcTenantResolutionMiddleware` reads this to scope the per-tenant `ClientStore` lookup.

**SignalR Communication**
- **Authentication (AB#5062)**: `SignalRClient<TOptions>.ConfigureHttpConnectionOptions` supplies the
  `IServiceClientAccessToken` the client is constructed with through
  `HttpConnectionOptions.AccessTokenProvider`. It used to write a literal
  `options.Headers["Authorization"] = "Bearer your-access-token"` under a `// TODO`, so the token
  object every caller already passed in was accepted and never read — which is why the controller's
  `/operatorHub` gate (AB#5059) had to ship observing-only. Three reasons it is the provider and not a
  header, all load-bearing:
  - The provider is invoked on **every** connection attempt (negotiate plus each transport start), so
    the token is re-read on every reconnect. A header freezes the value at connection-object
    construction time, and the reconnect loop here reuses one `HubConnection` for days.
  - SignalR sends no headers on the WebSocket and SSE transports; the provider's value goes into the
    `access_token` query parameter there instead. The controller's `OnMessageReceived` accepts it on
    the hub paths only, for exactly this reason.
  - A blank token yields `null` ⇒ **no** credential at all, neither header nor query parameter. An
    unconfigured client must look anonymous rather than present a malformed bearer: a garbage token
    reaches the server as a *failed* authentication rather than an absent one, which reads as expired
    or revoked and hides "never given a token" from any inventory built on it.

  Tests: `tests/Sdk.ServiceClient.Tests/SignalRClientAccessTokenTests.cs` (token supplied, re-read
  rather than frozen, blank ⇒ no credential, no placeholder header, caller headers still copied, and
  the built `HubConnection` really carries the provider).
- **`ISignalRClient.IsAlive` is a state query (AB#5473)**: it never throws and never creates a
  connection, and it is `false` for a client that is stopping, stopped or not yet started — which
  includes the gap between `StopAsync` and the next `StartAsync`. It reads the connection field
  directly. The protected `HubConnection` accessor is the opposite on purpose: it is for operations,
  throws `ObjectDisposedException` while the client is stopped (AB#3379) and creates the connection
  on first use. Building `IsAlive` on that accessor made every poller throw during the restart a
  tenant update triggers, which ended the adapter process. Tests: `SignalRClientTests`
  (`IsAlive_AfterStop_ReturnsFalse`, `IsAlive_BeforeStart_ReturnsFalseWithoutCreatingConnection`).
- **Connect/reconnect retries log their cause (AB#6418)**: a failed attempt in the start or reconnect
  loop is reported by `LogRetryableFailure` with exception type, message, HTTP status code (if any) and
  the innermost cause — before, the line was a bare "Common error during connect to SignalR hub X.
  Trying again.." and a hub behind a 503 ingress could not be told apart from any other failure. The
  same cause is logged at WARN at most once per `RetryFailureLogInterval` (60 s), tracked per cause;
  repeats go to Debug and are counted in the next report of that cause. Only type, message and status
  (also from a wrapped `HttpRequestException`) are logged, `access_token=`/`Bearer` values are redacted
  and the exception object is not attached. Tests: `SignalRClientRetryLoggingTests`.
- Bidirectional: Server-side `IAdapterHub` ↔ Client-side `IAdapterHubCallbacks`
- **`IAdapterPoolHub` ↔ `IAdapterPoolHubCallbacks`** (AB#4924) — the **tenant-free** management
  channel of adapter *pool members*, served at `/adapterPoolHub` and driven client-side by
  `AdapterPoolHubClient`. 🔴 A separate hub from `IAdapterHub` on purpose:
  `/{tenantId:tenantId}/adapterHub` is tenant-addressed and `AdapterHubAuthorizationFilter` (AB#5063)
  exists to bind a connection to its route tenant, while a pool member belongs to **no** tenant — it
  is handed one per lease. `AdapterPoolHubClient` therefore overrides `BuildServiceUri` to reach
  `{endpoint}/adapterPoolHub`, exactly as `OperatorHubClient` does for `/operatorHub`; the base
  implementation throws on a blank tenant, which is correct for every tenant-addressed hub.
  🔴 **`PoolMemberRegistrationDto.NodeDescriptors` replaced an earlier `NodeNames` string list**
  (AB#4924, 15.09.2026), and gained a sibling `PipelineSchemaJson`. A bare name cannot answer any of
  the questions a *borrower's* `DeployPipeline` asks about the pool that will execute its pipelines —
  execution class, process-boundness, node version, configuration schema — so the field now carries
  the same `NodeDescriptorDto`s a dedicated adapter sends on `RegisterAdapterWithSchemaAsync`.
  **Source-breaking, wire-compatible:** SignalR's JSON hub protocol ignores unknown members and
  defaults absent ones, and no member ever populated `NodeNames`, so no payload on any wire carried
  it. Publish order as for every hub contract: this repo first, then `octo-communication-sdk` and
  `octo-communication-controller-services`, or they fail with `CS0117`.
  🔴 **AB#5826 — a lease survives a controller restart.** `IAdapterPoolHub.ResumePoolMemberAsync`
  (registration with `PoolMemberRegistrationDto.ActiveLease` = `PoolMemberActiveLeaseDto`, identifiers
  only; answer carries `ActiveLeaseAdopted`) and `LeaseResultDto.ExecutionId`/`TenantId`/`MemberId`.
  All additive and defaulted. The method is separate on purpose: an older controller answers "unknown
  hub method" instead of registering a busy member as idle; the interface's default implementation
  answers `NotSupportedException` the same way, and `AdapterPoolHubClient` must implement it itself
  (pinned by `ResumePoolMember_IsImplementedByTheClient_NotInheritedFromTheInterfaceDefault`).
  Tests: `Communication.Contracts.Tests/DataTransferObjects/LeaseResumptionContractTests.cs`.
  🔴 **`LeaseDto` carries a client secret** — the borrower's own `PipelineServiceAccount` credential
  (AB#5027, concept §8 Q6) — so it overrides `ToString` to keep it out of any log a structured-logging
  call would produce. A record's generated `ToString` prints every property; pinned by
  `Communication.Contracts.Tests/DataTransferObjects/LeaseDtoTests.cs`.
  🟢 **AB#7458 — `ExecutePipelineMethodInvocation` marker.** `MessageObjects/ExecutePipelineMethodInvocation`
  (`MethodId`, `InvocationId`, `CkTypeId`, `PipelineRtId`) sits next to `ExecutePipelineCaller` and is
  carried as nullable `MethodInvocation` on `ExecutePipelineRequest` and `LeaseDto` (F3.7, AB#6884: lets
  a `replacesGenericCreate` method's own pipeline run create entities of its type). Set **only by the
  controller's method invoker, never by clients** — the controller must overwrite/clear it on every request
  it did not build; it is a marker, not a proof (no bus-message signing). Additive and nullable: old
  payloads give `null`, old peers ignore the unknown member. Not rendered by `LeaseDto.ToString`.
  Publish order: this repo first, then communication-sdk and the controller. Tests:
  `Communication.Contracts.Tests/MessageObjects/ExecutePipelineMethodInvocationTests.cs` (System.Text.Json,
  default and web options, both directions). There is no CHANGELOG for `Communication.Contracts`; this
  section is its change record.
- Adapter lifecycle: Register → Receive config → Pre-update notifications → Send results
- `IAdapterHubCallbacks.CkModelChangedAsync(tenantId)` (AB#4456): controller→adapter broadcast telling
  adapters to invalidate their in-process CK model cache after a tenant update (CK model import,
  `ClearCache`). Unlike `PreUpdateTenantAsync` it does not restart the adapter. The controller sends it
  to **all** adapter connections (not routed via the adapter cache); adapters filter by their own tenant.
  Bound in `AdapterHubClient`; old adapter builds without the handler just log an unbound-method warning.

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

## Scratch tenants and the tenant feature disabler (AB#7192)

- `ScratchTenantMarker` (`Communication.Contracts`): the persisted record of a scratch tenant (creator client, parent, grant / revoke time), kept in
  the system tenant configuration under `ScratchTenantMarker.GetConfigurationKey(tenantId)`; `TryCreateForCaller` is the single check "may this
  principal create a scratch tenant whose direct parent is X" (client-credentials token, `client_id`, `tenant_id` = X) used by the asset
  repository. `TenantDto` carries `IsScratch`, `ScratchCreatorClientId`, `ScratchCreatedAtUtc`, `ScratchAccessRevoked`.
- `IAssetServicesClient.CreateTenantAsync(child, db, scratch)` sends `scratch=true` only when set. (A restore has no scratch flag: restore into a scratch tenant with its child token; the marker survives.) `IAuthenticatorClient.RequestClientCredentialsTokenForTenantAsync` issues a client-credentials token for a named tenant (the mirror
  of an agent's client in its scratch tenant).
- `TenantFeatureDisabler` (`TenantLifecycle`): switches off the capabilities `TenantFeaturesStatusDto` reports as enabled - AI Services,
  Communication, Reporting, Stream Data, in that order - through `ITenantFeatureSwitches`, which the caller binds to the **child tenant's own token**.
  A refused capability (Stream Data with activated archives answers 409 naming them) is reported with the service's reason and does not stop the
  others; nothing is deleted. Shared by octo-cli `Delete --disable-features` and the MCP tool `delete_tenant` (`disableFeatures`).
