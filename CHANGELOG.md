# Changelog

Notable changes of the Octo SDK. Versions come from the release pipeline; entries name the work item.

## Unreleased

### Added

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

- `IIdentityServicesClient.CreateUser`, `ResetPassword`, `AddRoleToUser` and `RemoveRoleFromUser` (and the
  `IdentityServicesClient` implementations) are `[Obsolete]` (warning) with a pointer to `RtUserMethodClient.<Method>Async`
  generated from `System.Identity-[3.2,4.0)` (AB#8416). They keep calling REST during their REST window. Consumers with
  warnings-as-errors get `CS0618` and switch (octo-cli: AB#8417).

### Changed

- `QlMutationResponseConverter` also reads a single object under the per-type mutation field (method fields); lists
  (create/update/delete) are read as before.

### Unchanged

- Generated sources of models and types without methods are byte-identical (snapshot gate
  `MethodlessGeneratedCodeGoldenTests`).
