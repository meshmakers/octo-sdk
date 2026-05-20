namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Tracking row describing a <see cref="ClientDto"/> that has been
///     auto-provisioned from a parent tenant into a specific child tenant.
///     Lives in the parent tenant's identity DB.
/// </summary>
public sealed record ClientMirrorDto(
    string ParentClientId,
    string ParentTenantId,
    string ChildTenantId,
    DateTime ProvisionedAt,
    int SecretHashVersion);

/// <summary>
///     Result body for the backfill operation
///     (<c>POST .../{clientId}/mirrors/provisionInExistingTenants</c>).
/// </summary>
public sealed record ClientMirrorBackfillResponseDto(
    int ChildTenantsConsidered,
    int NewlyProvisioned,
    int AlreadyPresent);

/// <summary>
///     Result body for a single-tenant manual provision operation
///     (<c>POST .../{clientId}/mirrors/provisionInTenant</c>).
/// </summary>
public sealed record ClientMirrorProvisionResponseDto(
    int FlaggedClientsConsidered,
    int NewlyProvisioned,
    int AlreadyPresent);

/// <summary>
///     Body for <c>PATCH .../{clientId}/autoProvisionInChildTenants</c>.
/// </summary>
public sealed record SetAutoProvisionInChildTenantsDto(bool Enabled);
