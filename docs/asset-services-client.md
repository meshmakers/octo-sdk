---
description: CK catalog import and upgrade guard, blueprint apply, the files API, raw GraphQL on a tenant and scratch tenants.
applies_to: src/Sdk.ServiceClient/{AssetRepositoryServices,TenantLifecycle}/**, tests/Sdk.ServiceClient.Tests/{AssetRepositoryServices,TenantLifecycle}/**
---

# Asset services client

**CK Model Catalog DTOs (LibraryStatus / CheckUpgrade)**
- `CkModelLibraryStatusItemDto` carries the tenant upgrade guard result of the offered catalog version (read-only on this DTO; the import calls below enforce it): `IsBlocked`, `Blockers` (`CkUpgradeBlockerDto`: `Dependent`, `Update`, `Kind`, `Subject`, `Code`, `Message`, `DependentIsInUpdateSet`, `SuggestedDependentUpdate`, `ResolveHint`) and `IsRangeRetaining` (installed model declares dependency ranges). A blocked row keeps `HasUpdate = true` and has `NeedsAction = false`.
- `UpgradeCheckResponseDto` carries `IsBlocked` and `Blockers` as well; `HasBreakingChanges` stays the model's own migration verdict.

**CK Model Catalog import enforcement (tenant upgrade guard)**
- `ImportFromCatalogRequestDto` and `ImportFromCatalogBatchRequestDto` carry `ForceBreakingUpdate` (default `false`; override of the guard, needs the data model management policy; ignored by the read-only calls). `DependencyResolutionResponseDto` (one tree) and `BatchDependencyResolutionResponseDto` (`ModelsToImport` as one set) carry `IsBlocked` / `Blockers` so a preview agrees with the refusal.
- `IAssetServicesClient.ImportFromCatalogAsync` (new, single model, returns `TransferModelResponseDto`) and `ImportFromCatalogBatchAsync` turn the service's `409` problem details with `code = CK_UPGRADE_BLOCKED` into `CkUpgradeBlockedException : ServiceClientResultException` (`Blockers`, `UpdateSet`, `Detail`); another 409 stays a plain `ServiceClientResultException`. A batch is refused as a whole.
- Tests: `ImportFromCatalogUpgradeGuardTests` (loopback service).
- **File import**: `ImportCkModelAsync(tenantId, path)` now also throws `CkUpgradeBlockedException` when the service refuses the file (a breaking
  file, `System` included); the additive overload `ImportCkModelAsync(tenantId, path, bool forceBreakingUpdate)` sends the form member
  `forceBreakingUpdate=true` next to the file (needs the data model management policy, else 403). Both go through the tenant-scope client like
  `ImportFromCatalogAsync`. An older service ignores the member (and does not guard). `CheckUpgradeAsync` already carries `IsBlocked` / `Blockers`
  and, since the service change, judges `System` too. Tests: `ImportFromCatalogUpgradeGuardTests` (`ImportCkModelAsync_*`; `LoopbackHttpService.Bodies`).
- All new members are optional on the wire: against an older asset repository they keep their neutral defaults (`false` / empty), and older clients ignore them. `Kind` is a string and may gain values.

**CK model draft validation**
- `IAssetServicesClient.ValidateCkModelAsync(tenantId, path)` posts the draft (`.zip` / `.json` / `.yaml`, same mime rule as `ImportCkModelAsync`, shared
  `AddCkModelFile`) as multipart member `file` to `{tenantId}/v1/models/ValidateCk` through the tenant-scope client and returns
  `CkModelValidationResponseDto` (`IsValid`, `ModelId`, `SourceKind`, `Errors`/`Warnings` = `CkModelValidationMessageDto`, `IsBlocked`, `Blockers`).
  An invalid draft is a 200 result, not an exception. Needs an asset repository that has this endpoint (an older one answers 404). Consumer: MCP `validate_ck_model`.
  Tests: `ValidateCkModelTests` (loopback service).

**Blueprint apply enforcement (tenant upgrade guard)**
- `BlueprintApplyRequestDto` and `BlueprintUpdateRequestDto` carry `ForceBreakingUpdate` (default `false`; needs the data model management policy; the install's `Force` still only re-applies seed data). `BlueprintUpdatePreviewDto` and `BlueprintUpdateResultDto` carry `IsBlocked`, `CkUpdateSet` and `Blockers` (`CkUpgradeBlockerDto`, the entries of the library status); `BlueprintApplyResultDto` carries `CkUpdateSet` and the forced `Blockers`.
- `IAssetServicesClient.ApplyBlueprintAsync(tenantId, BlueprintApplyRequestDto)` (new overload; the `(tenantId, blueprintId, force)` overload calls it) and `ApplyBlueprintUpdateAsync` turn the service's `409` with `code = CK_UPGRADE_BLOCKED` into `CkUpgradeBlockedException` (same type as the imports); another 409 stays a `ServiceClientResultException`. The preview and a dry run answer 200 with `IsBlocked`.
- Tests: `BlueprintUpgradeGuardClientTests` (loopback service). Members are optional on the wire; against an older service they read as `false` / empty.

## Asset Services Client — files API

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
  (until the response headers), `MaxTimeout` for metadata, stats and capabilities. The body stream of a download is
  wrapped in `ReadIdleTimeoutStream`: a read that gets no data for `StreamReadIdleTimeout` (default 2 min,
  ≤ 0 = none) throws `TimeoutException`; a slow but progressing body is never cut off.
  A timeout throws `TimeoutException`, a caller cancellation stays `OperationCanceledException`. The default
  constructor uses a process-wide `HttpClient` over one `SocketsHttpHandler` (`PooledConnectionLifetime` 5 min; one per
  `ServerCertificateTrust` mode), because the MCP server creates an asset client per tool call; `Dispose` does not
  close it. `AssetServicesClient.Files` is a `Lazy<FilesClient>` (thread-safe).
- **Deliberately a plain `HttpClient`, not RestSharp and not `TenantClient`:** uploads/downloads must stream (100 MB),
  the upload body must not dispose the caller's stream (`UploadContent`, not `StreamContent`), and the error codes
  must reach the caller — RestSharp buffers, and `TenantClient.CheckResult` flattens GraphQL errors to text (the
  errors with their `extensions` are kept on `QlQueryErrorException.Errors`, but the message stays text). The bearer token is read from the token object per request.
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

## Raw GraphQL on a tenant — `SendGraphQlAsync`

`IAssetServicesClient.SendGraphQlAsync(tenantId, query, variablesJson?, operationName?, ct)` posts one GraphQL document to
`{endpoint}/tenants/{tenantId}/GraphQL` and returns `GraphQlRawResponse` (`Content` = the body exactly as the service sent
it, `StatusCode`, derived `HasErrors` = non-empty top-level `errors` array). It exists for callers that want the untouched
JSON (octo-cli `GraphQl`, scripts) and not a typed DTO; typed use stays `ITenantClient.SendQueryAsync<TDto>`.

- **Same choke point as the REST routes:** the token comes from `ResolveTenantAccessTokenAsync`, which
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

## Scratch tenants and the tenant feature disabler

- `ScratchTenantMarker` (`Communication.Contracts`): the persisted record of a scratch tenant (creator client, parent, grant / revoke time), kept in
  the system tenant configuration under `ScratchTenantMarker.GetConfigurationKey(tenantId)`; `TryCreateForCaller` is the single check "may this
  principal create a scratch tenant whose direct parent is X" (client-credentials token, `client_id`, `tenant_id` = X) used by the asset
  repository. `TenantDto` carries `IsScratch`, `ScratchCreatorClientId`, `ScratchCreatedAtUtc`, `ScratchAccessRevoked`.
- `IAssetServicesClient.CreateTenantAsync(child, db, scratch)` sends `scratch=true` only when set; the overload with `blueprintId` sends `blueprintId` only when set (the service applies the blueprint in the create transaction, a failure aborts and throws its 400 message; tests `CreateTenantWithBlueprintClientTests`). (A restore has no scratch flag: restore into a scratch tenant with its child token; the marker survives.) `IAuthenticatorClient.RequestClientCredentialsTokenForTenantAsync` issues a client-credentials token for a named tenant (the mirror
  of an agent's client in its scratch tenant).
- `TenantFeatureDisabler` (`TenantLifecycle`): switches off the capabilities `TenantFeaturesStatusDto` reports as enabled - AI Services,
  Communication, Reporting, Stream Data, in that order - through `ITenantFeatureSwitches`, which the caller binds to the **child tenant's own token**.
  A refused capability (Stream Data with activated archives answers 409 naming them) is reported with the service's reason and does not stop the
  others; nothing is deleted. Shared by octo-cli `Delete --disable-features` and the MCP tool `delete_tenant` (`disableFeatures`).
