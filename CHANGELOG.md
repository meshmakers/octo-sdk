# Changelog

Notable changes of the Octo SDK. Versions come from the release pipeline; entries name the work item.

## Unreleased

### Added

- AB#9052 (communication wave 3, Feature F5.3): method clients for the System.Communication operations, proven against
  `System.Communication` 4.19.0. The SDK ships no System.Communication surface; a consumer generates
  `RtAdapterMethodClient`, `RtAdapterPoolMethodClient`, `RtApplicationMethodClient`, `RtPipelineMethodClient`,
  `RtDataFlowMethodClient`, `RtDeploymentSiteMethodClient` and `RtPipelineTriggerMethodClient` (22 CK methods, 30 client
  methods: the four `DeployableWorkload` methods on each concrete workload type) from `System.Communication-[4.19,5.0)`,
  including the methods without an SDK REST wrapper (`DeployUpdate`, `Wake`, `ReconcileServiceAccount`, the pool mirrors
  and the lease). Tests: `CommunicationMethodClientTests` (one client method per CK method, signatures of the
  replacements, declared error constants equal to the model's, every document validated against the recorded schema of
  an asset repository tenant with System.Communication 4.19.0, answers recorded from its method gateway as typed results).
  New fixtures: `ck-system.communication-4.*`, `ck-system-2.yaml`, `assetrepo-system-communication-schema.graphql.gz`,
  `assetrepo-communication-answer-*.json` (`docs/method-clients.md`). Migration guide: "Communication wave 3". Additive:
  the replaced `ICommunicationServicesClient` methods are not obsolete yet; they become `[Obsolete]` after octo-cli and
  octo-mcp-service switched (AB#9053), as in AB#8860 and AB#9044.
- AB#9044 (identity wave 2, Feature F5.2): method clients for the identity wave 2 operations, proven against
  `System.Identity` 3.8.0. The SDK still ships no System.Identity surface; a consumer generates `RtGroupMethodClient`,
  `RtClientMethodClient`, `RtRoleMethodClient`, `RtApiScopeMethodClient`, `RtApiResourceMethodClient`,
  `RtDataPermissionMethodClient`, `RtDataPolicyMethodClient`, `RtExternalTenantUserMappingMethodClient`,
  `RtEmailDomainGroupRuleMethodClient` and one client per concrete identity provider type from
  `System.Identity-[3.x,4.0)`. Tests: `IdentityWave2MethodClientTests` (one client method per CK method, signatures of the
  replacements, declared error constants equal to the model's, answers recorded from the asset repository's method
  gateway as typed results). The System.Identity fixtures, the recorded schema and the sync-client golden of System.Identity
  (generator of 7c48ed3) are re-recorded with 3.8.0 (`docs/method-clients.md`). Migration guide: "Identity wave 2" (operation
  map from the 57 REST write methods of `IIdentityServicesClient` to the method clients, differences, error mapping).
  Additive: the REST methods are not obsolete yet; they become `[Obsolete]` after octo-cli and octo-mcp-service switched
  (AB#9045, AB#9046), as in AB#8860.
- AB#8966: blueprint uninstall results and dry run. `BlueprintUninstallResultDto` gains `DryRun`, `DeletedEntities`
  (`BlueprintUninstallDeletedEntityDto`: key, ckTypeId, entityId, wellKnownName, `TenantOwned`) and `TenantOwnedKept`
  (`BlueprintTenantOwnedKeptEntityDto`: the same identity plus `Reason` - `Edited` | `Referenced` | `NotComparable` | `EraseFailed`, a string -
  and `ReferencedBy`); all empty or false from asset repositories without AB#8966. New
  `IAssetServicesClient.PreviewUninstallBlueprintAsync(tenantId, blueprintName, cascade)` calls
  `POST blueprints/{name}/uninstall/preview`, a route of its own: an older asset repository answers 404 and never
  uninstalls. Additive: one new interface member (implemented by `AssetServicesClient` only).
  - **Behaviour change:** `UninstallBlueprintAsync` (and the dry run) return a 409 "blocked by dependents" as a result with
    `Success = false` and `BlockingDependents` instead of throwing `ServiceClientResultException`, so callers can name the
    dependents (octo-cli's `--cascade` hint never ran before).

### Deprecated

- AB#8860: `IIdentityServicesClient.CreateUser` and `ResetPassword` (and the `IdentityServicesClient` implementations)
  are `[Obsolete]` (warning, not error) with a pointer to `RtUserMethodClient.CreateUserAsync` / `ResetPasswordAsync`
  generated from `System.Identity-[3.2,4.0)`. They keep calling REST (`POST users`, `POST users/ResetPassword`) during
  their REST window. The first-party consumers switched before (octo-cli #83, octo-mcp-service #74, AB#8418); no
  first-party repository calls the two methods any more. External consumers with warnings-as-errors get `CS0618`; the
  method client needs `Methods:Identity:EnablePasswordMethods` on the identity service — where it is off, keep the REST
  call and suppress the warning at that call only (`docs/method-client-migration-guide.md`, "Identity wave 1").

### Fixed

- AB#9493: the DTO generator compiles models whose types derive from a type of another model, or carry attributes it does
  not support. A base type of another model is now referenced with the namespace of that model's DTOs
  (`global::{root}.DataTransferObjects.{Model}.v{Major}.Rt{Base}Dto`; before: unqualified, CS0246 - e.g. the 16
  System.Communication types deriving from `System@2/Configuration`); a base of the same model and `System/Entity` stay
  unqualified. A mutation DTO no longer writes `[JsonIgnore]` before an attribute it generates no property for (an
  unsupported value type such as Binary or BinaryLinked, which only becomes a comment, or an Enum or Record attribute
  without its element id, which produces nothing); the attribute had attached to the next property (CS0579 duplicate
  attribute, e.g. `RtEnergyCommunityConfigurationMutationDto`). Generated code of every other model is unchanged except
  that one line per such attribute of a mutation DTO is gone (DTO golden of `ck-assetrepositoryintegrationtest`: three
  lines).
  - **Behavior change:** the qualified base expects the base model's DTOs in the same generated root namespace, i.e. the
    consumer generates the base model too (imports it in `construction-kits.yaml`). A consumer that compiled such a
    derived type before only through a hand-written `using` to DTOs of another root namespace or another assembly now gets
    CS0234 and must generate the base model in the same project.
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
- `CreateUser` and `ResetPassword` are obsolete too, see the AB#8860 entry under "Deprecated" above.

### Changed

- `QlMutationResponseConverter` also reads a single object under the per-type mutation field (method fields); lists
  (create/update/delete) are read as before.

### Unchanged

- Generated sources of models and types without methods are byte-identical (snapshot gate
  `MethodlessGeneratedCodeGoldenTests`).
