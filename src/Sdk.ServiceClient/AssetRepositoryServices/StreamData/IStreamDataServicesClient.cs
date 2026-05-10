namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     Interface for the StreamData services.
/// </summary>
public interface IStreamDataServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the StreamData for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables StreamData  for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);

    /// <summary>
    ///     Activates a CkArchive: provisions the per-archive CrateDB table and transitions the
    ///     archive to <c>Activated</c>. Allowed from <c>Created</c>, <c>Disabled</c>, or
    ///     <c>Failed</c>; idempotent on <c>Activated</c>.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    Task ActivateArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Disables a CkArchive: transitions to <c>Disabled</c> (data preserved). Allowed only
    ///     from <c>Activated</c>.
    /// </summary>
    Task DisableArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Re-enables a previously disabled archive: transitions <c>Disabled → Activated</c>.
    ///     Re-validates column paths against the current CK model; no DDL.
    /// </summary>
    Task EnableArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Retries activation after a previous DDL failure. Allowed only from <c>Failed</c>.
    /// </summary>
    Task RetryArchiveActivationAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Drops the per-archive CrateDB table (idempotent) and soft-deletes the <c>CkArchive</c>
    ///     entity. Destructive — historical data is lost. Allowed from any status.
    /// </summary>
    Task DeleteArchiveAsync(string tenantId, string archiveRtId);
}