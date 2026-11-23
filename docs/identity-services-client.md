---
description: Identity DTOs, client methods, cross-tenant grants and why credentials travel in the request body only.
applies_to: src/Sdk.ServiceClient/IdentityServices/**, tests/Sdk.ServiceClient.Tests/IdentityServices/**
---

# Identity services client

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
- `CreateExternalTenantUserMappingDto` — creation DTO with `SourceTenantId`, `SourceUserId`, `SourceUserName` (all required), `RoleIds`, optional `GroupIds` (target-tenant groups; not serialized when `null`)
- `SourceUserLookupRequestDto` / `SourceUserLookupResultDto` — exact lookup of one source-tenant user (`SourceTenantId`, `Identifier` → `SourceTenantId`, `UserId`, `UserName`, `DisplayName`; no e-mail). Problem codes in `ApiErrors/CrossTenantGrantProblemCodes`. Contract: `octo-identity-services/docs/CONCEPT-SIBLING-TENANT-GRANTS.md`
- `UpdateExternalTenantUserMappingDto` — update DTO with `RoleIds`
- `IIdentityServicesClient.GrantExternalTenantUser` — same route as `CreateExternalTenantUserMapping`, returns the stored mapping (201 body: user name from the source tenant's record, `GroupNames`)
- `CrossTenantGrantPermissionsDto` — `TargetTenantIds`, the tenant's allow-list as a source of sibling-tenant grants; `GetCrossTenantGrantPermissions()` / `UpdateCrossTenantGrantPermissions(dto, ifMatch = null)` on the client's tenant. `ifMatch` is sent as `If-Match` when given and reserved for an ETag on the PUT; identity services before it ignore it
- `ServiceProblem.TryParse(ServiceClientResultException, out problem)` — reads RFC 9457 problem bodies (`Status`, `Code`, `Title`, `Detail`, `RetryAfterSeconds`, `UnknownGroupIds`, `ExistingMappingId`; `ToDisplayText()` for one readable line). `ServiceClientResultException.ResponseContent` holds the raw body (set by `ValidateResponse`)

**Identity Services Client Methods**
- Groups: `GetGroups`, `GetGroup`, `GetGroupByName`, `CreateGroup`, `UpdateGroup`, `DeleteGroup`, `UpdateGroupRoles`, `AddUserToGroup`, `RemoveUserFromGroup`, `AddGroupToGroup`, `RemoveGroupFromGroup`, `AddClientToGroup`, `RemoveClientFromGroup`
- Client roles: `GetClientDirectRoles`, `UpdateClientRoles`, `AddRoleToClient`, `RemoveRoleFromClient`. `GroupDto` carries `MemberClientIds`; `ClientDto` carries `RtId` (read-only)
- External Tenant User Mappings: `GetExternalTenantUserMappings` (with skip/take/sourceTenantId), `GetExternalTenantUserMapping`, `CreateExternalTenantUserMapping`, `UpdateExternalTenantUserMapping`, `DeleteExternalTenantUserMapping`, `LookupSourceUser` (`null` on the uniform `404 SourceUserNotFound`, throws otherwise)
- Admin Provisioning: `GetAdminProvisioningMappings`, `CreateAdminProvisioningMapping`, `ProvisionCurrentUser`, `DeleteAdminProvisioningMapping`

## Identity Services Client — no credential in a request URL

`IdentityServicesClient` sends credential values only in the JSON body, never in the URL (request logs, traces and proxy access
logs capture URLs):

| Method | Request | Body DTO (`Communication.Contracts`) |
|--------|---------|--------------------------------------|
| `ResetPassword(userName, password)` | `POST users/ResetPassword` | `UserPasswordResetDto { userName, password }` |
| `DeleteApiSecretClient` / `DeleteApiSecretApiResource(id, storedValue)` | `POST apiSecrets/{client\|apiResource}/{id}/delete` | `ApiSecretReferenceDto { valueEncrypted }` |
| `GetApiSecretForClient` / `GetApiSecretForApiResource(id, storedValue)` | `POST apiSecrets/{client\|apiResource}/{id}/lookup` | `ApiSecretReferenceDto { valueEncrypted }` |

`storedValue` is the stored hash (`ApiSecretDto.ValueEncrypted`), no longer Base64-encoded again. The identity service refuses the
former reset query form (`?userName=&password=`) with 400; the former secret path variants (`DELETE`/`GET .../{base64(hash)}`) stay
there for one release. **Compatibility:** these methods need an identity service with the credentials-in-body change — an older one answers the body form
with 400 (reset) or 405 (secret delete/lookup). Both DTOs override `ToString()` without the value.
`UpdateApiSecretClient` / `UpdateApiSecretApiResource` send `PUT` (they used `ExecutePostAsync`, which forces `POST` - the create
endpoint - so an update generated a new secret instead; fixed). Tests:
`Sdk.ServiceClient.Tests/IdentityServices/CredentialsInBodyClientTests` (local listener: method, path without the value, body).
