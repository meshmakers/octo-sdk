namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     Options for the <see cref="AiServicesClient" />. Enable / Disable run against the
///     System API so <see cref="TenantId"/> is optional here (we fall back to <c>system/v1</c>
///     when it is empty); the tenant-scoped Phase-1 endpoints are not yet covered by the SDK.
/// </summary>
public class AiServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests. Optional for the System-API enable / disable
    ///     calls; when set, future tenant-scoped methods route to <c>{tenantId}/v1</c>.
    /// </summary>
    public string? TenantId { get; set; }
}
