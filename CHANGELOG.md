# Changelog

Notable changes of the Octo SDK. Versions come from the release pipeline; entries name the work item.

## Unreleased

### Deprecated

- AB#8860: `IIdentityServicesClient.CreateUser` and `ResetPassword` (and the `IdentityServicesClient` implementations)
  are `[Obsolete]` (warning, not error) with a pointer to `RtUserMethodClient.CreateUserAsync` / `ResetPasswordAsync`
  generated from `System.Identity-[3.2,4.0)`. They keep calling REST (`POST users`, `POST users/ResetPassword`) during
  their REST window. The first-party consumers switched before (octo-cli #83, octo-mcp-service #74, AB#8418); no
  first-party repository calls the two methods any more. External consumers with warnings-as-errors get `CS0618`; the
  method client needs `Methods:Identity:EnablePasswordMethods` on the identity service — where it is off, keep the REST
  call and suppress the warning at that call only (`docs/method-client-migration-guide.md`, "Identity wave 1").

### Fixed

- AB#8920: a tenant dump download (`IBotServicesClient.DownloadDumpToFileAsync`) or a file download of the files client
  whose connection dies silently no longer waits forever. The response body is read with a read-idle timeout, the new
  `ServiceClientOptions.StreamReadIdleTimeout` (default 2 minutes, zero or negative = none): a read that gets no data for
  that long throws a `TimeoutException` naming the transfer, and the dump download deletes its part file. The timer
  restarts with every read, so a slow but progressing download is never aborted.

- AB#8885: `IBotServicesClient.StartRestoreFromUploadAsync` (AB#8447) gets an overload with `overrideForeignOwnership`
  (AB#8672), shaped like the one of `RestoreRepositoryWithTusAsync`: every parameter required, the query parameter
  `overrideForeignOwnership=True` is sent only when set, the short signature keeps its defaults and restores without the
  override. `RestoreRepositoryWithTusAsync` with the override now passes it on to the start of the job. Repairs the compile
  of `main` after #107 and #109 met. Additive interface member: implementations outside this repository (none known) need it.

### Added

- AB#8778: `BlueprintUpdatePreviewDto` and `BlueprintUpdateResultDto` read `TenantOwnedRefreshed` (unedited tenant-owned
  seed entities the update writes with the changed seed; not in `Safe` mode) and `TenantOwnedUpdateAvailable` (entities it
  keeps although the seed changed, because their content differs from the installed seed or the comparison was not possible). Empty against older services.
- **Upload and start separately (AB#8447, F3.6).** `IBotServicesClient.UploadRestoreFileWithTusAsync` and
  `UploadArchiveDataWithTusAsync` upload a backup or an archive data export with tus (in parts, with resume) and return the tus file
  id without starting anything; `StartRestoreFromUploadAsync` and `StartImportArchiveDataFromUploadAsync` start the REST job from
  such an upload. octo-cli uses them to start `System.Bot/TenantRepository.Restore-1` / `ImportArchiveData-1` with the upload and to
  fall back to the REST job with the same upload. `RestoreRepositoryWithTusAsync` and `StartImportArchiveDataWithTusAsync` are
  unchanged (now composed of the two steps). Additive interface members: implementations outside this repository (none known) need them.

- **Operation handles of asynchronous CK methods (AB#8446, F3.6).** The generated client of a method declared
  `execution.mode: async` returns `CkMethodResult<CkOperationHandle<T>>` (`CkOperationHandle` without a result) and selects
  `operation { operationId acceptedAtUtc retryAfterSeconds expiresAtUtc }` instead of `result`; before, it selected `result`,
  which the asset repository's schema does not have for an asynchronous method (AB#8444), so the call failed. The new
  `CkOperationsClient` reads the operations through `runtime.operations`: `GetOperationAsync`, `ListOperationsAsync`
  (method, state, since, all owners, paging), `WaitForOperationAsync` (typed result on `SUCCEEDED`, declared error as
  `CkMethodResult.Error`, every other code such as `METHOD_OUTCOME_UNKNOWN` thrown as `CkMethodCallException`; waits
  `retryAfterSeconds` between reads, within 1-30 s; progress callback; the cancellation token ends only the wait) and
  `WaitForCompletionAsync` (the final operation as read). `OPERATION_NOT_FOUND` / `OPERATION_QUERY_REFUSED` throw
  `CkOperationQueryException`. Clients of synchronous methods are byte-identical (golden `SyncMethodClientGoldenTests`).
  Needs an asset repository with AB#8444. Docs: `docs/method-clients.md`, "Asynchronous methods".

- **Raw-archive writes and corrections (AB#8590).** `IStreamDataServicesClient.InsertAsync(tenantId, archiveRtId,
  IReadOnlyCollection<RawPointDto>)` posts to `{tenantId}/v1/streamdata/archives/{archiveRtId}/insert` of the asset repository
  and returns `StreamDataWriteResultDto`. `RawPointDto` mirrors `TimeRangePointDto` with `Timestamp` (sent as UTC; an unspecified
  kind is UTC) and reuses `StreamDataOrigin`. A raw row is identified by archive + `RtId` + `Timestamp` (milliseconds); a manual
  point must name every ingested column (null allowed). A refusal (400, 403, 409) throws - a batch with a manual point needs the
  tenant role `StreamDataAdmin` on both `InsertAsync` and `InsertTimeRangeAsync`, otherwise the service answers 403. An empty
  batch returns an empty result without a call; an answer without a readable write result
  throws instead of reporting rows as stored. Needs an asset repository with AB#8590 (an older one answers 404). Existing
  signatures are unchanged.

- **Coded 403 refusals reach the caller (AB#8529).** A tenant-scope call of `AssetServicesClient` that the service refuses
  with `403` and a body `{"message": "...", "code": "..."}` (e.g. the asset repository's `DataModelManagementPolicy`, code
  `DataModelManagementRequired`) now throws `ServiceClientResultException` (status `Forbidden`, `ResponseContent` set, message =
  service message plus code) instead of `TenantTokenExchangeDeniedException` with the provisioning hint, which pointed to the
  wrong remedy. A `403` without a code behaves as before. `ServiceProblem.TryParse` uses `message` as `Detail` when the body
  carries a `code`. New constants `AuthorizationProblemCodes.DataModelManagementRequired` (Communication.Contracts).

- **Sibling-tenant grants for CLI and MCP (AB#8346).** `IIdentityServicesClient.GrantExternalTenantUser` creates an
  external tenant user mapping and returns it as stored (user name from the source tenant, group names).
  `GetCrossTenantGrantPermissions` / `UpdateCrossTenantGrantPermissions` read and replace the tenant's allow-list as a
  source of sibling-tenant grants (`CrossTenantGrantPermissionsDto`, identity AB#8345); the optional `ifMatch` argument
  is sent as `If-Match` and is reserved for AB#8432. `ServiceProblem.TryParse` reads RFC 9457 problem answers (code,
  `retryAfterSeconds`, `unknownGroupIds`, `existingMappingId`) from a `ServiceClientResultException`, whose new
  `ResponseContent` property carries the response body. All additive; existing members are unchanged.

- **Typed CK method clients (AB#8423, F5.5).** `Meshmakers.Octo.Sdk.SourceGeneration` emits `Rt{Type}MethodClient` for
  every concrete CK type with methods: one async method per CK method on top of `ITenantClient.SendMutationAsync`,
  following the asset repository's typed method mutations (AB#5670). Declared method errors come back as
  `CkMethodResult.Error`; other errors throw `CkMethodCallException` (a `QlQueryErrorException`). Runtime support in
  `Meshmakers.Octo.Sdk.ServiceClient` (`AssetRepositoryServices.Tenants.Methods`). Docs: `docs/method-clients.md`,
  migration guide skeleton `docs/method-client-migration-guide.md`.
- `QlQueryErrorException.Errors`: the GraphQL errors of the response with their `extensions` (set by `TenantClient`).
- **User method clients, identity wave 1 (AB#8416, F5.1).** Consumers generate `RtUserMethodClient` from
  `System.Identity-[3.2,4.0)` (`construction-kits.yaml`): `CreateUserAsync`, `ResetPasswordAsync`, `AddRoleAsync`,
  `RemoveRoleAsync`, `SetRolesAsync`, `MergeAsync`, `ChangePasswordAsync`, `SetPasswordAsync`. The SDK ships no
  System.Identity surface of its own; System.Identity 3.2.0 is generator input of the tests only
  (`UserMethodClientTests`: signatures, documents validated against a recorded asset-repository schema with
  System.Identity, declared errors as typed results from recorded answers). Migration guide section "Identity wave 1".

### Deprecated

- `IIdentityServicesClient.AddRoleToUser` and `RemoveRoleFromUser` (and the `IdentityServicesClient` implementations)
  are `[Obsolete]` (warning) with a pointer to `RtUserMethodClient.AddRoleAsync` / `RemoveRoleAsync` generated from
  `System.Identity-[3.2,4.0)` (AB#8416). They keep calling REST during their REST window. Consumers with
  warnings-as-errors get `CS0618` and switch (octo-cli: AB#8417, octo-mcp-service: AB#8440).
- `CreateUser` and `ResetPassword` are **not** obsolete yet: `CreateUserAsync` / `ResetPasswordAsync` need
  `Methods:Identity:EnablePasswordMethods` on the identity service (off by default, off on staging/prod until B0). They
  become obsolete with AB#8418, once password methods are enabled on every cluster.

### Changed

- `QlMutationResponseConverter` also reads a single object under the per-type mutation field (method fields); lists
  (create/update/delete) are read as before.

### Unchanged

- Generated sources of models and types without methods are byte-identical (snapshot gate
  `MethodlessGeneratedCodeGoldenTests`).
